using Employee_service.Employees.Domain;
using Employee_service.Employees.Domain.Ports;
using Employee_service.Employees.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Employee_service.Employees.Infrastructure.Repositories;

/// <summary>
/// Adaptador de saída: implementa a porta IEmployeeRepository usando EF Core + PostgreSQL.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly EmployeeDbContext _context;

    public EmployeeRepository(EmployeeDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Cpf == cpf, cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is not null)
        {
            _context.Employees.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
