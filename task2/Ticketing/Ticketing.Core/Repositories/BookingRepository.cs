using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly IDbConnection _connection;

    public BookingRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Booking?> GetByIdAsync(string bookingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);

        var booking = await _connection.QuerySingleOrDefaultAsync<Booking>(BookingSqlQueries.GetById, new { BookingId = bookingId });
        if (booking is null)
        {
            return null;
        }

        booking.Items = (await GetItemsByBookingIdAsync(bookingId)).ToList();
        return booking;
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var bookings = (await _connection.QueryAsync<Booking>(BookingSqlQueries.GetByUserId, new { UserId = userId })).ToList();
        foreach (var booking in bookings)
        {
            booking.Items = (await GetItemsByBookingIdAsync(booking.BookingId)).ToList();
        }

        return bookings;
    }

    public async Task<IEnumerable<BookingItem>> GetItemsByBookingIdAsync(string bookingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);

        return await _connection.QueryAsync<BookingItem>(BookingSqlQueries.GetItemsByBookingId, new { BookingId = bookingId });
    }

    public async Task<Booking> CreateAsync(Booking booking, IEnumerable<BookingItem> items, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("A booking must contain at least one item.");
        }

        if (string.IsNullOrWhiteSpace(booking.UserId))
        {
            throw new InvalidOperationException("Booking requires a UserId.");
        }

        if (string.IsNullOrWhiteSpace(booking.BookingId))
        {
            booking.BookingId = Guid.NewGuid().ToString();
        }

        if (booking.ExpiresAt == default)
        {
            booking.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
        }

        if (booking.CreatedAt == default)
        {
            booking.CreatedAt = DateTime.UtcNow;
        }

        return await ExecuteInTransactionAsync(async (connection, dbTransaction) =>
        {
            var firstItem = itemList[0];
            if (string.IsNullOrWhiteSpace(firstItem.EventId))
            {
                throw new InvalidOperationException("Every booking item must include an EventId.");
            }

            var eventEntity = await connection.QuerySingleOrDefaultAsync<Event>(BookingSqlQueries.GetEventById, new { EventId = firstItem.EventId }, dbTransaction);
            if (eventEntity is null)
            {
                throw new InvalidOperationException("The event does not exist.");
            }

            if (!eventEntity.IsActive || eventEntity.EventDate < DateTime.UtcNow)
            {
                throw new InvalidOperationException("The event is not available for booking.");
            }

            var createdItems = new List<BookingItem>();
            foreach (var item in itemList)
            {
                if (string.IsNullOrWhiteSpace(item.SeatId))
                {
                    throw new InvalidOperationException("Every booking item must include a SeatId.");
                }

                if (string.IsNullOrWhiteSpace(item.EventId))
                {
                    item.EventId = firstItem.EventId;
                }

                if (!string.Equals(item.EventId, firstItem.EventId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("All booking items must belong to the same event.");
                }

                var seat = await connection.QuerySingleOrDefaultAsync<Seat>(BookingSqlQueries.GetSeatById, new { SeatId = item.SeatId }, dbTransaction);
                if (seat is null)
                {
                    throw new InvalidOperationException($"Seat '{item.SeatId}' does not exist.");
                }

                if (seat.EventId != firstItem.EventId)
                {
                    throw new InvalidOperationException($"Seat '{item.SeatId}' does not belong to the requested event.");
                }

                if (seat.Status != SeatStatus.Available)
                {
                    throw new InvalidOperationException($"Seat '{item.SeatId}' is not available.");
                }

                item.BookingId = booking.BookingId;
                item.ItemId = string.IsNullOrWhiteSpace(item.ItemId) ? Guid.NewGuid().ToString() : item.ItemId;
                item.TicketQrCode = string.IsNullOrWhiteSpace(item.TicketQrCode) ? $"{eventEntity.EventId}-{item.SeatId}-{Guid.NewGuid():N}" : item.TicketQrCode;
                item.TicketStatus = TicketStatus.Pending;
                item.IssuedAt = item.IssuedAt == default ? DateTime.UtcNow : item.IssuedAt;
                item.ValidUntil ??= eventEntity.EventDate;

                createdItems.Add(item);
            }

            await connection.ExecuteAsync(BookingSqlQueries.InsertBooking, new
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                Status = BookingStatus.Pending,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt
            }, dbTransaction);

            foreach (var item in createdItems)
            {
                await connection.ExecuteAsync(BookingSqlQueries.InsertBookingItem, new
                {
                    ItemId = item.ItemId,
                    BookingId = item.BookingId,
                    SeatId = item.SeatId,
                    EventId = item.EventId,
                    TicketQrCode = item.TicketQrCode,
                    TicketStatus = item.TicketStatus,
                    IssuedAt = item.IssuedAt,
                    ValidUntil = item.ValidUntil
                }, dbTransaction);

                await connection.ExecuteAsync(BookingSqlQueries.UpdateSeatStatus, new
                {
                    SeatId = item.SeatId,
                    Status = SeatStatus.Locked
                }, dbTransaction);
            }

            booking.Items = createdItems;
            return booking;
        }, transaction);
    }

    public async Task<int> CancelPendingAsync(string bookingId, IDbTransaction? transaction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);

        return await ExecuteInTransactionAsync(async (connection, dbTransaction) =>
        {
            var bookingUpdated = await connection.ExecuteAsync(BookingSqlQueries.CancelPendingBooking, new
            {
                BookingId = bookingId,
                Pending = BookingStatus.Pending,
                Cancelled = BookingStatus.Cancelled
            }, dbTransaction);

            if (bookingUpdated == 0)
            {
                return 0;
            }

            await connection.ExecuteAsync(BookingSqlQueries.ReleaseSeatsForBooking, new
            {
                BookingId = bookingId,
                Available = SeatStatus.Available,
                Locked = SeatStatus.Locked
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.CancelBookingItems, new
            {
                BookingId = bookingId,
                Pending = TicketStatus.Pending,
                Cancelled = TicketStatus.Cancelled
            }, dbTransaction);

            return bookingUpdated;
        }, transaction);
    }

    public async Task<int> MarkCompletedAsync(string bookingId, IDbTransaction? transaction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);

        return await ExecuteInTransactionAsync(async (connection, dbTransaction) =>
        {
            var bookingUpdated = await connection.ExecuteAsync(BookingSqlQueries.MarkCompletedBooking, new
            {
                BookingId = bookingId,
                Pending = BookingStatus.Pending,
                Completed = BookingStatus.Completed
            }, dbTransaction);

            if (bookingUpdated == 0)
            {
                return 0;
            }

            await connection.ExecuteAsync(BookingSqlQueries.MarkSeatsSoldForBooking, new
            {
                BookingId = bookingId,
                Sold = SeatStatus.Sold,
                Locked = SeatStatus.Locked
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.MarkBookingItemsValid, new
            {
                BookingId = bookingId,
                Pending = TicketStatus.Pending,
                Valid = TicketStatus.Valid,
                Cancelled = TicketStatus.Cancelled
            }, dbTransaction);

            return bookingUpdated;
        }, transaction);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action, IDbTransaction? existingTransaction)
    {
        if (existingTransaction is not null)
        {
            return await action(_connection, existingTransaction);
        }

        using var transaction = _connection.BeginTransaction();
        try
        {
            var result = await action(_connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

public static class BookingSqlQueries
{
    public const string GetById = @"
        SELECT BookingId, UserId, Status, ExpiresAt, CreatedAt
        FROM Bookings
        WHERE BookingId = @BookingId;";

    public const string GetByUserId = @"
        SELECT BookingId, UserId, Status, ExpiresAt, CreatedAt
        FROM Bookings
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC;";

    public const string GetItemsByBookingId = @"
        SELECT ItemId, BookingId, SeatId, EventId, TicketQrCode, TicketStatus, IssuedAt, ValidUntil
        FROM BookingItems
        WHERE BookingId = @BookingId
        ORDER BY IssuedAt;";

    public const string GetEventById = @"
        SELECT EventId, Title, Description, EventDate, IsActive, Venue, CreatedAt
        FROM Events
        WHERE EventId = @EventId;";

    public const string GetSeatById = @"
        SELECT SeatId, EventId, SeatNumber, Price, Status
        FROM Seats
        WHERE SeatId = @SeatId;";

    public const string InsertBooking = @"
        INSERT INTO Bookings (BookingId, UserId, Status, ExpiresAt, CreatedAt)
        VALUES (@BookingId, @UserId, @Status, @ExpiresAt, @CreatedAt);";

    public const string InsertBookingItem = @"
        INSERT INTO BookingItems (ItemId, BookingId, SeatId, EventId, TicketQrCode, TicketStatus, IssuedAt, ValidUntil)
        VALUES (@ItemId, @BookingId, @SeatId, @EventId, @TicketQrCode, @TicketStatus, @IssuedAt, @ValidUntil);";

    public const string UpdateSeatStatus = @"
        UPDATE Seats
        SET Status = @Status
        WHERE SeatId = @SeatId;";

    public const string CancelPendingBooking = @"
        UPDATE Bookings
        SET Status = @Cancelled
        WHERE BookingId = @BookingId
          AND Status = @Pending;";

    public const string ReleaseSeatsForBooking = @"
        UPDATE Seats
        SET Status = @Available
        WHERE SeatId IN (
            SELECT SeatId
            FROM BookingItems
            WHERE BookingId = @BookingId
        )
        AND Status = @Locked;";

    public const string CancelBookingItems = @"
        UPDATE BookingItems
        SET TicketStatus = @Cancelled
        WHERE BookingId = @BookingId
          AND TicketStatus = @Pending;";

    public const string MarkCompletedBooking = @"
        UPDATE Bookings
        SET Status = @Completed
        WHERE BookingId = @BookingId
          AND Status = @Pending;";

    public const string MarkSeatsSoldForBooking = @"
        UPDATE Seats
        SET Status = @Sold
        WHERE SeatId IN (
            SELECT SeatId
            FROM BookingItems
            WHERE BookingId = @BookingId
        )
        AND Status = @Locked;";

    public const string MarkBookingItemsValid = @"
        UPDATE BookingItems
        SET TicketStatus = @Valid
        WHERE BookingId = @BookingId
          AND TicketStatus = @Pending;";
}
