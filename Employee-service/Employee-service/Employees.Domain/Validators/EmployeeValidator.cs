using Employee_service.Employees.Domain.Ports;
using FluentValidation;

namespace Employee_service.Employees.Domain.Validators;

/// <summary>
/// Implementação concreta do validator de Employee usando FluentValidation.
/// Implementa a porta IEmployeeValidator definida no domínio.
/// </summary>
public class EmployeeValidator : AbstractValidator<Employee>, IEmployeeValidator
{
    public EmployeeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do funcionário é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter ao menos 3 caracteres.")
            .MaximumLength(100).WithMessage("O nome não pode ultrapassar 100 caracteres.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Matches(@"^\d{11}$").WithMessage("O CPF deve conter 11 dígitos.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O e-mail informado é inválido.")
            .MaximumLength(150).WithMessage("O e-mail não pode ultrapassar 150 caracteres.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("O telefone é obrigatório.")
            .Matches(@"^\d{10,11}$").WithMessage("O telefone deve conter 10 ou 11 dígitos (com DDD).");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Cargo inválido para o funcionário.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido para o funcionário.");
    }

    /// <summary>
    /// Valida a entidade e retorna o ValidationResult do domínio.
    /// </summary>
    public new ValidationResult Validate(Employee employee)
    {
        var fluentResult = base.Validate(employee);
        var result = new ValidationResult();

        foreach (var error in fluentResult.Errors)
            result.AddError(error.ErrorMessage);

        return result;
    }
}
