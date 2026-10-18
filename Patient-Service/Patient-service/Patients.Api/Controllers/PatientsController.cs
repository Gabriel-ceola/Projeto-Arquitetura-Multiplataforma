using Patient_service.Patients.Application.DTOs;
using Patient_service.Patients.Domain.Exceptions;
using Patient_service.Patients.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Patient_service.Patients.Api.Controllers;

/// <summary>
/// Adaptador de entrada (HTTP): expõe os casos de uso de Patient via REST
/// e delega cada chamada ao IPatientUseCase.
/// </summary>
[ApiController]
[Route("api/patients")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IPatientUseCase _useCase;

    public PatientsController(IPatientUseCase useCase)
    {
        _useCase = useCase;
    }

    // ──────────────────────────────────────────
    // GET /api/patients
    // ──────────────────────────────────────────

    /// <summary>Lista todos os pacientes da clínica.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PatientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _useCase.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    // ──────────────────────────────────────────
    // GET /api/patients/{id}
    // ──────────────────────────────────────────

    /// <summary>Retorna um paciente pelo Id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _useCase.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // POST /api/patients
    // ──────────────────────────────────────────

    /// <summary>Cadastra um novo paciente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _useCase.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // PUT /api/patients/{id}
    // ──────────────────────────────────────────

    /// <summary>Edita os dados de um paciente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _useCase.UpdateAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────
    // DELETE /api/patients/{id}
    // ──────────────────────────────────────────

    /// <summary>Remove um paciente da clínica.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _useCase.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
