using System.Text;
using System.Text.Json;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CmsSyncService.Api.Messaging;

public sealed class RabbitMqCmsEventWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqCmsEventWorker> _logger;

    public RabbitMqCmsEventWorker(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqCmsEventWorker> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var retryDelay = InitialRetryDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
                retryDelay = InitialRetryDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ CMS event worker failed. Retrying in {RetryDelay}.", retryDelay);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = GetNextRetryDelay(retryDelay);
            }
        }
    }

    private static TimeSpan GetNextRetryDelay(TimeSpan currentDelay)
    {
        var nextDelaySeconds = Math.Min(currentDelay.TotalSeconds * 2, MaxRetryDelay.TotalSeconds);
        return TimeSpan.FromSeconds(nextDelaySeconds);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqCmsEventPublisher.CreateConnectionFactory(_options);
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.Span);
                var events = JsonSerializer.Deserialize<List<CmsEventDto>>(json, SerializerOptions) ?? [];

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ICmsEventApplicationService>();
                await service.ProcessBatchAsync(events, stoppingToken);

                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process RabbitMQ CMS event message.");
                channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ CMS event worker consuming queue {QueueName}", _options.QueueName);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
