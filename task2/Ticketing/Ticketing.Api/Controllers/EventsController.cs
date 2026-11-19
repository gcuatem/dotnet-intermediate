using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using Ticketing.Core.Enums;
using Ticketing.Core.Model;
using Ticketing.Core.Repositories;

namespace Ticketing.Api.Controllers;

[Route("events")]
[ApiController]
public sealed class EventsController(IEventRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateEventResponse>> Create(
        [FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = new Event
        {
            VenueId = request.VenueId,
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            EventSeats = request.Seats.Select(seat => new EventSeat
            {
                SeatId = seat.SeatId,
                Price = new TicketPrice
                {
                    Name = seat.Name,
                    Price = seat.Price
                }
            }).ToList()
        };

        await repository.AddAsync(eventEntity);
        return Created($"/events/{eventEntity.EventId}", new CreateEventResponse(
            eventEntity.EventId,
            eventEntity.VenueId,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.EventDate));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> Get(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync());

    [HttpGet("{eventId}/seats/available")]
    public async Task<ActionResult<IEnumerable<EventSeatResponse>>> GetAvailableSeats(
        string eventId, CancellationToken cancellationToken)
    {
        var eventEntity = await repository.GetByIdAsync(eventId);
        if (eventEntity is null) return NotFound();

        var seats = await repository.GetAvailableSeatsByEventIdAsync(eventId);
        var response = seats.Select(seat => new EventSeatResponse(
            seat.SectionId,
            seat.SectionName,
            seat.SeatRow,
            seat.SeatNumber,
            seat.SeatId,
            seat.Status,
            new EventSeatPriceResponse(seat.PriceId, seat.PriceName)));
        return Ok(response);
    }

    [HttpGet("{eventId}/sections/{sectionId}/seats")]
    public async Task<ActionResult<IEnumerable<EventSeatResponse>>> GetSeats(
        string eventId, string sectionId, CancellationToken cancellationToken)
    {
        var eventEntity = await repository.GetByIdAsync(eventId);
        if (eventEntity is null) return NotFound();

        var seats = await repository.GetSeatsByEventAndSectionAsync(eventId, sectionId);
        var response = seats.Select(seat => new EventSeatResponse(
            sectionId,
            seat.SectionName,
            seat.SeatRow,
            seat.SeatNumber,
            seat.SeatId,
            seat.Status,
            new EventSeatPriceResponse(seat.PriceId, seat.PriceName)));
        return Ok(response);
    }
}

public sealed record NamedOption(string Id, string Name);
/// <summary>Payload for creating an event and configuring named venue seat prices.</summary>
public sealed record CreateEventRequest
{
    [Required] public required string VenueId { get; init; }
    [Required] public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    [Required, MinLength(1)] public required IReadOnlyList<CreateEventSeatRequest> Seats { get; init; }
}

/// <summary>Event-specific named price for a venue seat.</summary>
public sealed record CreateEventSeatRequest
{
    [Required] public required string SeatId { get; init; }
    [Required, StringLength(200)] public required string Name { get; init; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal Price { get; init; }
}

/// <summary>Created event response.</summary>
public sealed record CreateEventResponse(string EventId, string VenueId, string Title, string Description, DateTime EventDate);

public sealed record EventSeatPriceResponse(Guid PriceId, string Name);

public sealed record EventSeatResponse(
    string SectionId,
    string SectionName,
    string SeatRow,
    string SeatNumber,
    string SeatId,
    SeatStatus Status,
    EventSeatPriceResponse Price);
