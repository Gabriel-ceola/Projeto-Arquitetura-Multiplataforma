using Patient_service.Patients.Domain;
using Patient_service.Tests.Helpers;
using FluentAssertions;

namespace Patient_service.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade de domínio Patient.
/// Cobre: construtor, normalização, convênio, Update, MarkAsDeleted.
/// </summary>
public class PatientTests
{
    // ──────────────────────────────────────────
    // Construtor
    // ──────────────────────────────────────────

    [Fact]
    public void Constructor_WhenCalledWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = PatientFactory.DefaultId;

        // Act
        var patient = PatientFactory.Create(id: id, name: "Ana Costa", status: PatientStatus.ativo);

        // Assert
        patient.Id.Should().Be(id);
        patient.Name.Should().Be("Ana Costa");
        patient.Cpf.Should().Be(PatientFactory.DefaultCpf);
        patient.Email.Should().Be(PatientFactory.DefaultEmail);
        patient.Phone.Should().Be(PatientFactory.DefaultPhone);
        patient.BirthDate.Should().Be(PatientFactory.DefaultBirthDate);
        patient.Status.Should().Be(PatientStatus.ativo);
        patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        patient.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeDeletedAtAsNull()
    {
        // Arrange & Act
        var patient = PatientFactory.Create();

        // Assert
        patient.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenCpfAndPhoneAreFormatted_ShouldKeepOnlyDigits()
    {
        // Arrange & Act
        var patient = PatientFactory.Create(cpf: "987.654.321-00", phone: "(43) 98888-7777");

        // Assert
        patient.Cpf.Should().Be("98765432100");
        patient.Phone.Should().Be("43988887777");
    }

    [Fact]
    public void Constructor_WhenNameAndEmailHaveExtraSpacesOrUppercase_ShouldNormalize()
    {
        // Arrange & Act
        var patient = PatientFactory.Create(name: "  Carlos Pereira  ", email: "  Carlos.Pereira@Email.COM ");

        // Assert
        patient.Name.Should().Be("Carlos Pereira");
        patient.Email.Should().Be("carlos.pereira@email.com");
    }

    // ──────────────────────────────────────────
    // Convênio
    // ──────────────────────────────────────────

    [Fact]
    public void Constructor_WhenInsuranceIsNotInformed_ShouldHaveNoInsurance()
    {
        // Arrange & Act
        var patient = PatientFactory.Create();

        // Assert
        patient.InsuranceName.Should().BeNull();
        patient.InsuranceCardNumber.Should().BeNull();
        patient.HasInsurance.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenInsuranceIsInformed_ShouldTrimAndHaveInsurance()
    {
        // Arrange & Act
        var patient = PatientFactory.Create(insuranceName: "  Unimed ", insuranceCardNumber: " 0001234 ");

        // Assert
        patient.InsuranceName.Should().Be("Unimed");
        patient.InsuranceCardNumber.Should().Be("0001234");
        patient.HasInsurance.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenInsuranceIsBlank_ShouldStoreNull()
    {
        // Arrange & Act
        var patient = PatientFactory.Create(insuranceName: "   ", insuranceCardNumber: "");

        // Assert
        patient.InsuranceName.Should().BeNull();
        patient.InsuranceCardNumber.Should().BeNull();
    }

    // ──────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────

    [Fact]
    public void Update_WhenCalled_ShouldChangeMutableFieldsAndKeepCpf()
    {
        // Arrange
        var patient   = PatientFactory.Create();
        var createdAt = patient.CreatedAt;
        var birthDate = new DateOnly(1985, 1, 10);

        // Act
        patient.Update("Carlos P. Lima", "carlos.lima@email.com", "4333334444", birthDate, PatientStatus.inativo, "Amil", "999");

        // Assert
        patient.Name.Should().Be("Carlos P. Lima");
        patient.Email.Should().Be("carlos.lima@email.com");
        patient.Phone.Should().Be("4333334444");
        patient.BirthDate.Should().Be(birthDate);
        patient.Status.Should().Be(PatientStatus.inativo);
        patient.InsuranceName.Should().Be("Amil");
        patient.InsuranceCardNumber.Should().Be("999");
        patient.Cpf.Should().Be(PatientFactory.DefaultCpf);
        patient.Id.Should().Be(PatientFactory.DefaultId);
        patient.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Update_WhenInsuranceIsNotInformed_ShouldKeepInsurance()
    {
        // Arrange
        var patient = PatientFactory.Create(insuranceName: "Unimed", insuranceCardNumber: "0001234");

        // Act
        patient.Update(patient.Name, patient.Email, patient.Phone, patient.BirthDate, patient.Status);

        // Assert
        patient.InsuranceName.Should().Be("Unimed");
        patient.InsuranceCardNumber.Should().Be("0001234");
        patient.HasInsurance.Should().BeTrue();
    }

    [Fact]
    public void Update_WhenInsuranceIsBlank_ShouldClearInsurance()
    {
        // Arrange
        var patient = PatientFactory.Create(insuranceName: "Unimed", insuranceCardNumber: "0001234");

        // Act
        patient.Update(patient.Name, patient.Email, patient.Phone, patient.BirthDate, patient.Status, "", "");

        // Assert
        patient.InsuranceName.Should().BeNull();
        patient.InsuranceCardNumber.Should().BeNull();
        patient.HasInsurance.Should().BeFalse();
    }

    [Fact]
    public void Update_WhenCalled_ShouldRefreshUpdatedAt()
    {
        // Arrange
        var patient       = PatientFactory.Create();
        var oldUpdatedAt  = patient.UpdatedAt;
        Thread.Sleep(10);

        // Act
        patient.Update(patient.Name, patient.Email, patient.Phone, patient.BirthDate, patient.Status);

        // Assert
        patient.UpdatedAt.Should().BeAfter(oldUpdatedAt);
    }

    // ──────────────────────────────────────────
    // MarkAsDeleted
    // ──────────────────────────────────────────

    [Fact]
    public void MarkAsDeleted_WhenCalled_ShouldSetDeletedAt()
    {
        // Arrange
        var patient = PatientFactory.Create();

        // Act
        patient.MarkAsDeleted();

        // Assert
        patient.DeletedAt.Should().NotBeNull();
        patient.DeletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
