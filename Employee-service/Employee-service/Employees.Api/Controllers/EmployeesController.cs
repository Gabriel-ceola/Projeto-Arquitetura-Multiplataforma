using Employee_service.Employees.Application.DTOs;
using Employee_service.Employees.Domain.Exceptions;
using Employee_service.Employees.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Employee_service.Employees.Api.Controllers;

/// <summary>
/// Adaptador de entrada (HTTP): EmployeeRouter do modelo C4.
/// Expõe os casos de uso de Employee via REST e delega cada chamada ao IEmployeeUseCase.
/// </summary>
[ApiController]
[Route("api/employees")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeUseCase _useCase;

    public EmployeesController(IEmployeeUseCase useCase)
    {
        _useCase = useCase;
    }

    // ──────────────────────────────────────────
    // GET /api/employees
    // ──────────────────────────────────────────

    /// <summary>Lista todos os funcionários da clínica (GetEmployees).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _useCase.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    // ──────────────────────────────────────────
    // GET /api/employees/{id}
    // ──────────────────────────────────────────

    /// <summary>Retorna um funcionário pelo Id (GetEmployeeById).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
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
    // POST /api/employees
    // ──────────────────────────────────────────

    /// <summary>Cadastra um novo funcionário (CreateEmployee).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
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
    // PUT /api/employees/{id}
    // ──────────────────────────────────────────

    /// <summary>Edita os dados de um funcionário (EditEmployee).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
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
    // DELETE /api/employees/{id}
    // ──────────────────────────────────────────

    /// <summary>Remove um funcionário da clínica (RemoveEmployee).</summary>
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
