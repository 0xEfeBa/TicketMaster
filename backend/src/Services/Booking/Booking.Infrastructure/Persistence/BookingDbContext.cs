using Booking.Application.Abstractions;
using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence;

public class BookingDbContext : DbContext, IUnitOfWork
{
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
