using Patient_service.Patients.Application.DTOs;

namespace Patient_service.Patients.Domain.Ports;

/// <summary>
/// Porta de entrada do domínio: contrato dos casos de uso para Patient.
/// Implementações concretas ficam na camada de aplicação.
/// </summary>
public interface IPatientUseCase
{
    /// <summary>Busca os dados de um paciente específico.</summary>
    Task<PatientResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os pacientes da clínica.</summary>
    Task<IEnumerable<PatientResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Cadastra um novo paciente.</summary>
    Task<PatientResponse> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edita os dados de um paciente.</summary>
    Task<PatientResponse> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);

    /// <summary>Remove um paciente da clínica.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
