using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly IDbConnection _connection;

    public VenueRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IEnumerable<Venue>> GetAllAsync()
    {
        using var results = await _connection.QueryMultipleAsync(VenueSqlQueries.GetAll);

        var venues = (await results.ReadAsync<Venue>()).ToList();
        var sections = (await results.ReadAsync<Section>()).ToList();
        var seats = (await results.ReadAsync<Seat>()).ToList();
        var events = (await results.ReadAsync<Event>()).ToList();

        var sectionsByVenue = sections
            .GroupBy(section => section.VenueId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var seatsBySection = seats
            .GroupBy(seat => seat.SectionId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var eventsByVenue = events
            .GroupBy(@event => @event.VenueId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var section in sections)
        {
            section.Seats = seatsBySection.TryGetValue(section.SectionId, out var sectionSeats)
                ? sectionSeats
                : new List<Seat>();
        }

        foreach (var venue in venues)
        {
            venue.Sections = sectionsByVenue.TryGetValue(venue.VenueId, out var venueSections)
                ? venueSections
                : new List<Section>();
            venue.Events = eventsByVenue.TryGetValue(venue.VenueId, out var venueEvents)
                ? venueEvents
                : new List<Event>();
        }

        return venues;
    }

    public async Task<Venue?> GetByIdAsync(string venueId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueId);
        return await _connection.QuerySingleOrDefaultAsync<Venue>(VenueSqlQueries.GetById, new { VenueId = venueId });
    }

    public async Task<IEnumerable<Section>> GetSectionsAsync(string venueId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueId);
        return await _connection.QueryAsync<Section>(VenueSqlQueries.GetSections, new { VenueId = venueId });
    }

    public async Task<int> AddAsync(Venue venue)
    {
        ArgumentNullException.ThrowIfNull(venue);
        venue.VenueId = string.IsNullOrWhiteSpace(venue.VenueId) ? Guid.NewGuid().ToString() : venue.VenueId;
        if (string.IsNullOrWhiteSpace(venue.Name)) throw new InvalidOperationException("Venue name is required.");

        using var transaction = _connection.BeginTransaction();
        try
        {
            var rows = await _connection.ExecuteAsync(VenueSqlQueries.Add, venue, transaction);
            foreach (var section in venue.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Name))
                    throw new InvalidOperationException("Section name is required.");

                section.SectionId = string.IsNullOrWhiteSpace(section.SectionId)
                    ? Guid.NewGuid().ToString() : section.SectionId;
                section.VenueId = venue.VenueId;
                rows += await _connection.ExecuteAsync(VenueSqlQueries.AddSection, section, transaction);

                foreach (var seat in section.Seats)
                {
                    if (string.IsNullOrWhiteSpace(seat.SeatNumber))
                        throw new InvalidOperationException("Seat number is required.");
                    if (string.IsNullOrWhiteSpace(seat.SeatRow))
                        throw new InvalidOperationException("Seat row is required.");
                    seat.SeatId = string.IsNullOrWhiteSpace(seat.SeatId)
                        ? Guid.NewGuid().ToString() : seat.SeatId;
                    seat.SectionId = section.SectionId;
                    rows += await _connection.ExecuteAsync(VenueSqlQueries.AddSeat, seat, transaction);
                }
            }
            transaction.Commit();
            return rows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> UpdateAsync(Venue venue)
    {
        ArgumentNullException.ThrowIfNull(venue);
        ArgumentException.ThrowIfNullOrWhiteSpace(venue.VenueId);
        return await _connection.ExecuteAsync(VenueSqlQueries.Update, venue);
    }

    public async Task<int> DeleteAsync(string venueId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueId);
        return await _connection.ExecuteAsync(VenueSqlQueries.Delete, new { VenueId = venueId });
    }
}

public static class VenueSqlQueries
{
    public const string GetAll = @"
        SELECT VenueId, Name, Address FROM Venues;
        SELECT SectionId, VenueId, Name FROM Sections;
        SELECT SeatId, SectionId, SeatNumber, SeatRow FROM Seats;
        SELECT EventId, VenueId, Title, Description, EventDate, IsActive, CreatedAt FROM Events;";
    public const string GetById = "SELECT VenueId, Name, Address FROM Venues WHERE VenueId = @VenueId;";
    public const string GetSections = "SELECT SectionId, VenueId, Name FROM Sections WHERE VenueId = @VenueId;";
    public const string Add = "INSERT INTO Venues (VenueId, Name, Address) VALUES (@VenueId, @Name, @Address);";
    public const string AddSection = "INSERT INTO Sections (SectionId, VenueId, Name) VALUES (@SectionId, @VenueId, @Name);";
    public const string AddSeat = "INSERT INTO Seats (SeatId, SectionId, SeatNumber, SeatRow) VALUES (@SeatId, @SectionId, @SeatNumber, @SeatRow);";
    public const string Update = "UPDATE Venues SET Name = @Name, Address = @Address WHERE VenueId = @VenueId;";
    public const string Delete = "DELETE FROM Venues WHERE VenueId = @VenueId;";
}
