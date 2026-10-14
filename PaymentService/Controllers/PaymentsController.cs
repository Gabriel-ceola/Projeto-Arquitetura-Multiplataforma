using PaymentService.Application.DTOs;
using PaymentService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>Lista os pagamentos registrados durante a execução do serviço.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var payments = await paymentService.GetAllAsync(cancellationToken);
        return Ok(payments);
    }

    /// <summary>Consulta um pagamento pelo seu identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var payment = await paymentService.GetByIdAsync(id, cancellationToken);
        return payment is null
            ? NotFound(new { message = $"Pagamento com id '{id}' não encontrado." })
            : Ok(payment);
    }

    /// <summary>Cria uma solicitação de pagamento com status inicial pendente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await paymentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Atualiza o status de um pagamento para simular seu processamento.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await paymentService.UpdateStatusAsync(id, request, cancellationToken);
            return payment is null
                ? NotFound(new { message = $"Pagamento com id '{id}' não encontrado." })
                : Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
