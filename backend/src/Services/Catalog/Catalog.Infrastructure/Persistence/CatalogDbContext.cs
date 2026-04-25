using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TicketFlow.BuildingBlocks.Messaging.Outbox;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
