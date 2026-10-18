namespace Patient_service.Patients.Domain.Ports;

/// <summary>
/// Porta de saída do domínio: contrato de persistência para Patient.
/// Implementações concretas ficam na camada de infraestrutura.
/// </summary>
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Patient>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
