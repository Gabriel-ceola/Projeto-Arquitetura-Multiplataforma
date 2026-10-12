using Employee_service.Employees.Application.DTOs;
using Employee_service.Employees.Application.UseCases;
using Employee_service.Employees.Domain;
using Employee_service.Employees.Domain.Exceptions;
using Employee_service.Employees.Domain.Ports;
using Employee_service.Tests.Helpers;
using FluentAssertions;
using Moq;
using DomainValidationResult = Employee_service.Employees.Domain.ValidationResult;

namespace Employee_service.Tests.Application;

/// <summary>
/// Testes unitários para EmployeeUseCase.
/// As dependências (repositório e validator) são mockadas com Moq.
/// </summary>
public class EmployeeUseCaseTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IEmployeeValidator>  _validatorMock;
    private readonly EmployeeUseCase           _useCase;

    public EmployeeUseCaseTests()
    {
        _repositoryMock = new Mock<IEmployeeRepository>();
        _validatorMock  = new Mock<IEmployeeValidator>();
        _useCase        = new EmployeeUseCase(_repositoryMock.Object, _validatorMock.Object);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private void SetupValidValidation() =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<Employee>()))
            .Returns(DomainValidationResult.Success());

    private void SetupInvalidValidation(params string[] errors)
    {
        var result = new DomainValidationResult();
        foreach (var error in errors)
            result.AddError(error);

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<Employee>()))
            .Returns(result);
    }

    private static CreateEmployeeRequest BuildCreateRequest(string? cpf = null, string? name = null) =>
        new(
            Name:   name ?? "Maria Souza",
            Cpf:    cpf  ?? EmployeeFactory.DefaultCpf,
            Email:  EmployeeFactory.DefaultEmail,
            Phone:  EmployeeFactory.DefaultPhone,
            Role:   EmployeeRole.recepcionista,
            Status: EmployeeStatus.ativo);

    // ──────────────────────────────────────────
    // GetByIdAsync (GetEmployeeById)
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenEmployeeExists_ShouldReturnResponse()
    {
        // Arrange
        var employee = EmployeeFactory.Create();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _useCase.GetByIdAsync(employee.Id);

        // Assert
        result.Id.Should().Be(employee.Id);
        result.Name.Should().Be(employee.Name);
        result.Cpf.Should().Be(employee.Cpf);
        result.Role.Should().Be(employee.Role);
        result.Status.Should().Be(employee.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmployeeNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = async () => await _useCase.GetByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ──────────────────────────────────────────
    // GetAllAsync (GetEmployees)
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenEmployeesExist_ShouldReturnAll()
    {
        // Arrange
        var employees = new List<Employee>
        {
            EmployeeFactory.Create(id: Guid.NewGuid(), name: "Funcionário 1", cpf: "11111111111"),
            EmployeeFactory.Create(id: Guid.NewGuid(), name: "Funcionário 2", cpf: "22222222222"),
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        // Act
        var result = await _useCase.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo("Funcionário 1", "Funcionário 2");
    }

    [Fact]
    public async Task GetAllAsync_WhenThereAreNoEmployees_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        // Act
        var result = await _useCase.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────
    // CreateAsync (CreateEmployee)
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
        result.Role.Should().Be(request.Role);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfIsFormatted_ShouldCheckDuplicityWithOnlyDigits()
    {
        // Arrange
        var request = BuildCreateRequest(cpf: "123.456.789-01");
        SetupValidValidation();

        _repositoryMock
            .Setup(r => r.ExistsByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.CreateAsync(request);

        // Assert
        result.Cpf.Should().Be("12345678901");
        _repositoryMock.Verify(r => r.ExistsByCpfAsync("12345678901", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ShouldThrowArgumentException()
    {
        // Arrange
        var request = BuildCreateRequest(name: "");
        SetupInvalidValidation("O nome do funcionário é obrigatório.", "O CPF deve conter 11 dígitos.");

        // Act
        var act = async () => await _useCase.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nome*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfAlreadyExists_ShouldThrowDomainException()
    {
        // Arrange
        var request = BuildCreateRequest();
        SetupValidValidation();

        _repositoryMock
            .Setup(r => r.ExistsByCpfAsync(EmployeeFactory.DefaultCpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _useCase.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*CPF*");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // UpdateAsync (EditEmployee)
    // ──────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenEmployeeExistsAndDataIsValid_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var employee = EmployeeFactory.Create();
        var request  = new UpdateEmployeeRequest(
            Name:   "Ana Paula",
            Email:  "ana.paula@clinica.com",
            Phone:  "4333334444",
            Role:   EmployeeRole.administrativo,
            Status: EmployeeStatus.ferias);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupValidValidation();

        // Act
        var result = await _useCase.UpdateAsync(employee.Id, request);

        // Assert
        result.Name.Should().Be("Ana Paula");
        result.Email.Should().Be("ana.paula@clinica.com");
        result.Role.Should().Be(EmployeeRole.administrativo);
        result.Status.Should().Be(EmployeeStatus.ferias);
        result.Cpf.Should().Be(EmployeeFactory.DefaultCpf);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateEmployeeRequest("Nome", EmployeeFactory.DefaultEmail, EmployeeFactory.DefaultPhone, EmployeeRole.auxiliar, EmployeeStatus.ativo);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = async () => await _useCase.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ShouldThrowArgumentExceptionAndNotPersist()
    {
        // Arrange
        var employee = EmployeeFactory.Create();
        var request  = new UpdateEmployeeRequest("", "email-invalido", EmployeeFactory.DefaultPhone, EmployeeRole.auxiliar, EmployeeStatus.ativo);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupInvalidValidation("O nome do funcionário é obrigatório.", "O e-mail informado é inválido.");

        // Act
        var act = async () => await _useCase.UpdateAsync(employee.Id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // DeleteAsync (RemoveEmployee)
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenEmployeeExists_ShouldCallRepositoryDelete()
    {
        // Arrange
        var employee = EmployeeFactory.Create();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        await _useCase.DeleteAsync(employee.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(employee.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenEmployeeNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var act = async () => await _useCase.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
