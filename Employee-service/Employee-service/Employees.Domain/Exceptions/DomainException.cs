namespace Employee_service.Employees.Domain.Exceptions;

/// <summary>
/// Exceção de domínio lançada quando uma regra de negócio é violada.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
