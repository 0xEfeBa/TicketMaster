using MediatR;

namespace Catalog.Application.Features.Events.Commands.CreateEvent;

public record CreateEventCommand(
    string Title,
    string Description,
    string Venue,
    string? ImageUrl,
    Guid OrganizerUserId) : IRequest<Guid>;
