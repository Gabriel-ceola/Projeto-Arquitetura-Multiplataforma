namespace Employee_service.Employees.Domain;

/// <summary>
/// Entidade de domínio que representa um funcionário da clínica.
/// Contém os value objects EmployeeRole (cargo) e EmployeeStatus (situação).
/// </summary>
public class Employee
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public EmployeeRole Role { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Construtor protegido exigido pelo EF Core para materialização de entidades.
    /// Não usar diretamente no código de aplicação.
    /// </summary>
    protected Employee() { }

    public Employee(
        Guid id,
        string name,
        string cpf,
        string email,
        string phone,
        EmployeeRole role,
        EmployeeStatus status)
    {
        Id = id;
        Name = NormalizeText(name);
        Cpf = OnlyDigits(cpf);
        Email = NormalizeEmail(email);
        Phone = OnlyDigits(phone);
        Role = role;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza os dados mutáveis do funcionário, preservando Id, CPF e CreatedAt.
    /// O CPF identifica o funcionário e não pode ser alterado.
    /// </summary>
    public void Update(string name, string email, string phone, EmployeeRole role, EmployeeStatus status)
    {
        Name = NormalizeText(name);
        Email = NormalizeEmail(email);
        Phone = OnlyDigits(phone);
        Role = role;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o funcionário como deletado (soft delete).
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
    /// Mantém apenas os dígitos (ex.: "123.456.789-01" → "12345678901").
    /// </summary>
    private static string OnlyDigits(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
