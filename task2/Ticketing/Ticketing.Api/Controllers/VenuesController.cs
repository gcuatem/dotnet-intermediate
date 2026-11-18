using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;
using Ticketing.Core.Repositories;

namespace Ticketing.Api.Controllers;

[Route("venues")]
[ApiController]
public sealed class VenuesController(IVenueRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Venue>>> Get(CancellationToken cancellationToken)
        => Ok(await repository.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<CreateVenueResponse>> Create(
        [FromBody] CreateVenueRequest request, CancellationToken cancellationToken)
    {
        var venue = new Venue
        {
            Name = request.Name,
            Address = request.Address,
            Sections = request.Sections.Select(section => new Section
            {
                Name = section.Name,
                Seats = section.Seats.Select(seat => new Seat
                {
                    SeatNumber = seat.SeatNumber,
                    SeatRow = seat.SeatRow
                }).ToList()
            }).ToList()
        };

        await repository.AddAsync(venue);
        var response = new CreateVenueResponse(
            venue.VenueId,
            venue.Name,
            venue.Address,
            venue.Sections.Select(section => new CreateSectionResponse(
                section.SectionId,
                section.Name,
                section.Seats.Select(seat => new CreateSeatResponse(
                    seat.SeatId, seat.SeatNumber, seat.SeatRow)).ToList())).ToList());

        return Created($"/venues/{venue.VenueId}", response);
    }

    [HttpGet("{venueId}/sections")]
    public async Task<ActionResult<IEnumerable<Section>>> GetSections(string venueId, CancellationToken cancellationToken)
    {
        var venue = await repository.GetByIdAsync(venueId);
        if (venue is null) return NotFound();
        return Ok(await repository.GetSectionsAsync(venueId));
    }
}

/// <summary>Payload for creating a venue with nested sections and seats.</summary>
public sealed record CreateVenueRequest
{
    [Required] public required string Name { get; init; }
    [Required] public required string Address { get; init; }
    [Required, MinLength(1)] public required IReadOnlyList<CreateSectionRequest> Sections { get; init; }
}

/// <summary>Payload for creating a section and its seats.</summary>
public sealed record CreateSectionRequest
{
    [Required] public required string Name { get; init; }
    [Required, MinLength(1)] public required IReadOnlyList<CreateSeatRequest> Seats { get; init; }
}

/// <summary>Payload for creating a seat.</summary>
public sealed record CreateSeatRequest
{
    [Required] public required string SeatNumber { get; init; }
    [Required] public required string SeatRow { get; init; }
}

/// <summary>Created venue response.</summary>
public sealed record CreateVenueResponse(string VenueId, string Name, string Address, IReadOnlyList<CreateSectionResponse> Sections);

/// <summary>Created section response.</summary>
public sealed record CreateSectionResponse(string SectionId, string Name, IReadOnlyList<CreateSeatResponse> Seats);

/// <summary>Created seat response.</summary>
public sealed record CreateSeatResponse(string SeatId, string SeatNumber, string SeatRow);
