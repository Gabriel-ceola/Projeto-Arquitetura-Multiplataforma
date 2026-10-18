using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Observability.Tests;

public class ObservabilityExtensionsTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "app"));

    private static ILogger BuildLogger(Dictionary<string, string?> logLevels)
    {
        var section = new ConfigurationBuilder().AddInMemoryCollection(logLevels).Build();
        var configuration = new LoggerConfiguration();
        ObservabilityExtensions.ApplyLogLevels(configuration, section);
        return configuration.CreateLogger();
    }

    [Fact]
    public void DeveAplicarNivelDefaultDoLoggingLogLevel()
    {
        var logger = BuildLogger(new() { ["Default"] = "Warning" });

        logger.IsEnabled(LogEventLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogEventLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public void DeveAplicarOverridePorCategoria()
    {
        var logger = BuildLogger(new()
        {
            ["Default"] = "Information",
            ["Microsoft.AspNetCore"] = "Warning"
        });

        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Routing.EndpointMiddleware")
            .IsEnabled(LogEventLevel.Information).Should().BeFalse();
        logger.ForContext("SourceContext", "Dentist_service.Controller")
            .IsEnabled(LogEventLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void NaoDeveHabilitarDebugQuandoNaoConfigurado()
    {
        var logger = BuildLogger([]);

        logger.IsEnabled(LogEventLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogEventLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void NaoDeveGravarArquivoQuandoLogDirectoryVazio() =>
        ObservabilityExtensions.BuildLogFilePath(new ObservabilityOptions(), "svc", Root).Should().BeNull();

    [Fact]
    public void DeveResolverCaminhoRelativoAPartirDaRaizDaAplicacao()
    {
        var options = new ObservabilityOptions { LogDirectory = "../../logs" };

        var path = ObservabilityExtensions.BuildLogFilePath(options, "dentist-service", Root);

        path.Should().Be(Path.GetFullPath(Path.Combine(Root, "..", "..", "logs", "dentist-service", "dentist-service-.json")));
    }

    [Fact]
    public void DeveManterCaminhoAbsoluto()
    {
        var absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "logs"));
        var options = new ObservabilityOptions { LogDirectory = absolute };

        var path = ObservabilityExtensions.BuildLogFilePath(options, "svc", Root);

        path.Should().Be(Path.Combine(absolute, "svc", "svc-.json"));
    }
}
