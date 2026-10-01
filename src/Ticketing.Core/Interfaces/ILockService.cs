namespace Ticketing.Core.Interfaces;

public interface ILockService
{
    Task<bool> AcquireSeatLockAsync(Guid seatId, string userId, TimeSpan lockDuration);
    Task<bool> ReleaseSeatLockAsync(Guid seatId, string userId);
}