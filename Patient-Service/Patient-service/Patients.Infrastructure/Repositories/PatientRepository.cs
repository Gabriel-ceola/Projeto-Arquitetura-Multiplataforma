using Patient_service.Patients.Domain;
using Patient_service.Patients.Domain.Ports;
using Patient_service.Patients.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Patient_service.Patients.Infrastructure.Repositories;

/// <summary>
/// Adaptador de saída: implementa a porta IPatientRepository usando EF Core + PostgreSQL.
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly PatientDbContext _context;

    public PatientRepository(PatientDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Cpf == cpf, cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _context.Patients.AddAsync(patient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is not null)
        {
            _context.Patients.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
