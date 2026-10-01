namespace Ticketing.Api.DTOs;

public record ReserveSeatRequest(
    Guid EventId,
    Guid SeatId,
    string UserId
);

public record ReserveSeatResponse(
    Guid ReservationId,
    string Message,
    DateTime ExpiresAtUtc
);