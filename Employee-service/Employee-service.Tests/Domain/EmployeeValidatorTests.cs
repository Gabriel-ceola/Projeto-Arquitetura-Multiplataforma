using Employee_service.Employees.Domain;
using Employee_service.Employees.Domain.Validators;
using Employee_service.Tests.Helpers;
using FluentAssertions;

namespace Employee_service.Tests.Domain;

/// <summary>
/// Testes unitários para EmployeeValidator.
/// Cobre: entidade válida, nome, CPF, e-mail, telefone, cargo e status inválidos.
/// </summary>
public class EmployeeValidatorTests
{
    private readonly EmployeeValidator _validator = new();

    // ──────────────────────────────────────────
    // Cenário válido
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenEntityIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var employee = EmployeeFactory.Create();

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenCpfAndPhoneAreFormatted_ShouldReturnSuccess()
    {
        // Arrange
        var employee = EmployeeFactory.Create(cpf: "123.456.789-01", phone: "(43) 99999-8888");

        // Act
        var result = _validator.Validate(employee);

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
        var employee = EmployeeFactory.Create(name: "");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nome"));
    }

    [Fact]
    public void Validate_WhenNameIsTooShort_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(name: "Al");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("3 caracteres"));
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(name: new string('A', 101));

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("100 caracteres"));
    }

    // ──────────────────────────────────────────
    // CPF
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenCpfIsEmpty_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(cpf: "");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("CPF"));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    public void Validate_WhenCpfDoesNotHave11Digits_ShouldReturnError(string cpf)
    {
        // Arrange
        var employee = EmployeeFactory.Create(cpf: cpf);

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("11 dígitos"));
    }

    // ──────────────────────────────────────────
    // Email
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenEmailIsEmpty_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(email: "");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("e-mail"));
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(email: "email-invalido");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("inválido"));
    }

    // ──────────────────────────────────────────
    // Phone
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenPhoneIsEmpty_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(phone: "");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("telefone"));
    }

    [Fact]
    public void Validate_WhenPhoneHasLessThan10Digits_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(phone: "999998888");

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DDD"));
    }

    // ──────────────────────────────────────────
    // Role / Status
    // ──────────────────────────────────────────

    [Fact]
    public void Validate_WhenRoleIsInvalid_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(role: (EmployeeRole)99);

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Cargo"));
    }

    [Fact]
    public void Validate_WhenStatusIsInvalid_ShouldReturnError()
    {
        // Arrange
        var employee = EmployeeFactory.Create(status: (EmployeeStatus)99);

        // Act
        var result = _validator.Validate(employee);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Status"));
    }
}
