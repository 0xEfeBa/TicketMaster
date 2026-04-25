using Catalog.Application.Features.Events.Commands.CreateEvent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.BuildingBlocks.Web;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = "OrganizerOrAdmin")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var organizerUserId))
            return Unauthorized("Geçersiz kullanıcı kimliği.");

        var command = new CreateEventCommand(
            request.Title,
            request.Description,
            request.Venue,
            request.ImageUrl,
            organizerUserId);

        var eventId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetEventById), new { id = eventId }, new { Id = eventId });
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventById(Guid id, CancellationToken cancellationToken)
    {
        Guid? viewerId = null;
        string? role = null;
        if (User.Identity?.IsAuthenticated == true && User.TryGetUserId(out var uid))
        {
            viewerId = uid;
            role = User.GetRoleName();
        }

        var result = await _mediator.Send(
            new Catalog.Application.Features.Events.Queries.GetEventById.GetEventByIdQuery(
                id,
                viewerId,
                role),
            cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new Catalog.Application.Features.Events.Queries.GetEvents.GetEventsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/publish")]
    [Authorize(Policy = "OrganizerOrAdmin")]
    public async Task<IActionResult> PublishEvent(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        var isAdmin = string.Equals(User.GetRoleName(), "Admin", StringComparison.Ordinal);

        await _mediator.Send(new Catalog.Application.Features.Events.Commands.PublishEvent.PublishEventCommand(id, userId, isAdmin), cancellationToken);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "OrganizerOrAdmin")]
    public async Task<IActionResult> CancelEvent(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        var isAdmin = string.Equals(User.GetRoleName(), "Admin", StringComparison.Ordinal);

        await _mediator.Send(new Catalog.Application.Features.Events.Commands.CancelEvent.CancelEventCommand(id, userId, isAdmin), cancellationToken);
        return Ok();
    }

    [HttpPost("{id}/ticket-types")]
    [Authorize(Policy = "OrganizerOrAdmin")]
    public async Task<IActionResult> AddTicketType(Guid id, [FromBody] AddTicketTypeRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        var isAdmin = string.Equals(User.GetRoleName(), "Admin", StringComparison.Ordinal);

        var ticketTypeId = await _mediator.Send(
            new Catalog.Application.Features.Events.Commands.AddTicketType.AddTicketTypeCommand(
                id, request.Name, request.PriceAmount, request.TotalQuantity, userId, isAdmin),
            cancellationToken);
        return Ok(new { TicketTypeId = ticketTypeId });
    }
}

public record CreateEventRequest(string Title, string Description, string Venue, string? ImageUrl);
public record AddTicketTypeRequest(string Name, decimal PriceAmount, int TotalQuantity);
