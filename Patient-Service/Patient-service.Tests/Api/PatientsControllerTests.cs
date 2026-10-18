using Patient_service.Patients.Api.Controllers;
using Patient_service.Patients.Application.DTOs;
using Patient_service.Patients.Domain;
using Patient_service.Patients.Domain.Exceptions;
using Patient_service.Patients.Domain.Ports;
using Patient_service.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Patient_service.Tests.Api;

/// <summary>
/// Testes unitários para PatientsController.
/// O use case é mockado com Moq; valida o mapeamento de resultados e exceções para status HTTP.
/// </summary>
public class PatientsControllerTests
{
    private readonly Mock<IPatientUseCase> _useCaseMock;
    private readonly PatientsController    _controller;

    public PatientsControllerTests()
    {
        _useCaseMock = new Mock<IPatientUseCase>();
        _controller  = new PatientsController(_useCaseMock.Object);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private static PatientResponse BuildResponse(
        Guid?  id   = null,
        string name = "Carlos Pereira") =>
        new(
            Id:                  id ?? PatientFactory.DefaultId,
            Name:                name,
            Cpf:                 PatientFactory.DefaultCpf,
            Email:               PatientFactory.DefaultEmail,
            Phone:               PatientFactory.DefaultPhone,
            BirthDate:           PatientFactory.DefaultBirthDate,
            InsuranceName:       null,
            InsuranceCardNumber: null,
            Status:              PatientStatus.ativo,
            CreatedAt:           DateTime.UtcNow,
            UpdatedAt:           DateTime.UtcNow);

    private static CreatePatientRequest BuildCreateRequest() =>
        new(
            Name:      "Carlos Pereira",
            Cpf:       PatientFactory.DefaultCpf,
            Email:     PatientFactory.DefaultEmail,
            Phone:     PatientFactory.DefaultPhone,
            BirthDate: PatientFactory.DefaultBirthDate,
            Status:    PatientStatus.ativo);

    private static UpdatePatientRequest BuildUpdateRequest(string name = "Carlos P. Lima") =>
        new(
            Name:      name,
            Email:     PatientFactory.DefaultEmail,
            Phone:     PatientFactory.DefaultPhone,
            BirthDate: PatientFactory.DefaultBirthDate,
            Status:    PatientStatus.inativo);

    // ──────────────────────────────────────────
    // GET /api/patients
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAll_WhenPatientsExist_ShouldReturn200WithList()
    {
        // Arrange
        var responses = new List<PatientResponse> { BuildResponse() };

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

    // ──────────────────────────────────────────
    // GET /api/patients/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenPatientExists_ShouldReturn200()
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
    public async Task GetById_WhenPatientNotFound_ShouldReturn404()
    {
        // Arrange
        _useCaseMock
            .Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Paciente não encontrado."));

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    // ──────────────────────────────────────────
    // POST /api/patients
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
        created.ActionName.Should().Be(nameof(PatientsController.GetById));
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
            .ThrowsAsync(new ArgumentException("O nome do paciente é obrigatório."));

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
            .ThrowsAsync(new DomainException("Já existe um paciente cadastrado com o CPF informado."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>()
            .Which.StatusCode.Should().Be(409);
    }

    // ──────────────────────────────────────────
    // PUT /api/patients/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task Update_WhenPatientExistsAndDataIsValid_ShouldReturn200()
    {
        // Arrange
        var patientId = PatientFactory.DefaultId;
        var request   = BuildUpdateRequest();
        var response  = BuildResponse(id: patientId, name: "Carlos P. Lima");

        _useCaseMock
            .Setup(u => u.UpdateAsync(patientId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Update(patientId, request, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Update_WhenPatientNotFound_ShouldReturn404()
    {
        // Arrange
        var request = BuildUpdateRequest();

        _useCaseMock
            .Setup(u => u.UpdateAsync(It.IsAny<Guid>(), request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Paciente não encontrado."));

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
            .ThrowsAsync(new ArgumentException("O nome do paciente é obrigatório."));

        // Act
        var result = await _controller.Update(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ──────────────────────────────────────────
    // DELETE /api/patients/{id}
    // ──────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenPatientExists_ShouldReturn204()
    {
        // Arrange
        var patientId = PatientFactory.DefaultId;

        _useCaseMock
            .Setup(u => u.DeleteAsync(patientId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(patientId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task Delete_WhenPatientNotFound_ShouldReturn404()
    {
        // Arrange
        _useCaseMock
            .Setup(u => u.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Paciente não encontrado."));

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }
}
