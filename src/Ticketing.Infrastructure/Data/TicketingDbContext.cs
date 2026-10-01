using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Entities;

namespace Ticketing.Infrastructure.Data;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
    {
    }

    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurazione dell'entità Seat
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SeatNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            // Indice composto per velocizzare le query di ricerca posti per evento e stato
            entity.HasIndex(e => new { e.EventId, e.Status });

            // Optimistic Concurrency Token per PostgreSQL (usando xmin system column)
            entity.Property(e => e.RowVersion)
                  .IsRowVersion();
        });

        // Configurazione dell'entità Reservation
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SeatId);
        });
    }
}