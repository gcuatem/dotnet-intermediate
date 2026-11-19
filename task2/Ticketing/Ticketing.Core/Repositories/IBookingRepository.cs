using System.Data;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string bookingId, string userId);
    Task<IEnumerable<Booking>> GetByUserIdAsync(string userId);
    Task<IEnumerable<BookingItem>> GetItemsByBookingIdAsync(string bookingId);
    Task<Booking> CreateAsync(Booking booking, IEnumerable<BookingItem> items, IDbTransaction? transaction = null);
    Task<int> CancelPendingAsync(string bookingId, IDbTransaction? transaction = null);
    Task<int> MarkCompletedAsync(string bookingId, IDbTransaction? transaction = null);
    Task<Booking> AddItemAsync(string userId, string eventId, string seatId, Guid priceId);
    Task<Booking> AddItemAsync(string bookingId, string userId, string eventId, string seatId, Guid priceId);
    Task<bool> RemoveItemAsync(string bookingId, string userId, string eventId, string seatId);
    Task<string> MarkBookedAsync(string bookingId, string userId);
}
