using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Core.Events;

public record TicketReservationRequestedEvent(
    Guid ReservationId,
    Guid EventId,
    Guid SeatId,
    string UserId,
    DateTime RequestedAtUtc
);