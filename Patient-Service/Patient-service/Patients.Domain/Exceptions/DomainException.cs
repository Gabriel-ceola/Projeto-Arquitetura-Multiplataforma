namespace Patient_service.Patients.Domain.Exceptions;

/// <summary>
/// Exceção de domínio lançada quando uma regra de negócio é violada.
/// Usada para sinalizar conflito de dados (ex.: CPF já cadastrado).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
