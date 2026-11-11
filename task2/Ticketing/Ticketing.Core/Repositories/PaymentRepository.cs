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

          if(payment.Status == PaymentStatus.Success)
          {
            await connection.ExecuteAsync(BookingSqlQueries.MarkCompletedBooking, new
            {
              BookingId = payment.BookingId,
              Pending = BookingStatus.Pending,
              Completed = BookingStatus.Completed
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.MarkSeatsSoldForBooking, new
            {
              BookingId = payment.BookingId,
              Sold = SeatStatus.Sold,
              Locked = SeatStatus.Locked
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.MarkBookingItemsValid, new
            {
              BookingId = payment.BookingId,
              Pending = TicketStatus.Pending,
              Valid = TicketStatus.Valid,
              Cancelled = TicketStatus.Cancelled
            }, dbTransaction);
          }
          else
          {
            await connection.ExecuteAsync(BookingSqlQueries.CancelPendingBooking, new
            {
              BookingId = payment.BookingId,
              Pending = BookingStatus.Pending,
              Cancelled = BookingStatus.Cancelled
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.ReleaseSeatsForBooking, new
            {
              BookingId = payment.BookingId,
              Available = SeatStatus.Available,
              Locked = SeatStatus.Locked
            }, dbTransaction);

            await connection.ExecuteAsync(BookingSqlQueries.CancelBookingItems, new
            {
              BookingId = payment.BookingId,
              Pending = TicketStatus.Pending,
              Cancelled = TicketStatus.Cancelled
            }, dbTransaction);
          }

            return payment;
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

}
