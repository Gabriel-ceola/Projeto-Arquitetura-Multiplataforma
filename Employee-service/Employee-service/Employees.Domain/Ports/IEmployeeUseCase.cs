using Employee_service.Employees.Application.DTOs;

namespace Employee_service.Employees.Domain.Ports;

/// <summary>
/// Porta de entrada do domínio: contrato dos casos de uso para Employee.
/// Cada método corresponde a um componente do modelo C4 (CreateEmployee, EditEmployee,
/// GetEmployees, GetEmployeeById e RemoveEmployee).
/// Implementações concretas ficam na camada de aplicação.
/// </summary>
public interface IEmployeeUseCase
{
    /// <summary>GetEmployeeById: busca os dados de um funcionário específico.</summary>
    Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>GetEmployees: lista todos os funcionários da clínica.</summary>
    Task<IEnumerable<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>CreateEmployee: cadastra um novo funcionário.</summary>
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>EditEmployee: edita os dados de um funcionário.</summary>
    Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>RemoveEmployee: remove um funcionário da clínica.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
