using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Ticketing.Core.Repositories;

namespace Ticketing.Api.Controllers;

[Route("booking")]
[ApiController]
public sealed class BookingController(IBookingRepository repository) : ControllerBase
{
    [HttpGet("{bookingId}")]
    public async Task<IActionResult> Get(string bookingId, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

        var booking = await repository.GetByIdAsync(bookingId, userId);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingStateResponse>> Create(
        [FromBody] AddBookingItemRequest request, CancellationToken cancellationToken)
    {
        var booking = await repository.AddItemAsync(request.UserId, request.EventId, request.SeatId, request.Price);
        var total = booking.Items.Sum(item => item.Price);
        return Created($"/booking/{booking.BookingId}",
            new BookingStateResponse(booking.BookingId, booking.Status.ToString(), total));
    }

    [HttpPost("{bookingId}")]
    public async Task<ActionResult<BookingStateResponse>> AddItem(
        string bookingId, [FromBody] AddBookingItemRequest request, CancellationToken cancellationToken)
    {
        var booking = await repository.AddItemAsync(bookingId, request.UserId, request.EventId, request.SeatId, request.Price);
        var total = booking.Items.Sum(item => item.Price);
        return Ok(new BookingStateResponse(bookingId, booking.Status.ToString(), total));
    }

    [HttpDelete("{bookingId}/events/{eventId}/seats/{seatId}")]
    public async Task<IActionResult> RemoveItem(
        string bookingId, string eventId, string seatId, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

        var removed = await repository.RemoveItemAsync(bookingId, userId, eventId, seatId);
        return removed ? NoContent() : NotFound();
    }

    [HttpPut("{bookingId}/book")]
    public async Task<ActionResult<PaymentReferenceResponse>> Book(
        string bookingId, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

        var paymentId = await repository.MarkBookedAsync(bookingId, userId);
        return Ok(new PaymentReferenceResponse(paymentId));
    }
}

/// <summary>Payload for adding an event seat to a client-generated booking.</summary>
public sealed record AddBookingItemRequest
{
    [System.ComponentModel.DataAnnotations.Required] public required string UserId { get; init; }
    [System.ComponentModel.DataAnnotations.Required] public required string EventId { get; init; }
    [System.ComponentModel.DataAnnotations.Required] public required string SeatId { get; init; }
    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "999999.99")] public decimal Price { get; init; }
}
public sealed record BookingStateResponse(string BookingId, string Status, decimal TotalAmount);
public sealed record PaymentReferenceResponse(string PaymentId);
