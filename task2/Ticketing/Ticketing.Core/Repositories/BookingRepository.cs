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

    public async Task<Booking?> GetByIdAsync(string bookingId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var booking = await _connection.QuerySingleOrDefaultAsync<Booking>(
            BookingSqlQueries.GetByIdForUser,
            new { BookingId = bookingId, UserId = userId });
        if (booking is null) return null;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(booking.UserId);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("A booking must contain at least one item.");
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
            var createdItems = new List<BookingItem>();

            await connection.ExecuteAsync(BookingSqlQueries.InsertBooking, new
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                Status = BookingStatus.Pending,
                ExpiresAt = booking.ExpiresAt,
                CreatedAt = booking.CreatedAt
            }, dbTransaction);

            foreach (var item in itemList)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(item.EventSeatId);

                var eventSeat = await connection.QuerySingleOrDefaultAsync<EventSeat>(
                    BookingSqlQueries.GetEventSeatById,
                    new { EventSeatId = item.EventSeatId }, dbTransaction);
                if (eventSeat is null)
                    throw new InvalidOperationException("Event seat does not exist.");
                if (eventSeat.Status != SeatStatus.Available)
                    throw new InvalidOperationException("Event seat is not available.");

                item.BookingId = booking.BookingId;
                item.ItemId = string.IsNullOrWhiteSpace(item.ItemId) ? Guid.NewGuid().ToString() : item.ItemId;
                item.Price = item.Price == 0 ? eventSeat.Price : item.Price;
                item.TicketQrCode = string.IsNullOrWhiteSpace(item.TicketQrCode)
                    ? $"{eventSeat.EventId}-{eventSeat.SeatId}-{Guid.NewGuid():N}"
                    : item.TicketQrCode;
                item.TicketStatus = TicketStatus.Pending;
                item.IssuedAt = item.IssuedAt == default ? DateTime.UtcNow : item.IssuedAt;

                await connection.ExecuteAsync(BookingSqlQueries.InsertBookingItem, new
                {
                    ItemId = item.ItemId,
                    BookingId = item.BookingId,
                    EventSeatId = item.EventSeatId,
                    Price = item.Price,
                    TicketQrCode = item.TicketQrCode,
                    TicketStatus = item.TicketStatus,
                    IssuedAt = item.IssuedAt,
                    ValidUntil = item.ValidUntil
                }, dbTransaction);

                await connection.ExecuteAsync(BookingSqlQueries.UpdateEventSeatStatus, new
                {
                    EventSeatId = item.EventSeatId,
                    Status = SeatStatus.Locked
                }, dbTransaction);

                createdItems.Add(item);
            }

            booking.Items = createdItems;
            return booking;
        }, transaction);
    }

    public Task<Booking> AddItemAsync(string userId, string eventId, string seatId, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        return AddItemAsync(Guid.NewGuid().ToString(), userId, eventId, seatId, price);
    }

    public async Task<Booking> AddItemAsync(string bookingId, string userId, string eventId, string seatId, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        using var transaction = _connection.BeginTransaction();
        try
        {
            var booking = await _connection.QuerySingleOrDefaultAsync<Booking>(BookingSqlQueries.GetById,
                new { BookingId = bookingId }, transaction);
            if (booking is null)
            {
                booking = new Booking
                {
                    BookingId = bookingId,
                    UserId = userId,
                    Status = BookingStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow
                };
                await _connection.ExecuteAsync(BookingSqlQueries.InsertBooking, new
                {
                    booking.BookingId,
                    UserId = userId,
                    Status = BookingStatus.Pending,
                    booking.ExpiresAt,
                    booking.CreatedAt
                }, transaction);
            }
            else if (!string.Equals(booking.UserId, userId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("The booking does not belong to this user.");
            }

            var eventSeat = await _connection.QuerySingleOrDefaultAsync<EventSeat>(BookingSqlQueries.GetEventSeat,
                new { EventId = eventId, SeatId = seatId }, transaction);
            if (eventSeat is null) throw new InvalidOperationException("The seat is not available for this event.");
            if (eventSeat.Status != SeatStatus.Available) throw new InvalidOperationException("The seat is not available.");

            var existing = await _connection.ExecuteScalarAsync<int>(BookingSqlQueries.CountItem,
                new { BookingId = bookingId, EventSeatId = eventSeat.EventSeatId }, transaction);
            if (existing > 0) throw new InvalidOperationException("The seat is already in the booking.");

            var item = new BookingItem
            {
                BookingId = bookingId,
                EventSeatId = eventSeat.EventSeatId,
                Price = eventSeat.Price,
                TicketStatus = TicketStatus.Pending,
                IssuedAt = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddHours(24)
            };
            await _connection.ExecuteAsync(BookingSqlQueries.InsertBookingItem, new
            {
                item.ItemId,
                item.BookingId,
                item.EventSeatId,
                Price = eventSeat.Price,
                TicketQrCode = $"{eventId}-{seatId}-{Guid.NewGuid():N}",
                item.TicketStatus,
                item.IssuedAt,
                item.ValidUntil
            }, transaction);
            await _connection.ExecuteAsync(BookingSqlQueries.UpdateEventSeatStatus,
                new { EventSeatId = eventSeat.EventSeatId, Status = SeatStatus.Locked }, transaction);

            transaction.Commit();
            booking.Items = (await GetItemsByBookingIdAsync(bookingId)).ToList();
            return booking;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RemoveItemAsync(string bookingId, string userId, string eventId, string seatId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        using var transaction = _connection.BeginTransaction();
        var booking = await _connection.QuerySingleOrDefaultAsync<Booking>(
            BookingSqlQueries.GetByIdForUser,
            new { BookingId = bookingId, UserId = userId }, transaction);
        if (booking is null)
        {
            transaction.Commit();
            return false;
        }

        var eventSeatId = await _connection.ExecuteScalarAsync<string>(BookingSqlQueries.GetEventSeatId,
            new { EventId = eventId, SeatId = seatId }, transaction);
        var deleted = eventSeatId is null ? 0 : await _connection.ExecuteAsync(BookingSqlQueries.DeleteItem,
            new { BookingId = bookingId, EventSeatId = eventSeatId }, transaction);
        if (deleted > 0)
        {
            await _connection.ExecuteAsync(BookingSqlQueries.ReleaseEventSeat,
                new { EventSeatId = eventSeatId, Available = SeatStatus.Available, Locked = SeatStatus.Locked }, transaction);
        }
        transaction.Commit();
        return deleted > 0;
    }

    public async Task<string> MarkBookedAsync(string bookingId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        using var transaction = _connection.BeginTransaction();
        try
        {
            var booking = await _connection.QuerySingleOrDefaultAsync<Booking>(
                BookingSqlQueries.GetByIdForUser,
                new { BookingId = bookingId, UserId = userId }, transaction);
            if (booking is null)
                throw new UnauthorizedAccessException("The booking does not belong to this user.");

            var paymentId = await MarkBookedCoreAsync(bookingId, transaction);
            transaction.Commit();
            return paymentId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task<string> MarkBookedCoreAsync(string bookingId, IDbTransaction transaction)
    {
        var items = (await _connection.QueryAsync<BookingItem>(BookingSqlQueries.GetItemsByBookingId,
            new { BookingId = bookingId }, transaction)).ToList();
        if (items.Count == 0) throw new InvalidOperationException("The booking has no items.");

        await _connection.ExecuteAsync(BookingSqlQueries.MarkCompletedBooking,
            new { BookingId = bookingId, Pending = BookingStatus.Pending, Completed = BookingStatus.Completed }, transaction);
        await _connection.ExecuteAsync(BookingSqlQueries.MarkSeatsSoldForBooking,
            new { BookingId = bookingId, Sold = SeatStatus.Sold, Locked = SeatStatus.Locked }, transaction);
        await _connection.ExecuteAsync(BookingSqlQueries.MarkBookingItemsValid,
            new { BookingId = bookingId, Pending = TicketStatus.Pending, Valid = TicketStatus.Valid, Cancelled = TicketStatus.Cancelled }, transaction);

        var paymentId = Guid.NewGuid().ToString();
        await _connection.ExecuteAsync(PaymentSqlQueries.Insert,
            new
            {
                PaymentId = paymentId,
                BookingId = bookingId,
                Amount = items.Sum(i => i.Price),
                Currency = "USD",
                Status = PaymentStatus.Success,
                GatewayReference = string.Empty,
                Timestamp = DateTime.UtcNow
            }, transaction);
        return paymentId;
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

    public const string GetByIdForUser = @"
        SELECT BookingId, UserId, Status, ExpiresAt, CreatedAt
        FROM Bookings
        WHERE BookingId = @BookingId AND UserId = @UserId;";

    public const string GetByUserId = @"
        SELECT BookingId, UserId, Status, ExpiresAt, CreatedAt
        FROM Bookings
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC;";

    public const string CountItem = @"
        SELECT COUNT(*) FROM BookingItems
        WHERE BookingId = @BookingId AND EventSeatId = @EventSeatId;";

    public const string GetEventSeat = @"
        SELECT EventSeatId, EventId, SeatId, Price, Status
        FROM EventSeats
        WHERE EventId = @EventId AND SeatId = @SeatId;";

    public const string GetEventSeatById = @"
        SELECT EventSeatId, EventId, SeatId, Price, Status
        FROM EventSeats
        WHERE EventSeatId = @EventSeatId;";

    public const string GetEventSeatId = @"
        SELECT EventSeatId FROM EventSeats
        WHERE EventId = @EventId AND SeatId = @SeatId;";

    public const string DeleteItem = @"
        DELETE FROM BookingItems
        WHERE BookingId = @BookingId AND EventSeatId = @EventSeatId;";

    public const string ReleaseEventSeat = @"
        UPDATE EventSeats SET Status = @Available
        WHERE EventSeatId = @EventSeatId AND Status = @Locked;";

    public const string GetItemsByBookingId = @"
        SELECT bi.ItemId, bi.BookingId, bi.EventSeatId, bi.TicketQrCode, bi.TicketStatus, bi.IssuedAt, bi.ValidUntil,
               COALESCE(bi.Price, es.Price) AS Price
        FROM BookingItems bi
        LEFT JOIN EventSeats es ON es.EventSeatId = bi.EventSeatId
        WHERE bi.BookingId = @BookingId
        ORDER BY IssuedAt;";

    public const string GetEventById = @"
        SELECT EventId, VenueId, Title, Description, EventDate, IsActive, CreatedAt
        FROM Events
        WHERE EventId = @EventId;";


    public const string InsertBooking = @"
        INSERT INTO Bookings (BookingId, UserId, Status, ExpiresAt, CreatedAt)
        VALUES (@BookingId, @UserId, @Status, @ExpiresAt, @CreatedAt);";

    public const string InsertBookingItem = @"
        INSERT INTO BookingItems (ItemId, BookingId, EventSeatId, Price, TicketQrCode, TicketStatus, IssuedAt, ValidUntil)
        VALUES (@ItemId, @BookingId, @EventSeatId, @Price, @TicketQrCode, @TicketStatus, @IssuedAt, @ValidUntil);";

    public const string UpdateEventSeatStatus = @"
        UPDATE EventSeats
        SET Status = @Status
        WHERE EventSeatId = @EventSeatId;";

    public const string CancelPendingBooking = @"
        UPDATE Bookings
        SET Status = @Cancelled
        WHERE BookingId = @BookingId
          AND Status = @Pending;";

    public const string ReleaseSeatsForBooking = @"
        UPDATE EventSeats
        SET Status = @Available
        WHERE EventSeatId IN (
            SELECT EventSeatId
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
        UPDATE EventSeats
        SET Status = @Sold
        WHERE EventSeatId IN (
            SELECT EventSeatId
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
