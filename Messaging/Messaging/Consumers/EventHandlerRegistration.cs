using System.Text.Json;
using Messaging.Contracts;

namespace Messaging.Consumers;

public sealed record EventHandlerRegistration(
    string EventType,
    string Topic,
    Func<IServiceProvider, JsonElement, EventMetadata, CancellationToken, Task> InvokeAsync);
