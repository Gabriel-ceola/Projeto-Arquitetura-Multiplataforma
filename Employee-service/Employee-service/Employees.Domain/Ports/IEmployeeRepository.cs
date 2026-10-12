namespace Employee_service.Employees.Domain.Ports;

/// <summary>
/// Porta de saída do domínio: contrato de persistência para Employee.
/// Implementações concretas ficam na camada de infraestrutura.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
