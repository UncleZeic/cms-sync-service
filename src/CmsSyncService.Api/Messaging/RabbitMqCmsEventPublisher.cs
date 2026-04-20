using System.Text;
using System.Text.Json;
using CmsSyncService.Application.DTOs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CmsSyncService.Api.Messaging;

public sealed class RabbitMqCmsEventPublisher : ICmsEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;

    public RabbitMqCmsEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public bool PublishesAsynchronously => true;

    public Task PublishBatchAsync(IReadOnlyCollection<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var factory = CreateConnectionFactory(_options);
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        RabbitMqTopology.Declare(channel, _options);

        var payload = JsonSerializer.Serialize(events, SerializerOptions);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    internal static ConnectionFactory CreateConnectionFactory(RabbitMqOptions options)
    {
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            VirtualHost = options.VirtualHost,
            UserName = options.UserName,
            Password = options.Password,
            DispatchConsumersAsync = true
        };
    }
}
