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

        RabbitMqTopology.Declare(channel, _options);
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
                RepublishFailedMessage(channel, args);
                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
        };

        channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ CMS event worker consuming queue {QueueName}", _options.QueueName);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private void RepublishFailedMessage(IModel channel, BasicDeliverEventArgs args)
    {
        var retryCount = GetRetryCount(args.BasicProperties) + 1;
        var targetQueue = retryCount > _options.MaxRetryAttempts
            ? RabbitMqTopology.GetDeadLetterQueueName(_options)
            : RabbitMqTopology.GetRetryQueueName(_options);

        var properties = channel.CreateBasicProperties();
        properties.ContentType = args.BasicProperties.ContentType;
        properties.DeliveryMode = 2;
        properties.Headers = CopyHeaders(args.BasicProperties);
        properties.Headers["x-retry-count"] = retryCount;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: targetQueue,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);

        if (retryCount > _options.MaxRetryAttempts)
        {
            _logger.LogError(
                "Moved RabbitMQ CMS event message to dead-letter queue {DeadLetterQueueName} after {RetryCount} failed attempts.",
                targetQueue,
                retryCount);
        }
        else
        {
            _logger.LogWarning(
                "Moved RabbitMQ CMS event message to retry queue {RetryQueueName}. Attempt {RetryCount} of {MaxRetryAttempts}.",
                targetQueue,
                retryCount,
                _options.MaxRetryAttempts);
        }
    }

    private static Dictionary<string, object> CopyHeaders(IBasicProperties properties)
    {
        return properties.Headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(properties.Headers);
    }

    private static int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return 0;
        }

        return value switch
        {
            int retryCount => retryCount,
            long retryCount => checked((int)retryCount),
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var retryCount) => retryCount,
            _ => 0
        };
    }
}
