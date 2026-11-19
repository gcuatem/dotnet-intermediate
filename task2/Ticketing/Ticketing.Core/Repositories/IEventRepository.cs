using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public sealed record EventSeatListItem(
    string EventSeatId,
    string SeatId,
    string SectionId,
    string SectionName,
    string SeatNumber,
    string SeatRow,
    Guid PriceId,
    string PriceName,
    SeatStatus Status);

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(string eventId);
    Task<IEnumerable<EventSeatListItem>> GetSeatsByEventIdAsync(string eventId);
    Task<IEnumerable<EventSeatListItem>> GetAvailableSeatsByEventIdAsync(string eventId);
    Task<IEnumerable<EventSeatListItem>> GetSeatsByEventAndSectionAsync(string eventId, string sectionId);
    Task<int> AddAsync(Event @event);
    Task<int> UpdateAsync(Event @event);
    Task<int> DeleteAsync(string eventId);
}
