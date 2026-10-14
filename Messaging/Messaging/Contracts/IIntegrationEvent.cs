namespace Messaging.Contracts;

public interface IIntegrationEvent
{
    static abstract string Topic { get; }
    static abstract string EventType { get; }
    static abstract int EventVersion { get; }
    string Key { get; }
}
