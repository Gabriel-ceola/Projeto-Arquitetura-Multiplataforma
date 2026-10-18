using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Observability.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task DeveGerarCorrelationIdQuandoAusente()
    {
        var context = new DefaultHttpContext();

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        var id = context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Guid.TryParse(id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task DeveManterCorrelationIdValidoRecebido()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "teste-123";

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("teste-123");
    }

    [Theory]
    [InlineData("com espaco")]
    [InlineData("quebra\nde-linha")]
    [InlineData("<script>")]
    public void DeveSubstituirCorrelationIdInvalido(string recebido)
    {
        var resultado = CorrelationIdMiddleware.Resolve(recebido);

        resultado.Should().NotBe(recebido);
        Guid.TryParse(resultado, out _).Should().BeTrue();
    }

    [Fact]
    public void DeveSubstituirCorrelationIdMuitoLongo() =>
        CorrelationIdMiddleware.Resolve(new string('a', 129)).Should().HaveLength(36);

    [Fact]
    public async Task DeveIncluirCorrelationIdEUsuarioNosLogsDaRequisicao()
    {
        var sink = new ColetorDeEventos();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        var dentistId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "cid-1";
        context.Request.Headers[CorrelationIdMiddleware.DentistIdHeader] = dentistId.ToString();

        await new CorrelationIdMiddleware(_ =>
        {
            logger.Information("dentro da requisição");
            return Task.CompletedTask;
        }).InvokeAsync(context);

        var evento = sink.Eventos.Should().ContainSingle().Subject;
        ((ScalarValue)evento.Properties["CorrelationId"]).Value.Should().Be("cid-1");
        ((ScalarValue)evento.Properties["UserId"]).Value.Should().Be(dentistId.ToString());
    }

    [Fact]
    public async Task NaoDeveIncluirUsuarioQuandoHeaderDeIdentidadeForInvalido()
    {
        var sink = new ColetorDeEventos();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.PatientIdHeader] = "nao-e-guid";

        await new CorrelationIdMiddleware(_ =>
        {
            logger.Information("x");
            return Task.CompletedTask;
        }).InvokeAsync(context);

        sink.Eventos.Single().Properties.Should().NotContainKey("UserId");
    }

    private sealed class ColetorDeEventos : ILogEventSink
    {
        public List<LogEvent> Eventos { get; } = [];

        public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
    }
}
