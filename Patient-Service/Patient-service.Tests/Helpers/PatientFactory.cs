using Patient_service.Patients.Domain;

namespace Patient_service.Tests.Helpers;

/// <summary>
/// Factory centralizada para construção de entidades Patient nos testes.
/// Evita duplicação de código de setup e garante valores padrão consistentes.
/// </summary>
public static class PatientFactory
{
    public static readonly Guid     DefaultId        = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public const string             DefaultCpf       = "98765432100";
    public const string             DefaultEmail     = "carlos.pereira@email.com";
    public const string             DefaultPhone     = "43988887777";
    public static readonly DateOnly DefaultBirthDate = new(1990, 5, 20);

    /// <summary>
    /// Cria uma entidade válida com os valores padrão ou sobrescrevendo os campos desejados.
    /// </summary>
    public static Patient Create(
        Guid?         id                  = null,
        string?       name                = null,
        string?       cpf                 = null,
        string?       email               = null,
        string?       phone               = null,
        DateOnly?     birthDate           = null,
        PatientStatus status              = PatientStatus.ativo,
        string?       insuranceName       = null,
        string?       insuranceCardNumber = null)
    {
        return new Patient(
            id:                  id        ?? DefaultId,
            name:                name      ?? "Carlos Pereira",
            cpf:                 cpf       ?? DefaultCpf,
            email:               email     ?? DefaultEmail,
            phone:               phone     ?? DefaultPhone,
            birthDate:           birthDate ?? DefaultBirthDate,
            status:              status,
            insuranceName:       insuranceName,
            insuranceCardNumber: insuranceCardNumber);
    }
}
