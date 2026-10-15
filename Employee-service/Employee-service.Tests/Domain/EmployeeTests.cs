using Employee_service.Employees.Domain;
using Employee_service.Tests.Helpers;
using FluentAssertions;

namespace Employee_service.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade de domínio Employee.
/// Cobre: construtor, normalização, Update, MarkAsDeleted.
/// </summary>
public class EmployeeTests
{
    // ──────────────────────────────────────────
    // Construtor
    // ──────────────────────────────────────────

    [Fact]
    public void Constructor_WhenCalledWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = EmployeeFactory.DefaultId;

        // Act
        var employee = EmployeeFactory.Create(
            id:     id,
            name:   "João Lima",
            role:   EmployeeRole.auxiliar,
            status: EmployeeStatus.ativo);

        // Assert
        employee.Id.Should().Be(id);
        employee.Name.Should().Be("João Lima");
        employee.Cpf.Should().Be(EmployeeFactory.DefaultCpf);
        employee.Email.Should().Be(EmployeeFactory.DefaultEmail);
        employee.Phone.Should().Be(EmployeeFactory.DefaultPhone);
        employee.Role.Should().Be(EmployeeRole.auxiliar);
        employee.Status.Should().Be(EmployeeStatus.ativo);
        employee.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        employee.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeDeletedAtAsNull()
    {
        // Arrange & Act
        var employee = EmployeeFactory.Create();

        // Assert
        employee.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenCpfAndPhoneAreFormatted_ShouldKeepOnlyDigits()
    {
        // Arrange & Act
        var employee = EmployeeFactory.Create(cpf: "123.456.789-01", phone: "(43) 99999-8888");

        // Assert
        employee.Cpf.Should().Be("12345678901");
        employee.Phone.Should().Be("43999998888");
    }

    [Fact]
    public void Constructor_WhenNameAndEmailHaveExtraSpacesOrUppercase_ShouldNormalize()
    {
        // Arrange & Act
        var employee = EmployeeFactory.Create(name: "  Maria Souza  ", email: "  Maria.Souza@Clinica.COM ");

        // Assert
        employee.Name.Should().Be("Maria Souza");
        employee.Email.Should().Be("maria.souza@clinica.com");
    }

    // ──────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────

    [Fact]
    public void Update_WhenCalledWithValidData_ShouldUpdateMutableFields()
    {
        // Arrange
        var employee = EmployeeFactory.Create();

        // Act
        employee.Update("Ana Paula", "ana.paula@clinica.com", "4333334444", EmployeeRole.administrativo, EmployeeStatus.ferias);

        // Assert
        employee.Name.Should().Be("Ana Paula");
        employee.Email.Should().Be("ana.paula@clinica.com");
        employee.Phone.Should().Be("4333334444");
        employee.Role.Should().Be(EmployeeRole.administrativo);
        employee.Status.Should().Be(EmployeeStatus.ferias);
    }

    [Fact]
    public void Update_WhenCalled_ShouldRefreshUpdatedAt()
    {
        // Arrange
        var employee     = EmployeeFactory.Create();
        var beforeUpdate = employee.UpdatedAt;

        // Act
        employee.Update("Novo nome", EmployeeFactory.DefaultEmail, EmployeeFactory.DefaultPhone, EmployeeRole.auxiliar, EmployeeStatus.ativo);

        // Assert
        employee.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void Update_WhenCalled_ShouldPreserveIdAndCpfAndCreatedAt()
    {
        // Arrange
        var employee          = EmployeeFactory.Create();
        var originalId        = employee.Id;
        var originalCpf       = employee.Cpf;
        var originalCreatedAt = employee.CreatedAt;

        // Act
        employee.Update("Outro nome", "outro@clinica.com", "4311112222", EmployeeRole.auxiliar, EmployeeStatus.inativo);

        // Assert
        employee.Id.Should().Be(originalId);
        employee.Cpf.Should().Be(originalCpf);
        employee.CreatedAt.Should().Be(originalCreatedAt);
    }

    // ──────────────────────────────────────────
    // MarkAsDeleted
    // ──────────────────────────────────────────

    [Fact]
    public void MarkAsDeleted_WhenCalled_ShouldSetDeletedAtToUtcNow()
    {
        // Arrange
        var employee = EmployeeFactory.Create();

        // Act
        employee.MarkAsDeleted();

        // Assert
        employee.DeletedAt.Should().NotBeNull();
        employee.DeletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkAsDeleted_WhenCalled_ShouldNotChangeOtherProperties()
    {
        // Arrange
        var employee         = EmployeeFactory.Create();
        var originalName     = employee.Name;
        var originalCpf      = employee.Cpf;
        var originalStatus   = employee.Status;

        // Act
        employee.MarkAsDeleted();

        // Assert
        employee.Name.Should().Be(originalName);
        employee.Cpf.Should().Be(originalCpf);
        employee.Status.Should().Be(originalStatus);
    }
}
