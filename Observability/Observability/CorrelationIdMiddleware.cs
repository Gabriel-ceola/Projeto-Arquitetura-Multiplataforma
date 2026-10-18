using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Observability;

/// <summary>
/// Garante um <c>x-correlation-id</c> por requisição: reaproveita o recebido (se válido) ou gera um novo,
/// devolve no response, grava no request (o outbox da lib Messaging o lê dali) e o inclui em todos os logs.
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "x-correlation-id";
    public const string DentistIdHeader = "X-Dentist-Id";
    public const string PatientIdHeader = "X-Patient-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context.Request.Headers[HeaderName].ToString());
        var userId = ResolveUserId(context.Request);

        context.Request.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var userScope = userId is null ? null : LogContext.PushProperty("UserId", userId);

        await _next(context);
    }

    internal static string Resolve(string? incoming) =>
        !string.IsNullOrWhiteSpace(incoming) && ValidId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString();

    private static string? ResolveUserId(HttpRequest request)
    {
        foreach (var header in new[] { DentistIdHeader, PatientIdHeader })
        {
            if (Guid.TryParse(request.Headers[header].ToString(), out var id))
                return id.ToString();
        }

        return null;
    }

    // Restringe o formato para impedir injeção de quebras de linha/ruído nos logs.
    [GeneratedRegex("^[A-Za-z0-9_.:-]{1,128}$")]
    private static partial Regex ValidId();
}
