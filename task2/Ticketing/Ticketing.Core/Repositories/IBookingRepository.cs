using System.Data;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string bookingId);
    Task<IEnumerable<Booking>> GetByUserIdAsync(string userId);
    Task<IEnumerable<BookingItem>> GetItemsByBookingIdAsync(string bookingId);
    Task<Booking> CreateAsync(Booking booking, IEnumerable<BookingItem> items, IDbTransaction? transaction = null);
    Task<int> CancelPendingAsync(string bookingId, IDbTransaction? transaction = null);
    Task<int> MarkCompletedAsync(string bookingId, IDbTransaction? transaction = null);
}
