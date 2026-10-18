namespace Patient_service.Patients.Domain;

/// <summary>
/// Entidade de domínio que representa um paciente da clínica.
/// Contém o value object PatientStatus (situação) e os dados opcionais de convênio.
/// </summary>
public class Patient
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public string? InsuranceName { get; private set; }
    public string? InsuranceCardNumber { get; private set; }
    public PatientStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Construtor protegido exigido pelo EF Core para materialização de entidades.
    /// Não usar diretamente no código de aplicação.
    /// </summary>
    protected Patient() { }

    public Patient(
        Guid id,
        string name,
        string cpf,
        string email,
        string phone,
        DateOnly birthDate,
        PatientStatus status,
        string? insuranceName = null,
        string? insuranceCardNumber = null)
    {
        Id = id;
        Name = NormalizeText(name);
        Cpf = OnlyDigits(cpf);
        Email = NormalizeEmail(email);
        Phone = OnlyDigits(phone);
        BirthDate = birthDate;
        Status = status;
        InsuranceName = NormalizeOptional(insuranceName);
        InsuranceCardNumber = NormalizeOptional(insuranceCardNumber);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza os dados mutáveis do paciente, preservando Id, CPF e CreatedAt.
    /// O CPF identifica o paciente e não pode ser alterado.
    /// Convênio: null (não informado) mantém o valor atual; texto em branco remove.
    /// </summary>
    public void Update(
        string name,
        string email,
        string phone,
        DateOnly birthDate,
        PatientStatus status,
        string? insuranceName = null,
        string? insuranceCardNumber = null)
    {
        Name = NormalizeText(name);
        Email = NormalizeEmail(email);
        Phone = OnlyDigits(phone);
        BirthDate = birthDate;
        Status = status;
        if (insuranceName is not null)
            InsuranceName = NormalizeOptional(insuranceName);
        if (insuranceCardNumber is not null)
            InsuranceCardNumber = NormalizeOptional(insuranceCardNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Indica se o paciente informou um convênio (nome e número da carteirinha).
    /// </summary>
    public bool HasInsurance => InsuranceName is not null && InsuranceCardNumber is not null;

    /// <summary>
    /// Marca o paciente como deletado (soft delete).
    /// </summary>
    public void MarkAsDeleted()
    {
        DeletedAt = DateTime.UtcNow;
    }

    // ──────────────────────────────────────────
    // Normalização
    // ──────────────────────────────────────────

    private static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

    private static string NormalizeEmail(string? value) => NormalizeText(value).ToLowerInvariant();

    /// <summary>
    /// Texto opcional: espaços extras são removidos e valores em branco viram null.
    /// </summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Mantém apenas os dígitos (ex.: "123.456.789-01" → "12345678901").
    /// </summary>
    private static string OnlyDigits(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
