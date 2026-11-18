using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public sealed record EventSeatSeed(string SeatId);

public class EventRepository : IEventRepository
{
    private readonly IDbConnection _connection;

    public EventRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _connection.QueryAsync<Event>(EventSqlQueries.GetAll);
    }

    public async Task<Event?> GetByIdAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.QuerySingleOrDefaultAsync<Event>(EventSqlQueries.GetById, new { EventId = eventId });
    }

    public async Task<IEnumerable<EventSeatListItem>> GetSeatsByEventIdAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.QueryAsync<EventSeatListItem>(EventSqlQueries.GetSeatsByEventId, new { EventId = eventId });
    }

    public async Task<IEnumerable<EventSeatListItem>> GetAvailableSeatsByEventIdAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.QueryAsync<EventSeatListItem>(EventSqlQueries.GetAvailableSeats, new
        {
            EventId = eventId,
            Status = SeatStatus.Available
        });
    }

    public async Task<IEnumerable<EventSeatListItem>> GetSeatsByEventAndSectionAsync(string eventId, string sectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        return await _connection.QueryAsync<EventSeatListItem>(EventSqlQueries.GetSeatsByEventAndSection,
            new { EventId = eventId, SectionId = sectionId });
    }

    public async Task<int> AddAsync(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.EventId))
        {
            @event.EventId = Guid.NewGuid().ToString();
        }

        if (@event.CreatedAt == default)
        {
            @event.CreatedAt = DateTime.UtcNow;
        }

        if (@event.EventDate == default)
        {
            @event.EventDate = @event.CreatedAt;
        }

        if (@event.EventDate < @event.CreatedAt)
        {
            throw new InvalidOperationException("EventDate must be greater than or equal to CreatedAt.");
        }

        @event.IsActive = true;

        using var transaction = _connection.BeginTransaction();
        try
        {
            var eventRows = await _connection.ExecuteAsync(EventSqlQueries.Add, @event, transaction);
            var seats = (await _connection.QueryAsync<EventSeatSeed>(EventSqlQueries.GetVenueSeats,
                new { @event.VenueId }, transaction)).ToList();

            var requestedPrices = @event.EventSeats
                .Where(es => !string.IsNullOrWhiteSpace(es.SeatId))
                .ToDictionary(es => es.SeatId, es => es.Price, StringComparer.Ordinal);

            var eventSeatRows = 0;
            foreach (var seat in seats)
            {
                var price = requestedPrices.TryGetValue(seat.SeatId, out var configuredPrice)
                    ? configuredPrice
                    : 0m;
                if (price < 0)
                    throw new InvalidOperationException("Event seat price cannot be negative.");

                eventSeatRows += await _connection.ExecuteAsync(EventSqlQueries.AddEventSeat, new
                {
                    EventSeatId = Guid.NewGuid().ToString(),
                    EventId = @event.EventId,
                    SeatId = seat.SeatId,
                    Price = price,
                    Status = SeatStatus.Available
                }, transaction);
            }

            transaction.Commit();
            return eventRows + eventSeatRows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> UpdateAsync(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event.CreatedAt == default)
        {
            @event.CreatedAt = DateTime.UtcNow;
        }

        if (@event.EventDate == default)
        {
            @event.EventDate = @event.CreatedAt;
        }

        if (@event.EventDate < @event.CreatedAt)
        {
            throw new InvalidOperationException("EventDate must be greater than or equal to CreatedAt.");
        }

        return await _connection.ExecuteAsync(EventSqlQueries.Update, @event);
    }

    public async Task<int> DeleteAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.ExecuteAsync(EventSqlQueries.Delete, new { EventId = eventId });
    }
}

public static class EventSqlQueries
{
  public static string eventFields = @"EventId, VenueId, Title, Description, EventDate, IsActive, CreatedAt";
  public static string seatFields = @"es.EventSeatId, s.SeatId, s.SectionId, sec.Name AS SectionName, s.SeatNumber, s.SeatRow, es.Price, es.Status";
  public static string GetAll = @$"
        SELECT {eventFields}
        FROM Events
        ORDER BY EventDate DESC;";
  public static string GetById = @$"
        SELECT {eventFields}
        FROM Events
        WHERE EventId = @EventId;";
  public static string GetSeatsByEventId = @$"
        SELECT {seatFields}
        FROM EventSeats es
        INNER JOIN Seats s ON s.SeatId = es.SeatId
        INNER JOIN Sections sec ON sec.SectionId = s.SectionId
        WHERE es.EventId = @EventId
        ORDER BY es.Price ASC;";
  public static string GetSeatsByEventAndSection = @$"
        SELECT {seatFields}
        FROM EventSeats es
        INNER JOIN Seats s ON s.SeatId = es.SeatId
        INNER JOIN Sections sec ON sec.SectionId = s.SectionId
        WHERE es.EventId = @EventId
          AND s.SectionId = @SectionId
        ORDER BY s.SeatNumber;";
  public static string GetAvailableSeats = @$"
        SELECT {seatFields}
        FROM EventSeats es
        INNER JOIN Seats s ON s.SeatId = es.SeatId
        INNER JOIN Sections sec ON sec.SectionId = s.SectionId
        WHERE es.EventId = @EventId
          AND es.Status = @Status
        ORDER BY es.Price ASC;";
  public static string Add = @$"
        INSERT INTO Events ({eventFields})
        VALUES (
            @EventId,
            @VenueId,
            @Title,
            @Description,
            @EventDate,
            @IsActive,
            @CreatedAt);";
  public const string GetVenueSeats = @"
        SELECT s.SeatId
        FROM Seats s
        INNER JOIN Sections sec ON sec.SectionId = s.SectionId
        WHERE sec.VenueId = @VenueId;";
  public const string AddEventSeat = @"
        INSERT INTO EventSeats (EventSeatId, EventId, SeatId, Price, Status)
        VALUES (@EventSeatId, @EventId, @SeatId, @Price, @Status);";
  public static string Update = @"
        UPDATE Events
        SET
            Title = @Title,
            Description = @Description,
            EventDate = @EventDate,
            IsActive = @IsActive,
            VenueId = @VenueId
        WHERE EventId = @EventId;";
  public static string Delete = @"
        DELETE FROM Events
        WHERE EventId = @EventId;";
}