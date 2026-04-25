using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(r => r.Id);

        // Optimistic Locking Token mapping via PostgreSQL xmin
        builder.Property(r => r.Version).IsRowVersion();

        builder.HasMany(r => r.Tickets)
               .WithOne()
               .HasForeignKey(t => t.ReservationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
