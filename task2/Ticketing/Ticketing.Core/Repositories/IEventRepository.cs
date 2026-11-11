using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(string eventId);
    Task<IEnumerable<Seat>> GetSeatsByEventIdAsync(string eventId);
    Task<IEnumerable<Seat>> GetAvailableSeatsByEventIdAsync(string eventId);
    Task<int> AddAsync(Event @event);
    Task<int> UpdateAsync(Event @event);
    Task<int> DeleteAsync(string eventId);
}
