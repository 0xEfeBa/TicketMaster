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
            "Müthiş Rock Konseri",
            "Şehrin en büyük açık hava konseri!",
            "İstanbul Açık Hava Sahnesi",
            "https://picsum.photos/seed/rock/800/400",
            organizerId);

        @event.AddSession(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(30).AddHours(3));
        @event.AddTicketType(Guid.NewGuid(), "VIP", 1500, 50);
        @event.AddTicketType(Guid.NewGuid(), "Genel Alan", 500, 500);
        @event.Publish();

        var @event2 = new Event(
            Guid.NewGuid(),
            "Teknoloji Konferansı 2026",
            "Geleceğin teknolojileri burada konuşuluyor.",
            "Ankara Kongre Merkezi",
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
