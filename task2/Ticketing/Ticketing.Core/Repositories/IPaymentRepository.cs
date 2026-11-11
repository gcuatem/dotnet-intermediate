using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(string paymentId);
    Task<Payment?> GetByBookingIdAsync(string bookingId);
    Task<Payment> CreateAsync(Payment payment, IDbTransaction? transaction = null);
}
