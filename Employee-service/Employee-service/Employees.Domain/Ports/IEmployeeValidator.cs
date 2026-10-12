namespace Employee_service.Employees.Domain.Ports;

/// <summary>
/// Porta de entrada do domínio: contrato de validação para Employee.
/// Implementações concretas ficam na camada de infraestrutura/aplicação.
/// </summary>
public interface IEmployeeValidator
{
    /// <summary>
    /// Valida a entidade e retorna o resultado da validação.
    /// </summary>
    ValidationResult Validate(Employee employee);
}
