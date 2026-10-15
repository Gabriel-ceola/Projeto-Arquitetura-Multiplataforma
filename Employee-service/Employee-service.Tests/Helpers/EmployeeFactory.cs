using Employee_service.Employees.Domain;

namespace Employee_service.Tests.Helpers;

/// <summary>
/// Factory centralizada para construção de entidades Employee nos testes.
/// Evita duplicação de código de setup e garante valores padrão consistentes.
/// </summary>
public static class EmployeeFactory
{
    public static readonly Guid DefaultId    = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public const string         DefaultCpf   = "12345678901";
    public const string         DefaultEmail = "maria.souza@clinica.com";
    public const string         DefaultPhone = "43999998888";

    /// <summary>
    /// Cria uma entidade válida com os valores padrão ou sobrescrevendo os campos desejados.
    /// </summary>
    public static Employee Create(
        Guid?          id     = null,
        string?        name   = null,
        string?        cpf    = null,
        string?        email  = null,
        string?        phone  = null,
        EmployeeRole   role   = EmployeeRole.recepcionista,
        EmployeeStatus status = EmployeeStatus.ativo)
    {
        return new Employee(
            id:     id    ?? DefaultId,
            name:   name  ?? "Maria Souza",
            cpf:    cpf   ?? DefaultCpf,
            email:  email ?? DefaultEmail,
            phone:  phone ?? DefaultPhone,
            role:   role,
            status: status);
    }
}
