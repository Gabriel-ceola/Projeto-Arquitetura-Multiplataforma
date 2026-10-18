using System.Text.Json;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;

namespace Observability.Tests;

public class EcsJsonFormatterTests
{
    private readonly EcsJsonFormatter _formatter = new("dentist-service", "Development");

    [Fact]
    public void DeveGravarCamposObrigatoriosEcs()
    {
        var logEvent = CriarEvento(LogEventLevel.Information, "Agendamento criado");

        var json = Formatar(logEvent);

        json.GetProperty("@timestamp").GetString().Should().EndWith("Z");
        json.GetProperty("message").GetString().Should().Be("Agendamento criado");
        json.GetProperty("log").GetProperty("level").GetString().Should().Be("info");
        json.GetProperty("service").GetProperty("name").GetString().Should().Be("dentist-service");
        json.GetProperty("service").GetProperty("environment").GetString().Should().Be("Development");
        json.GetProperty("ecs").GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, "trace")]
    [InlineData(LogEventLevel.Debug, "debug")]
    [InlineData(LogEventLevel.Information, "info")]
    [InlineData(LogEventLevel.Warning, "warn")]
    [InlineData(LogEventLevel.Error, "error")]
    [InlineData(LogEventLevel.Fatal, "fatal")]
    public void DeveConverterNivelParaPadraoEcs(LogEventLevel nivel, string esperado)
    {
        var json = Formatar(CriarEvento(nivel, "x"));

        json.GetProperty("log").GetProperty("level").GetString().Should().Be(esperado);
    }

    [Fact]
    public void DeveUsarNomeDoEventIdComoEventAction()
    {
        var eventId = new StructureValue(
        [
            new LogEventProperty("Id", new ScalarValue(1001)),
            new LogEventProperty("Name", new ScalarValue("appointment.requested"))
        ]);

        var json = Formatar(CriarEvento(LogEventLevel.Information, "x", ("EventId", eventId)));

        json.GetProperty("event").GetProperty("action").GetString().Should().Be("appointment.requested");
    }

    [Fact]
    public void DeveGravarPropriedadesDeNegocioComoLabelsEmSnakeCaseTextual()
    {
        var appointmentId = Guid.NewGuid();

        var json = Formatar(CriarEvento(
            LogEventLevel.Information, "x",
            ("AppointmentId", new ScalarValue(appointmentId)),
            ("PriceCents", new ScalarValue(15000)),
            ("CorrelationId", new ScalarValue("abc-123"))));

        var labels = json.GetProperty("labels");
        labels.GetProperty("appointment_id").GetString().Should().Be(appointmentId.ToString());
        labels.GetProperty("price_cents").ValueKind.Should().Be(JsonValueKind.String);
        labels.GetProperty("price_cents").GetString().Should().Be("15000");
        labels.GetProperty("correlation_id").GetString().Should().Be("abc-123");
    }

    [Fact]
    public void DeveIgnorarPropriedadesDeInfraestrutura()
    {
        var json = Formatar(CriarEvento(
            LogEventLevel.Information, "x",
            ("ConnectionId", new ScalarValue("0HN")),
            ("RequestId", new ScalarValue("req")),
            ("ActionName", new ScalarValue("Create"))));

        json.TryGetProperty("labels", out _).Should().BeFalse();
    }

    [Fact]
    public void DeveMapearCamposDeRequisicaoHttp()
    {
        var json = Formatar(CriarEvento(
            LogEventLevel.Warning, "x",
            ("RequestMethod", new ScalarValue("POST")),
            ("RequestPath", new ScalarValue("/api/appointments")),
            ("StatusCode", new ScalarValue(409)),
            ("Elapsed", new ScalarValue(12.5d)),
            ("UserId", new ScalarValue("u-1"))));

        json.GetProperty("http").GetProperty("request").GetProperty("method").GetString().Should().Be("POST");
        json.GetProperty("http").GetProperty("response").GetProperty("status_code").GetInt32().Should().Be(409);
        json.GetProperty("url").GetProperty("path").GetString().Should().Be("/api/appointments");
        json.GetProperty("event").GetProperty("duration").GetInt64().Should().Be(12_500_000);
        json.GetProperty("user").GetProperty("id").GetString().Should().Be("u-1");
    }

    [Fact]
    public void DeveGravarDadosDaExcecao()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow, LogEventLevel.Error, new InvalidOperationException("falhou"),
            Template("erro"), []);

        var json = Formatar(logEvent);

        var error = json.GetProperty("error");
        error.GetProperty("type").GetString().Should().Be(typeof(InvalidOperationException).FullName);
        error.GetProperty("message").GetString().Should().Be("falhou");
        error.GetProperty("stack_trace").GetString().Should().Contain("falhou");
    }

    [Fact]
    public void DeveGerarExatamenteUmaLinhaPorEvento()
    {
        var logEvent = CriarEvento(LogEventLevel.Information, "linha1\nlinha2");
        using var writer = new StringWriter();

        _formatter.Format(logEvent, writer);

        writer.ToString().TrimEnd('\n').Should().NotContain("\n");
        writer.ToString().Should().EndWith("\n");
    }

    [Theory]
    [InlineData("AppointmentId", "appointment_id")]
    [InlineData("DentistId", "dentist_id")]
    [InlineData("KafkaTopic", "kafka_topic")]
    [InlineData("HTTPStatus", "http_status")]
    [InlineData("Id", "id")]
    public void DeveConverterParaSnakeCase(string entrada, string esperado) =>
        EcsJsonFormatter.ToSnakeCase(entrada).Should().Be(esperado);

    private JsonElement Formatar(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        return JsonDocument.Parse(writer.ToString()).RootElement.Clone();
    }

    private static LogEvent CriarEvento(LogEventLevel nivel, string mensagem, params (string Nome, LogEventPropertyValue Valor)[] propriedades) =>
        new(
            DateTimeOffset.UtcNow, nivel, null, Template(mensagem),
            propriedades.Select(p => new LogEventProperty(p.Nome, p.Valor)));

    private static MessageTemplate Template(string texto) => new(texto, [new TextToken(texto)]);
}
