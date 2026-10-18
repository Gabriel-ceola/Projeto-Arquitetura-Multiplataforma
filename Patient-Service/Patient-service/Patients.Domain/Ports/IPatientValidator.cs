namespace Patient_service.Patients.Domain.Ports;

/// <summary>
/// Porta de entrada do domínio: contrato de validação para Patient.
/// Implementações concretas ficam na camada de infraestrutura/aplicação.
/// </summary>
public interface IPatientValidator
{
    /// <summary>
    /// Valida a entidade e retorna o resultado da validação.
    /// </summary>
    ValidationResult Validate(Patient patient);
}
