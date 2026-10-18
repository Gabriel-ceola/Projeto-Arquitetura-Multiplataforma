using Patient_service.Patients.Application.DTOs;
using Patient_service.Patients.Domain;
using Patient_service.Patients.Domain.Exceptions;
using Patient_service.Patients.Domain.Ports;

namespace Patient_service.Patients.Application.UseCases;

/// <summary>
/// Application Service: implementa os casos de uso de Patient.
/// Depende apenas das portas definidas no domínio (hexagonal).
/// </summary>
public class PatientUseCase : IPatientUseCase
{
    private readonly IPatientRepository _repository;
    private readonly IPatientValidator _validator;

    public PatientUseCase(
        IPatientRepository repository,
        IPatientValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    // ──────────────────────────────────────────
    // Leitura
    // ──────────────────────────────────────────

    public async Task<PatientResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Paciente com id '{id}' não encontrado.");

        return ToResponse(entity);
    }

    public async Task<IEnumerable<PatientResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(ToResponse);
    }

    // ──────────────────────────────────────────
    // Escrita
    // ──────────────────────────────────────────

    public async Task<PatientResponse> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Patient(
            id: Guid.NewGuid(),
            name: request.Name,
            cpf: request.Cpf,
            email: request.Email,
            phone: request.Phone,
            birthDate: request.BirthDate,
            status: request.Status,
            insuranceName: request.InsuranceName,
            insuranceCardNumber: request.InsuranceCardNumber);

        var validationResult = _validator.Validate(entity);
        if (!validationResult.IsValid)
            throw new ArgumentException(string.Join("; ", validationResult.Errors));

        // Regra de negócio: não pode haver dois pacientes com o mesmo CPF
        if (await _repository.ExistsByCpfAsync(entity.Cpf, cancellationToken))
            throw new DomainException("Já existe um paciente cadastrado com o CPF informado.");

        await _repository.AddAsync(entity, cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PatientResponse> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Paciente com id '{id}' não encontrado.");

        entity.Update(
            request.Name,
            request.Email,
            request.Phone,
            request.BirthDate,
            request.Status,
            request.InsuranceName,
            request.InsuranceCardNumber);

        var validationResult = _validator.Validate(entity);
        if (!validationResult.IsValid)
            throw new ArgumentException(string.Join("; ", validationResult.Errors));

        await _repository.UpdateAsync(entity, cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Paciente com id '{id}' não encontrado.");

        await _repository.DeleteAsync(entity.Id, cancellationToken);
    }

    // ──────────────────────────────────────────
    // Mapeamento
    // ──────────────────────────────────────────

    private static PatientResponse ToResponse(Patient entity) =>
        new(entity.Id,
            entity.Name,
            entity.Cpf,
            entity.Email,
            entity.Phone,
            entity.BirthDate,
            entity.InsuranceName,
            entity.InsuranceCardNumber,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);
}
