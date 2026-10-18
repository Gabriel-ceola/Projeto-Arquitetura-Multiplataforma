using Patient_service.Patients.Domain;
using Patient_service.Patients.Domain.Validators;
using Patient_service.Tests.Helpers;
using FluentAssertions;

namespace Patient_service.Tests.Domain;

/// <summary>
/// Testes unitários para PatientValidator.
/// Cobre: entidade válida, nome, CPF, e-mail, telefone, data de nascimento, convênio e status.
/// </summary>
public class PatientValidatorTests
{
    private readonly PatientValidator _validator = new();

    // ──────────────────────────────────────────
    // Cenário válido
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenEntityIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var patient = PatientFactory.Create();

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenInsuranceIsComplete_ShouldReturnSuccess()
    {
        // Arrange
        var patient = PatientFactory.Create(insuranceName: "Unimed", insuranceCardNumber: "0001234");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ──────────────────────────────────────────
    // Name
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(name: "");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nome"));
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(name: new string('A', 101));

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("100 caracteres"));
    }

    // ──────────────────────────────────────────
    // CPF / Email / Phone
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("9876543210")]
    [InlineData("987654321001")]
    public void Validate_WhenCpfDoesNotHave11Digits_ShouldReturnError(string cpf)
    {
        // Arrange
        var patient = PatientFactory.Create(cpf: cpf);

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("11 dígitos"));
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(email: "email-invalido");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("inválido"));
    }

    [Fact]
    public void Validate_WhenPhoneHasLessThan10Digits_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(phone: "988887777");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DDD"));
    }

    // ──────────────────────────────────────────
    // BirthDate
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenBirthDateIsInTheFuture_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(birthDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nascimento"));
    }

    [Fact]
    public void Validate_WhenBirthDateIsBefore1900_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(birthDate: new DateOnly(1899, 12, 31));

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nascimento"));
    }

    // ──────────────────────────────────────────
    // Convênio
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenOnlyInsuranceNameIsInformed_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(insuranceName: "Unimed");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("carteirinha"));
    }

    [Fact]
    public void Validate_WhenOnlyInsuranceCardNumberIsInformed_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(insuranceCardNumber: "0001234");

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nome do convênio"));
    }

    // ──────────────────────────────────────────
    // Status
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenStatusIsInvalid_ShouldReturnError()
    {
        // Arrange
        var patient = PatientFactory.Create(status: (PatientStatus)99);

        // Act
        var result = _validator.Validate(patient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Status"));
    }
}
