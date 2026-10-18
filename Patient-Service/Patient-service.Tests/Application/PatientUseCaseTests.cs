using Patient_service.Patients.Application.DTOs;
using Patient_service.Patients.Application.UseCases;
using Patient_service.Patients.Domain;
using Patient_service.Patients.Domain.Exceptions;
using Patient_service.Patients.Domain.Ports;
using Patient_service.Tests.Helpers;
using FluentAssertions;
using Moq;
using DomainValidationResult = Patient_service.Patients.Domain.ValidationResult;

namespace Patient_service.Tests.Application;

/// <summary>
/// Testes unitários para PatientUseCase.
/// As dependências (repositório e validator) são mockadas com Moq.
/// </summary>
public class PatientUseCaseTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly Mock<IPatientValidator>  _validatorMock;
    private readonly PatientUseCase           _useCase;

    public PatientUseCaseTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _validatorMock  = new Mock<IPatientValidator>();
        _useCase        = new PatientUseCase(_repositoryMock.Object, _validatorMock.Object);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private void SetupValidValidation() =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<Patient>()))
            .Returns(DomainValidationResult.Success());

    private void SetupInvalidValidation(params string[] errors)
    {
        var result = new DomainValidationResult();
        foreach (var error in errors)
            result.AddError(error);

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<Patient>()))
            .Returns(result);
    }

    private static CreatePatientRequest BuildCreateRequest(string? cpf = null, string? name = null) =>
        new(
            Name:      name ?? "Carlos Pereira",
            Cpf:       cpf  ?? PatientFactory.DefaultCpf,
            Email:     PatientFactory.DefaultEmail,
            Phone:     PatientFactory.DefaultPhone,
            BirthDate: PatientFactory.DefaultBirthDate,
            Status:    PatientStatus.ativo);

    private static UpdatePatientRequest BuildUpdateRequest(string name = "Carlos P. Lima", string email = "carlos.lima@email.com") =>
        new(
            Name:                name,
            Email:               email,
            Phone:               "4333334444",
            BirthDate:           new DateOnly(1985, 1, 10),
            Status:              PatientStatus.inativo,
            InsuranceName:       "Amil",
            InsuranceCardNumber: "999");

    // ──────────────────────────────────────────
    // GetByIdAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenPatientExists_ShouldReturnResponse()
    {
        // Arrange
        var patient = PatientFactory.Create();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _useCase.GetByIdAsync(patient.Id);

        // Assert
        result.Id.Should().Be(patient.Id);
        result.Name.Should().Be(patient.Name);
        result.Cpf.Should().Be(patient.Cpf);
        result.BirthDate.Should().Be(patient.BirthDate);
        result.Status.Should().Be(patient.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var act = async () => await _useCase.GetByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ──────────────────────────────────────────
    // GetAllAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenPatientsExist_ShouldReturnAll()
    {
        // Arrange
        var patients = new List<Patient>
        {
            PatientFactory.Create(id: Guid.NewGuid(), name: "Paciente 1", cpf: "11111111111"),
            PatientFactory.Create(id: Guid.NewGuid(), name: "Paciente 2", cpf: "22222222222"),
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(patients);

        // Act
        var result = await _useCase.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo("Paciente 1", "Paciente 2");
    }

    [Fact]
    public async Task GetAllAsync_WhenThereAreNoPatients_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Patient>());

        // Act
        var result = await _useCase.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────
    // CreateAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldPersistAndReturnResponse()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupValidValidation();

        _repositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.CreateAsync(request);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(request.Name);
        result.Cpf.Should().Be(request.Cpf);
        result.BirthDate.Should().Be(request.BirthDate);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfIsFormatted_ShouldCheckDuplicityWithOnlyDigits()
    {
        // Arrange
        var request = BuildCreateRequest(cpf: "987.654.321-00");
        SetupValidValidation();

        _repositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.CreateAsync(request);

        // Assert
        result.Cpf.Should().Be("98765432100");
        _repositoryMock.Verify(r => r.ExistsByCpfAsync("98765432100", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ShouldThrowArgumentException()
    {
        // Arrange
        var request = BuildCreateRequest(name: "");
        SetupInvalidValidation("O nome do paciente é obrigatório.", "O CPF deve conter 11 dígitos.");

        // Act
        var act = async () => await _useCase.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nome*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfAlreadyExists_ShouldThrowDomainExceptionAndNotPersist()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupValidValidation();

        _repositoryMock
            .Setup(r => r.ExistsByCpfAsync(PatientFactory.DefaultCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _useCase.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*CPF*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // UpdateAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenPatientExistsAndDataIsValid_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var patient = PatientFactory.Create();
        var request = BuildUpdateRequest();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        SetupValidValidation();

        // Act
        var result = await _useCase.UpdateAsync(patient.Id, request);

        // Assert
        result.Name.Should().Be("Carlos P. Lima");
        result.Email.Should().Be("carlos.lima@email.com");
        result.Status.Should().Be(PatientStatus.inativo);
        result.InsuranceName.Should().Be("Amil");
        result.Cpf.Should().Be(PatientFactory.DefaultCpf);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenPatientNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var act = async () => await _useCase.UpdateAsync(Guid.NewGuid(), BuildUpdateRequest());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ShouldThrowArgumentExceptionAndNotPersist()
    {
        // Arrange
        var patient = PatientFactory.Create();
        var request = BuildUpdateRequest(name: "", email: "email-invalido");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        SetupInvalidValidation("O nome do paciente é obrigatório.", "O e-mail informado é inválido.");

        // Act
        var act = async () => await _useCase.UpdateAsync(patient.Id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // DeleteAsync
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenPatientExists_ShouldCallRepositoryDelete()
    {
        // Arrange
        var patient = PatientFactory.Create();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        await _useCase.DeleteAsync(patient.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(patient.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenPatientNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var act = async () => await _useCase.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
