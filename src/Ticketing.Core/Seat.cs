using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Core.Entities;

public enum SeatStatus
{
    Available = 0,
    Reserved = 1,  // Bloccato temporaneamente (TTL Redis / Reservation Pending)
    Sold = 2       // Acquistato definitivamente su Postgres
}

public class Seat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string SeatNumber { get; set; } = string.Empty; // es. "A-12"
    public decimal Price { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public byte[] RowVersion { get; set; } = []; // Per l'Optimistic Concurrency su EF Core
}
