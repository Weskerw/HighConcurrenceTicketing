using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Entities;
using Ticketing.Core.Events;
using Ticketing.Infrastructure.Data;

namespace Ticketing.Worker.Consumers;

public class TicketReservationConsumer : IConsumer<TicketReservationRequestedEvent>
{
    private readonly TicketingDbContext _dbContext;
    private readonly ILogger<TicketReservationConsumer> _logger;

    public TicketReservationConsumer(TicketingDbContext dbContext, ILogger<TicketReservationConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketReservationRequestedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Elaborazione prenotazione {ReservationId} per il posto {SeatId}...",
            message.ReservationId, message.SeatId);

        // 1. Recupera il posto su DB
        var seat = await _dbContext.Seats.FirstOrDefaultAsync(s => s.Id == message.SeatId);

        if (seat == null)
        {
            _logger.LogError("Posto {SeatId} non trovato a database.", message.SeatId);
            return;
        }

        if (seat.Status != SeatStatus.Available)
        {
            _logger.LogWarning("Posto {SeatId} già occupato o riservato su DB (Stato: {Status}).", message.SeatId, seat.Status);
            return;
        }

        // 2. Aggiorna lo stato del posto e crea la prenotazione
        seat.Status = SeatStatus.Reserved;

        var reservation = new Reservation
        {
            Id = message.ReservationId,
            SeatId = message.SeatId,
            UserId = message.UserId,
            CreatedAtUtc = message.RequestedAtUtc,
            ExpiresAtUtc = message.RequestedAtUtc.AddMinutes(10),
            IsConfirmed = false
        };

        _dbContext.Reservations.Add(reservation);

        try
        {
            // 3. Persiste le modifiche. Se un altro thread/processo ha toccato il Seat nello stesso istante,
            // la xmin row version di Postgres scatenerà una DbUpdateConcurrencyException
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Prenotazione {ReservationId} completata con successo su PostgreSQL!", message.ReservationId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Errore di concorrenza rilevato su Postgres per il posto {SeatId}.", message.SeatId);
            throw; // Rilancia per attivare le logiche di Retry di MassTransit se necessario
        }
    }
}