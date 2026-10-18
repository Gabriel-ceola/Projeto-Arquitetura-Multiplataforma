using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace Observability;

/// <summary>
/// Serializa cada evento de log como uma linha JSON no padrão ECS (Elastic Common Schema).
/// Propriedades de negócio viram <c>labels.snake_case</c> (sempre string, evitando conflito de mapping);
/// o nome do EventId do ILogger vira <c>event.action</c>.
/// </summary>
public sealed class EcsJsonFormatter : ITextFormatter
{
    private const string EcsVersion = "8.11.0";

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Propriedades de infraestrutura do ASP.NET/Serilog sem valor para busca.
    private static readonly HashSet<string> Ignored = new(StringComparer.Ordinal)
    {
        "SourceContext", "EventId", "ActionId", "ActionName", "ConnectionId",
        "ParentId", "SpanId", "TraceId", "RequestId", "Scope", "{OriginalFormat}"
    };

    private readonly string _serviceName;
    private readonly string _environment;

    public EcsJsonFormatter(string serviceName, string environment)
    {
        _serviceName = serviceName;
        _environment = environment;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var fields = Extract(logEvent);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            Write(writer, logEvent, fields);
        }

        output.Write(Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length));
        output.Write('\n');
    }

    private void Write(Utf8JsonWriter writer, LogEvent logEvent, ExtractedFields fields)
    {
        writer.WriteStartObject();
        writer.WriteString("@timestamp", logEvent.Timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
        writer.WriteString("message", logEvent.RenderMessage(CultureInfo.InvariantCulture));

        writer.WriteStartObject("log");
        writer.WriteString("level", ToEcsLevel(logEvent.Level));
        if (fields.Logger is not null) writer.WriteString("logger", fields.Logger);
        writer.WriteEndObject();

        writer.WriteStartObject("ecs");
        writer.WriteString("version", EcsVersion);
        writer.WriteEndObject();

        writer.WriteStartObject("service");
        writer.WriteString("name", _serviceName);
        writer.WriteString("environment", _environment);
        writer.WriteEndObject();

        if (fields.Action is not null || fields.DurationNanos is not null)
        {
            writer.WriteStartObject("event");
            if (fields.Action is not null) writer.WriteString("action", fields.Action);
            if (fields.DurationNanos is not null) writer.WriteNumber("duration", fields.DurationNanos.Value);
            writer.WriteEndObject();
        }

        if (logEvent.TraceId is not null)
        {
            writer.WriteStartObject("trace");
            writer.WriteString("id", logEvent.TraceId.Value.ToHexString());
            writer.WriteEndObject();
        }

        if (logEvent.SpanId is not null)
        {
            writer.WriteStartObject("span");
            writer.WriteString("id", logEvent.SpanId.Value.ToHexString());
            writer.WriteEndObject();
        }

        if (fields.UserId is not null)
        {
            writer.WriteStartObject("user");
            writer.WriteString("id", fields.UserId);
            writer.WriteEndObject();
        }

        if (fields.HttpMethod is not null || fields.HttpStatus is not null)
        {
            writer.WriteStartObject("http");
            if (fields.HttpMethod is not null)
            {
                writer.WriteStartObject("request");
                writer.WriteString("method", fields.HttpMethod);
                writer.WriteEndObject();
            }

            if (fields.HttpStatus is not null)
            {
                writer.WriteStartObject("response");
                writer.WriteNumber("status_code", fields.HttpStatus.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        if (fields.UrlPath is not null)
        {
            writer.WriteStartObject("url");
            writer.WriteString("path", fields.UrlPath);
            writer.WriteEndObject();
        }

        if (logEvent.Exception is { } exception)
        {
            writer.WriteStartObject("error");
            writer.WriteString("type", exception.GetType().FullName);
            writer.WriteString("message", exception.Message);
            writer.WriteString("stack_trace", exception.ToString());
            writer.WriteEndObject();
        }

        if (fields.Labels.Count > 0)
        {
            writer.WriteStartObject("labels");
            foreach (var (name, value) in fields.Labels)
                writer.WriteString(name, value);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static ExtractedFields Extract(LogEvent logEvent)
    {
        var fields = new ExtractedFields();

        foreach (var (name, value) in logEvent.Properties)
        {
            switch (name)
            {
                case "SourceContext":
                    fields.Logger = Text(value);
                    break;
                case "EventId":
                    fields.Action = EventName(value);
                    break;
                case "UserId":
                    fields.UserId = Text(value);
                    break;
                case "RequestMethod":
                    fields.HttpMethod = Text(value);
                    break;
                case "RequestPath":
                    fields.UrlPath = Text(value);
                    break;
                case "StatusCode" when value is ScalarValue { Value: IConvertible code }:
                    fields.HttpStatus = code.ToInt32(CultureInfo.InvariantCulture);
                    break;
                case "Elapsed" when value is ScalarValue { Value: IConvertible elapsedMs }:
                    fields.DurationNanos = (long)(elapsedMs.ToDouble(CultureInfo.InvariantCulture) * 1_000_000);
                    break;
                default:
                    if (!Ignored.Contains(name) && Text(value) is { } text)
                        fields.Labels[ToSnakeCase(name)] = text;
                    break;
            }
        }

        return fields;
    }

    private static string? EventName(LogEventPropertyValue value) =>
        value is StructureValue structure
            ? structure.Properties.FirstOrDefault(p => p.Name == "Name")?.Value is ScalarValue { Value: string name } ? name : null
            : null;

    private static string? Text(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => null,
        ScalarValue { Value: string s } => s,
        ScalarValue { Value: IFormattable f } => f.ToString(null, CultureInfo.InvariantCulture),
        ScalarValue scalar => scalar.Value?.ToString(),
        _ => value.ToString()
    };

    private static string ToEcsLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "trace",
        LogEventLevel.Debug => "debug",
        LogEventLevel.Information => "info",
        LogEventLevel.Warning => "warn",
        LogEventLevel.Error => "error",
        _ => "fatal"
    };

    internal static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current))
            {
                var startsWord = i > 0 &&
                    (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) ||
                     (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1])));
                if (startsWord) builder.Append('_');
                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    private sealed class ExtractedFields
    {
        public string? Logger { get; set; }
        public string? Action { get; set; }
        public string? UserId { get; set; }
        public string? HttpMethod { get; set; }
        public string? UrlPath { get; set; }
        public int? HttpStatus { get; set; }
        public long? DurationNanos { get; set; }
        public SortedDictionary<string, string> Labels { get; } = new(StringComparer.Ordinal);
    }
}
