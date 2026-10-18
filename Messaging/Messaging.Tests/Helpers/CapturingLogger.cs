using Microsoft.Extensions.Logging;

namespace Messaging.Tests.Helpers;

public sealed record CapturedLog(
    LogLevel Level,
    string? EventName,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties);

/// <summary>Logger de teste que guarda os registros com as propriedades da mensagem e dos escopos ativos.</summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<IReadOnlyDictionary<string, object?>> _scopes = [];

    public List<CapturedLog> Logs { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var properties = ToProperties(state);
        _scopes.Add(properties);
        return new ScopeRemoval(() => _scopes.Remove(properties));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = new Dictionary<string, object?>();
        foreach (var scope in _scopes)
            foreach (var pair in scope)
                properties[pair.Key] = pair.Value;
        foreach (var pair in ToProperties(state))
            properties[pair.Key] = pair.Value;

        Logs.Add(new CapturedLog(logLevel, eventId.Name, formatter(state, exception), exception, properties));
    }

    private static Dictionary<string, object?> ToProperties<TState>(TState state) =>
        state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(p => p.Key, p => p.Value)
            : [];

    private sealed class ScopeRemoval(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }
}
