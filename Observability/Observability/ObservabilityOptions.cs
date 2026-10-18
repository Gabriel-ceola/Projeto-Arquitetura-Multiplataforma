namespace Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Valor de <c>service.name</c> nos logs. Padrão: nome da aplicação.</summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Pasta raiz dos logs JSON (relativa à raiz do conteúdo da aplicação ou absoluta).
    /// Vazio desativa a gravação em arquivo; o console continua ativo.
    /// Os logs ficam em {LogDirectory}/{ServiceName}/.
    /// </summary>
    public string? LogDirectory { get; set; }

    public int RetainedFileCount { get; set; } = 3;
}
