using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Observability;

public static class ObservabilityExtensions
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} (cid={CorrelationId}){NewLine}{Exception}";

    /// <summary>
    /// Troca o provedor de logging por Serilog: console legível + arquivo JSON ECS rolling diário
    /// em {Observability:LogDirectory}/{service}/. Os níveis continuam controlados por "Logging:LogLevel".
    /// </summary>
    public static WebApplicationBuilder AddOdontoLogging(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
                      ?? new ObservabilityOptions();
        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName)
            ? builder.Environment.ApplicationName
            : options.ServiceName;
        var logFile = BuildLogFilePath(options, serviceName, builder.Environment.ContentRootPath);
        var formatter = new EcsJsonFormatter(serviceName, builder.Environment.EnvironmentName);

        var logLevels = builder.Configuration.GetSection("Logging:LogLevel");

        builder.Services.AddSerilog(configuration =>
        {
            ApplyLogLevels(configuration, logLevels);
            configuration
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: ConsoleTemplate);

            if (logFile is not null)
            {
                configuration.WriteTo.File(
                    formatter,
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedFileCount,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }
        });

        return builder;
    }

    /// <summary>Adiciona o correlation id e o log de requisições HTTP. Deve vir antes de <c>MapControllers</c>.</summary>
    public static IApplicationBuilder UseOdontoObservability(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
            options.GetLevel = (context, _, exception) =>
                exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                : context.Response.StatusCode >= 400 ? LogEventLevel.Warning
                : LogEventLevel.Information);

        return app;
    }

    /// <summary>
    /// O Serilog substitui o ILoggerFactory, então o filtro de "Logging:LogLevel" precisa ser aplicado aqui:
    /// "Default" vira o nível mínimo e as demais chaves viram overrides por categoria.
    /// </summary>
    internal static void ApplyLogLevels(LoggerConfiguration configuration, IConfiguration logLevels)
    {
        configuration.MinimumLevel.Information();

        foreach (var entry in logLevels.GetChildren())
        {
            if (!Enum.TryParse<LogLevel>(entry.Value, ignoreCase: true, out var level))
                continue;

            var serilogLevel = ToSerilogLevel(level);
            if (entry.Key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                configuration.MinimumLevel.Is(serilogLevel);
            else
                configuration.MinimumLevel.Override(entry.Key, serilogLevel);
        }
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        _ => LogEventLevel.Fatal
    };

    internal static string? BuildLogFilePath(ObservabilityOptions options, string serviceName, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(options.LogDirectory))
            return null;

        var root = Path.GetFullPath(options.LogDirectory, contentRoot);
        return Path.Combine(root, serviceName, $"{serviceName}-.json");
    }
}
