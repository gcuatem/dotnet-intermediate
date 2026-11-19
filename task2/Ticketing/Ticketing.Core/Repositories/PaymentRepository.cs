using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnection _connection;

    public PaymentRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Payment?> GetByIdAsync(string paymentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId);

        return await _connection.QuerySingleOrDefaultAsync<Payment>(PaymentSqlQueries.GetById, new { PaymentId = paymentId });
    }

    public async Task<Payment?> GetByBookingIdAsync(string bookingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingId);

        return await _connection.QuerySingleOrDefaultAsync<Payment>(PaymentSqlQueries.GetByBookingId, new { BookingId = bookingId });
    }

    public async Task<Payment> CreateAsync(Payment payment, IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (string.IsNullOrWhiteSpace(payment.BookingId))
        {
            throw new InvalidOperationException("A payment must belong to a booking.");
        }

        return await ExecuteInTransactionAsync(async (connection, dbTransaction) =>
        {
            payment.PaymentId = string.IsNullOrWhiteSpace(payment.PaymentId) ? Guid.NewGuid().ToString() : payment.PaymentId;
            payment.Status = PaymentStatus.Pending;
            payment.Timestamp = payment.Timestamp == default ? DateTime.UtcNow : payment.Timestamp;

            await connection.ExecuteAsync(PaymentSqlQueries.Insert, new
            {
                PaymentId = payment.PaymentId,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                GatewayReference = payment.GatewayReference,
                Timestamp = payment.Timestamp
            }, dbTransaction);

            return payment;
        }, transaction);
    }


    public Task<bool> CompleteAsync(string paymentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId);

        return ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var payment = await connection.QuerySingleOrDefaultAsync<Payment>(
                PaymentSqlQueries.GetById,
                new { PaymentId = paymentId },
                transaction);
            if (payment is null || payment.Status != PaymentStatus.Pending)
            {
                return false;
            }

            var paymentUpdated = await connection.ExecuteAsync(PaymentSqlQueries.MarkStatus,
                new { PaymentId = paymentId, Status = PaymentStatus.Success, Pending = PaymentStatus.Pending },
                transaction);
            if (paymentUpdated == 0) return false;

            await connection.ExecuteAsync(BookingSqlQueries.MarkCompletedBooking, new
            {
                BookingId = payment.BookingId,
                Pending = BookingStatus.Pending,
                Completed = BookingStatus.Completed
            }, transaction);
            await connection.ExecuteAsync(BookingSqlQueries.MarkSeatsSoldForBooking, new
            {
                BookingId = payment.BookingId,
                Sold = SeatStatus.Sold,
                Locked = SeatStatus.Locked
            }, transaction);
            await connection.ExecuteAsync(BookingSqlQueries.MarkBookingItemsValid, new
            {
                BookingId = payment.BookingId,
                Pending = TicketStatus.Pending,
                Valid = TicketStatus.Valid,
                Cancelled = TicketStatus.Cancelled
            }, transaction);

            return true;
        });
    }

    public Task<bool> FailAsync(string paymentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId);

        return ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var payment = await connection.QuerySingleOrDefaultAsync<Payment>(
                PaymentSqlQueries.GetById,
                new { PaymentId = paymentId },
                transaction);
            if (payment is null || payment.Status != PaymentStatus.Pending)
            {
                return false;
            }

            var paymentUpdated = await connection.ExecuteAsync(PaymentSqlQueries.MarkStatus,
                new { PaymentId = paymentId, Status = PaymentStatus.Failed, Pending = PaymentStatus.Pending },
                transaction);
            if (paymentUpdated == 0) return false;

            await connection.ExecuteAsync(BookingSqlQueries.CancelPendingBooking, new
            {
                BookingId = payment.BookingId,
                Pending = BookingStatus.Pending,
                Cancelled = BookingStatus.Cancelled
            }, transaction);
            await connection.ExecuteAsync(BookingSqlQueries.ReleaseSeatsForBooking, new
            {
                BookingId = payment.BookingId,
                Available = SeatStatus.Available,
                Locked = SeatStatus.Locked
            }, transaction);
            await connection.ExecuteAsync(BookingSqlQueries.CancelBookingItems, new
            {
                BookingId = payment.BookingId,
                Pending = TicketStatus.Pending,
                Cancelled = TicketStatus.Cancelled
            }, transaction);

            return true;
        });
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action, IDbTransaction? existingTransaction = null)
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

public static class PaymentSqlQueries
{
    public const string GetById = @"
        SELECT PaymentId, BookingId, Amount, Currency, Status, GatewayReference, Timestamp
        FROM Payments
        WHERE PaymentId = @PaymentId;";

    public const string GetByBookingId = @"
        SELECT PaymentId, BookingId, Amount, Currency, Status, GatewayReference, Timestamp
        FROM Payments
        WHERE BookingId = @BookingId;";

    public const string Insert = @"
        INSERT INTO Payments (PaymentId, BookingId, Amount, Currency, Status, GatewayReference, Timestamp)
        VALUES (@PaymentId, @BookingId, @Amount, @Currency, @Status, @GatewayReference, @Timestamp);";

    public const string MarkStatus = @"
        UPDATE Payments
        SET Status = @Status
        WHERE PaymentId = @PaymentId AND Status = @Pending;";

}
