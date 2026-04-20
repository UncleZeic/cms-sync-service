namespace CmsSyncService.Api.Messaging;

public sealed class RabbitMqOptions
{
    public bool Enabled { get; init; }

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string QueueName { get; init; } = "cms-events";
}
