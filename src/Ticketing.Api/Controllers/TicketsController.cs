using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.DTOs;
using Ticketing.Core.Events;
using Ticketing.Core.Interfaces;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ILockService _lockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ILockService lockService,
        IPublishEndpoint publishEndpoint,
        ILogger<TicketsController> logger)
    {
        _lockService = lockService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveSeat([FromBody] ReserveSeatRequest request)
    {
        // 1. Tenta l'acquisizione del lock atomico su Redis (TTL: 10 minuti)
        var lockDuration = TimeSpan.FromMinutes(10);
        var lockAcquired = await _lockService.AcquireSeatLockAsync(request.SeatId, request.UserId, lockDuration);

        if (!lockAcquired)
        {
            _logger.LogWarning("Conflict: Seat {SeatId} is currently locked or already reserved by another user.", request.SeatId);
            return Conflict(new { message = "Il posto selezionato è momentaneamente bloccato o non più disponibile." });
        }

        var reservationId = Guid.NewGuid();
        var requestedAtUtc = DateTime.UtcNow;

        // 2. Pubblica l'evento asincrono su RabbitMQ per l'elaborazione su DB
        await _publishEndpoint.Publish(new TicketReservationRequestedEvent(
            ReservationId: reservationId,
            EventId: request.EventId,
            SeatId: request.SeatId,
            UserId: request.UserId,
            RequestedAtUtc: requestedAtUtc
        ));

        _logger.LogInformation("Reservation {ReservationId} queued successfully for Seat {SeatId}.", reservationId, request.SeatId);

        // 3. Risposta immediata al client
        return Accepted(new ReserveSeatResponse(
            ReservationId: reservationId,
            Message: "Richiesta di prenotazione presa in carico.",
            ExpiresAtUtc: requestedAtUtc.Add(lockDuration)
        ));
    }
}