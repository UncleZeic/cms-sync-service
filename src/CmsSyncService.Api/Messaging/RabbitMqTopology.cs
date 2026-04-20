using RabbitMQ.Client;

namespace CmsSyncService.Api.Messaging;

internal static class RabbitMqTopology
{
    private const string DeadLetterExchangeArgument = "x-dead-letter-exchange";
    private const string DeadLetterRoutingKeyArgument = "x-dead-letter-routing-key";
    private const string MessageTtlArgument = "x-message-ttl";

    public static string GetRetryQueueName(RabbitMqOptions options)
    {
        return string.IsNullOrWhiteSpace(options.RetryQueueName)
            ? $"{options.QueueName}.retry"
            : options.RetryQueueName;
    }

    public static string GetDeadLetterQueueName(RabbitMqOptions options)
    {
        return string.IsNullOrWhiteSpace(options.DeadLetterQueueName)
            ? $"{options.QueueName}.dead-letter"
            : options.DeadLetterQueueName;
    }

    public static void Declare(IModel channel, RabbitMqOptions options)
    {
        var retryQueueName = GetRetryQueueName(options);
        var deadLetterQueueName = GetDeadLetterQueueName(options);

        channel.QueueDeclare(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                [MessageTtlArgument] = options.RetryDelayMilliseconds,
                [DeadLetterExchangeArgument] = string.Empty,
                [DeadLetterRoutingKeyArgument] = options.QueueName
            });

        channel.QueueDeclare(
            queue: deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }
}
