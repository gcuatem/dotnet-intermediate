using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

using Ticketing.Core.Repositories;

namespace Ticketing.Api.Controllers;

[Route("payments")]
[ApiController]
public sealed class PaymentsController(IPaymentRepository repository, IBookingRepository bookingRepository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create(
        [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, request.UserId);
        if (booking is null) return NotFound();

        var existing = await repository.GetByBookingIdAsync(request.BookingId);
        if (existing is not null) return Ok(ToResponse(existing));

        var items = (await bookingRepository.GetItemsByBookingIdAsync(request.BookingId)).ToList();
        if (items.Count == 0) return BadRequest("Booking has no items.");

        var amount = items.Sum(item => item.Price);
        var payment = await repository.CreateAsync(new Payment
        {
            BookingId = request.BookingId,
            Amount = amount,
            Currency = request.Currency,
            Status = PaymentStatus.Pending,
            GatewayReference = request.GatewayReference
        });

        return Created($"/payments/{payment.PaymentId}", ToResponse(payment));
    }

    [HttpPost("{paymentId}/complete")]
    public async Task<ActionResult<PaymentResponse>> Complete(
        string paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(paymentId);
        if (payment is null) return NotFound();
        if (!await repository.CompleteAsync(paymentId)) return Conflict("Payment is not pending.");

        payment = await repository.GetByIdAsync(paymentId);
        return payment is null ? NotFound() : Ok(ToResponse(payment));
    }

    [HttpPost("{paymentId}/failed")]
    public async Task<ActionResult<PaymentResponse>> Fail(
        string paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(paymentId);
        if (payment is null) return NotFound();
        if (!await repository.FailAsync(paymentId)) return Conflict("Payment is not pending.");

        payment = await repository.GetByIdAsync(paymentId);
        return payment is null ? NotFound() : Ok(ToResponse(payment));
    }

    [HttpGet("{paymentId}")]
    public async Task<ActionResult<PaymentResponse>> Get(string paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(paymentId);
        return payment is null ? NotFound() : Ok(ToResponse(payment));
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<ActionResult<PaymentResponse>> GetByBooking(
        string bookingId, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

        var booking = await bookingRepository.GetByIdAsync(bookingId, userId);
        if (booking is null) return NotFound();

        var payment = await repository.GetByBookingIdAsync(bookingId);
        return payment is null ? NotFound() : Ok(ToResponse(payment));
    }

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.PaymentId,
        payment.BookingId,
        payment.Amount,
        payment.Currency,
        payment.Status,
        payment.GatewayReference,
        new DateTimeOffset(payment.Timestamp, TimeSpan.Zero));
}

/// <summary>Payload for registering a payment for a booking.</summary>
public sealed record CreatePaymentRequest
{
    [Required] public required string UserId { get; init; }
    [Required] public required string BookingId { get; init; }
    [Required, MinLength(3), MaxLength(3)] public string Currency { get; init; } = "USD";
    public string GatewayReference { get; init; } = string.Empty;
}

/// <summary>Represents a payment and its current lifecycle status.</summary>
public sealed record PaymentResponse(
    string PaymentId,
    string BookingId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string GatewayReference,
    DateTimeOffset Timestamp);
