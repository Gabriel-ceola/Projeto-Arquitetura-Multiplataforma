using Employee_service.Employees.Api.Controllers;
using Employee_service.Employees.Application.DTOs;
using Employee_service.Employees.Domain;
using Employee_service.Employees.Domain.Exceptions;
using Employee_service.Employees.Domain.Ports;
using Employee_service.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Employee_service.Tests.Api;

/// <summary>
/// Testes unitários para EmployeesController.
/// O use case é mockado com Moq; valida o mapeamento de resultados e exceções para status HTTP.
/// </summary>
public class EmployeesControllerTests
{
    private readonly Mock<IEmployeeUseCase> _useCaseMock;
    private readonly EmployeesController    _controller;

    public EmployeesControllerTests()
    {
        _useCaseMock = new Mock<IEmployeeUseCase>();
        _controller  = new EmployeesController(_useCaseMock.Object);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private static EmployeeResponse BuildResponse(
        Guid?  id   = null,
        string name = "Maria Souza") =>
        new(
            Id:        id ?? EmployeeFactory.DefaultId,
            Name:      name,
            Cpf:       EmployeeFactory.DefaultCpf,
            Email:     EmployeeFactory.DefaultEmail,
            Phone:     EmployeeFactory.DefaultPhone,
            Role:      EmployeeRole.recepcionista,
            Status:    EmployeeStatus.ativo,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

    private static CreateEmployeeRequest BuildCreateRequest() =>
        new(
            Name:   "Maria Souza",
            Cpf:    EmployeeFactory.DefaultCpf,
            Email:  EmployeeFactory.DefaultEmail,
            Phone:  EmployeeFactory.DefaultPhone,
            Role:   EmployeeRole.recepcionista,
            Status: EmployeeStatus.ativo);

    private static UpdateEmployeeRequest BuildUpdateRequest(string name = "Maria Souza Lima") =>
        new(
            Name:   name,
            Email:  EmployeeFactory.DefaultEmail,
            Phone:  EmployeeFactory.DefaultPhone,
            Role:   EmployeeRole.administrativo,
            Status: EmployeeStatus.ferias);

    // ──────────────────────────────────────────
    // GET /api/employees
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAll_WhenEmployeesExist_ShouldReturn200WithList()
    {
        // Arrange
        var responses = new List<EmployeeResponse> { BuildResponse() };

        _useCaseMock
            .Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(responses);
    }

    [Fact]
    public async Task GetAll_WhenThereAreNoEmployees_ShouldReturn200WithEmptyList()
    {
        // Arrange
        _useCaseMock
            .Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeResponse>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new List<EmployeeResponse>());
    }

    // ──────────────────────────────────────────
    // GET /api/employees/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenEmployeeExists_ShouldReturn200()
    {
        // Arrange
        var response = BuildResponse();

        _useCaseMock
            .Setup(u => u.GetByIdAsync(response.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(response.Id, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_WhenEmployeeNotFound_ShouldReturn404()
    {
        // Arrange
        _useCaseMock
            .Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Funcionário não encontrado."));

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    // ──────────────────────────────────────────
    // POST /api/employees
    // ──────────────────────────────────────────

    [Fact]
    public async Task Create_WhenRequestIsValid_ShouldReturn201WithLocation()
    {
        // Arrange
        var request  = BuildCreateRequest();
        var response = BuildResponse();

        _useCaseMock
            .Setup(u => u.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(EmployeesController.GetById));
        created.RouteValues!["id"].Should().Be(response.Id);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturn400()
    {
        // Arrange
        var request = BuildCreateRequest();

        _useCaseMock
            .Setup(u => u.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("O nome do funcionário é obrigatório."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_WhenCpfAlreadyExists_ShouldReturn409()
    {
        // Arrange
        var request = BuildCreateRequest();

        _useCaseMock
            .Setup(u => u.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Já existe um funcionário cadastrado com o CPF informado."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>()
            .Which.StatusCode.Should().Be(409);
    }

    // ──────────────────────────────────────────
    // PUT /api/employees/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task Update_WhenEmployeeExistsAndDataIsValid_ShouldReturn200()
    {
        // Arrange
        var employeeId = EmployeeFactory.DefaultId;
        var request    = BuildUpdateRequest();
        var response   = BuildResponse(id: employeeId, name: "Maria Souza Lima");

        _useCaseMock
            .Setup(u => u.UpdateAsync(employeeId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Update(employeeId, request, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Update_WhenEmployeeNotFound_ShouldReturn404()
    {
        // Arrange
        var request = BuildUpdateRequest();

        _useCaseMock
            .Setup(u => u.UpdateAsync(It.IsAny<Guid>(), request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Funcionário não encontrado."));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Update_WhenValidationFails_ShouldReturn400()
    {
        // Arrange
        var request = BuildUpdateRequest(name: "");

        _useCaseMock
            .Setup(u => u.UpdateAsync(It.IsAny<Guid>(), request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("O nome do funcionário é obrigatório."));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ──────────────────────────────────────────
    // DELETE /api/employees/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenEmployeeExists_ShouldReturn204()
    {
        // Arrange
        var employeeId = EmployeeFactory.DefaultId;

        _useCaseMock
            .Setup(u => u.DeleteAsync(employeeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(employeeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_WhenEmployeeNotFound_ShouldReturn404()
    {
        // Arrange
        _useCaseMock
            .Setup(u => u.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Funcionário não encontrado."));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }
}
