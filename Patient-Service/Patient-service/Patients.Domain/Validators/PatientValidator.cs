using Patient_service.Patients.Domain.Ports;
using FluentValidation;

namespace Patient_service.Patients.Domain.Validators;

/// <summary>
/// Implementação concreta do validator de Patient usando FluentValidation.
/// Implementa a porta IPatientValidator definida no domínio.
/// </summary>
public class PatientValidator : AbstractValidator<Patient>, IPatientValidator
{
    private static readonly DateOnly MinBirthDate = new(1900, 1, 1);

    public PatientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do paciente é obrigatório.")
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

        RuleFor(x => x.BirthDate)
            .Must(date => date >= MinBirthDate && date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A data de nascimento deve estar entre 01/01/1900 e hoje.");

        // Convênio é opcional, mas nome e carteirinha andam juntos (mesma regra do agendamento)
        RuleFor(x => x.InsuranceName)
            .NotEmpty().When(x => x.InsuranceCardNumber is not null)
            .WithMessage("Informe o nome do convênio junto com o número da carteirinha.")
            .MaximumLength(120).WithMessage("O nome do convênio não pode ultrapassar 120 caracteres.");

        RuleFor(x => x.InsuranceCardNumber)
            .NotEmpty().When(x => x.InsuranceName is not null)
            .WithMessage("Informe o número da carteirinha junto com o nome do convênio.")
            .MaximumLength(80).WithMessage("O número da carteirinha não pode ultrapassar 80 caracteres.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido para o paciente.");
    }

    /// <summary>
    /// Valida a entidade e retorna o ValidationResult do domínio.
    /// </summary>
    public new ValidationResult Validate(Patient patient)
    {
        var fluentResult = base.Validate(patient);
        var result = new ValidationResult();

        foreach (var error in fluentResult.Errors)
            result.AddError(error.ErrorMessage);

        return result;
    }
}
