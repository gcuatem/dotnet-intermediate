using System.Data;
using Dapper;
using Ticketing.Core.Enums;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

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

    public async Task<IEnumerable<Seat>> GetSeatsByEventIdAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.QueryAsync<Seat>(EventSqlQueries.GetSeatsByEventId, new { EventId = eventId });
    }

    public async Task<IEnumerable<Seat>> GetAvailableSeatsByEventIdAsync(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        return await _connection.QueryAsync<Seat>(EventSqlQueries.GetAvailableSeats, new
        {
            EventId = eventId,
            Status = SeatStatus.Available
        });
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
        @event.Seats ??= new List<Seat>();

        using var transaction = _connection.BeginTransaction();

        var eventRows = await _connection.ExecuteAsync(EventSqlQueries.Add, @event, transaction);
        var seatRows = 0;

        foreach (var seat in @event.Seats)
        {
            if (seat is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(seat.SeatId))
            {
                seat.SeatId = Guid.NewGuid().ToString();
            }

            seat.EventId = @event.EventId;
            seat.Status = seat.Status == default ? SeatStatus.Available : seat.Status;

            if (string.IsNullOrWhiteSpace(seat.SeatNumber))
            {
                throw new InvalidOperationException("Each seat must have a SeatNumber.");
            }

            if (seat.Price < 0)
            {
                throw new InvalidOperationException("Seat price should be a positive value.");
            }

            seatRows += await _connection.ExecuteAsync(EventSqlQueries.InsertSeat, new
            {
                SeatId = seat.SeatId,
                EventId = seat.EventId,
                SeatNumber = seat.SeatNumber,
                Price = seat.Price,
                Status = seat.Status
            }, transaction);
        }

        transaction.Commit();
        return eventRows + seatRows;
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
  public static string eventFields = @"EventId, Title, Description, EventDate, IsActive, Venue, CreatedAt";
  public static string seatFields = @"SeatId, EventId, SeatNumber, Price, Status";
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
        FROM Seats
        WHERE EventId = @EventId
        ORDER BY Price ASC;";
  public static string GetAvailableSeats = @$"
        SELECT {seatFields}
        FROM Seats
        WHERE EventId = @EventId
          AND Status = @Status
        ORDER BY Price ASC;";
  public static string Add = @$"
        INSERT INTO Events ({eventFields})
        VALUES (
            @EventId,
            @Title,
            @Description,
            @EventDate,
            @IsActive,
            @Venue,
            @CreatedAt);";
  public static string InsertSeat = @$"
        INSERT INTO Seats ({seatFields})
        VALUES (
            @SeatId,
            @EventId,
            @SeatNumber,
            @Price,
            @Status);";
  public static string Update = @"
        UPDATE Events
        SET
            Title = @Title,
            Description = @Description,
            EventDate = @EventDate,
            IsActive = @IsActive,
            Venue = @Venue
        WHERE EventId = @EventId;";
  public static string Delete = @"
        DELETE FROM Events
        WHERE EventId = @EventId;";
}