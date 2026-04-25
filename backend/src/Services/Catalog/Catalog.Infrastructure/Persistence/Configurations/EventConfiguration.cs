using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Venue).IsRequired().HasMaxLength(200);
        
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasMany(e => e.Sessions)
               .WithOne()
               .HasForeignKey(s => s.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TicketTypes)
               .WithOne()
               .HasForeignKey(t => t.EventId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
