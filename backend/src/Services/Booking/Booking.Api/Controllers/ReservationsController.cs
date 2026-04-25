using Booking.Application.Abstractions;
using Booking.Application.Features.Reservations.Commands.ConfirmReservation;
using Booking.Application.Features.Reservations.Commands.CreateHold;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.BuildingBlocks.Web;

namespace Booking.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Customer")]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdempotencyService _idempotencyService;

    public ReservationsController(IMediator mediator, IIdempotencyService idempotencyService)
    {
        _mediator = mediator;
        _idempotencyService = idempotencyService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHold([FromBody] CreateHoldRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(idempotencyKey) || !Guid.TryParse(idempotencyKey, out var requestId))
        {
            return BadRequest("Idempotency-Key header is missing or invalid.");
        }

        if (await _idempotencyService.RequestExistsAsync(requestId))
        {
            return Ok(new { Message = "Request was already processed." });
        }

        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var bearerToken = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        var command = new CreateHoldCommand(userId, request.EventId, request.TicketTypeId, request.Quantity, bearerToken);
        var reservationId = await _mediator.Send(command, cancellationToken);

        await _idempotencyService.CreateRequestAsync(requestId, "CreateHoldCommand");

        return Created($"/api/v1/reservations/{reservationId}", new { ReservationId = reservationId });
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmReservation(Guid id, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(idempotencyKey) || !Guid.TryParse(idempotencyKey, out var requestId))
            return BadRequest("Idempotency-Key header is missing or invalid.");

        if (await _idempotencyService.RequestExistsAsync(requestId))
            return Ok(new { Message = "Confirmation was already processed." });

        if (!User.TryGetUserId(out var userId)) return Unauthorized();

        var command = new ConfirmReservationCommand(id, userId);
        await _mediator.Send(command, cancellationToken);
        
        await _idempotencyService.CreateRequestAsync(requestId, "ConfirmReservationCommand");

        return Ok(new { Message = "Reservation confirmed successfully." });
    }
}

public record CreateHoldRequest(Guid EventId, Guid TicketTypeId, int Quantity);
