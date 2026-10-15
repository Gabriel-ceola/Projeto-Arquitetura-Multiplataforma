using Employee_service.Employees.Application.DTOs;
using Employee_service.Employees.Domain;
using Employee_service.Employees.Domain.Exceptions;
using Employee_service.Employees.Domain.Ports;

namespace Employee_service.Employees.Application.UseCases;

/// <summary>
/// Application Service: implementa os casos de uso de Employee.
/// Depende apenas das portas definidas no domínio (hexagonal).
/// </summary>
public class EmployeeUseCase : IEmployeeUseCase
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeValidator _validator;

    public EmployeeUseCase(
        IEmployeeRepository repository,
        IEmployeeValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    // ──────────────────────────────────────────
    // Leitura
    // ──────────────────────────────────────────

    public async Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Funcionário com id '{id}' não encontrado.");

        return ToResponse(entity);
    }

    public async Task<IEnumerable<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(ToResponse);
    }

    // ──────────────────────────────────────────
    // Escrita
    // ──────────────────────────────────────────

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Employee(
            id: Guid.NewGuid(),
            name: request.Name,
            cpf: request.Cpf,
            email: request.Email,
            phone: request.Phone,
            role: request.Role,
            status: request.Status);

        var validationResult = _validator.Validate(entity);
        if (!validationResult.IsValid)
            throw new ArgumentException(string.Join("; ", validationResult.Errors));

        // Regra de negócio: não pode haver dois funcionários com o mesmo CPF
        if (await _repository.ExistsByCpfAsync(entity.Cpf, cancellationToken))
            throw new DomainException("Já existe um funcionário cadastrado com o CPF informado.");

        await _repository.AddAsync(entity, cancellationToken);
        return ToResponse(entity);
    }

    public async Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Funcionário com id '{id}' não encontrado.");

        entity.Update(request.Name, request.Email, request.Phone, request.Role, request.Status);

        var validationResult = _validator.Validate(entity);
        if (!validationResult.IsValid)
            throw new ArgumentException(string.Join("; ", validationResult.Errors));

        await _repository.UpdateAsync(entity, cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Funcionário com id '{id}' não encontrado.");

        await _repository.DeleteAsync(entity.Id, cancellationToken);
    }

    // ──────────────────────────────────────────
    // Mapeamento
    // ──────────────────────────────────────────

    private static EmployeeResponse ToResponse(Employee entity) =>
        new(entity.Id,
            entity.Name,
            entity.Cpf,
            entity.Email,
            entity.Phone,
            entity.Role,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);
}
