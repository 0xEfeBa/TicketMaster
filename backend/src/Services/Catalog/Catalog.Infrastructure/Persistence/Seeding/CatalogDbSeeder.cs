using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence.Seeding;

public static class CatalogDbSeeder
{
    public static async Task SeedAsync(CatalogDbContext dbContext)
    {
        if (await dbContext.Events.AnyAsync()) return;

        var organizerId = Guid.NewGuid();

        var @event = new Event(
            Guid.NewGuid(),
            "Rock Night Live",
            "Open-air summer concert.",
            "Istanbul Open-Air Stage",
            "https://picsum.photos/seed/rock/800/400",
            organizerId);

        @event.AddSession(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(30).AddHours(3));
        @event.AddTicketType(Guid.NewGuid(), "VIP", 1500, 50);
        @event.AddTicketType(Guid.NewGuid(), "General", 500, 500);
        @event.Publish();

        var @event2 = new Event(
            Guid.NewGuid(),
            "Tech Conference 2026",
            "Future of platform engineering.",
            "Ankara Convention Center",
            "https://picsum.photos/seed/tech/800/400",
            organizerId);

        @event2.AddSession(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(45), DateTimeOffset.UtcNow.AddDays(45).AddHours(8));
        @event2.AddTicketType(Guid.NewGuid(), "Standard", 250, 200);
        @event2.Publish();

        dbContext.Events.Add(@event);
        dbContext.Events.Add(@event2);

        await dbContext.SaveChangesAsync();
    }
}
