using System.Data;
using Dapper;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly IDbConnection _connection;

    public SectionRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IEnumerable<Section>> GetAllAsync() =>
        await _connection.QueryAsync<Section>(SectionSqlQueries.GetAll);

    public async Task<Section?> GetByIdAsync(string sectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        return await _connection.QuerySingleOrDefaultAsync<Section>(SectionSqlQueries.GetById,
            new { SectionId = sectionId });
    }

    public async Task<IEnumerable<Section>> GetByVenueIdAsync(string venueId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueId);
        return await _connection.QueryAsync<Section>(SectionSqlQueries.GetByVenueId,
            new { VenueId = venueId });
    }

    public async Task<IEnumerable<Seat>> GetSeatsAsync(string sectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        return await _connection.QueryAsync<Seat>(SectionSqlQueries.GetSeats,
            new { SectionId = sectionId });
    }

    public async Task<int> AddAsync(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(section.VenueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(section.Name);
        section.SectionId = string.IsNullOrWhiteSpace(section.SectionId)
            ? Guid.NewGuid().ToString() : section.SectionId;

        using var transaction = _connection.BeginTransaction();
        try
        {
            var rows = await _connection.ExecuteAsync(SectionSqlQueries.Add, section, transaction);
            foreach (var seat in section.Seats)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatNumber);
                ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatRow);
                seat.SeatId = string.IsNullOrWhiteSpace(seat.SeatId) ? Guid.NewGuid().ToString() : seat.SeatId;
                seat.SectionId = section.SectionId;
                rows += await _connection.ExecuteAsync(SectionSqlQueries.AddSeat, seat, transaction);
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

    public async Task<int> UpdateAsync(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(section.SectionId);
        return await _connection.ExecuteAsync(SectionSqlQueries.Update, section);
    }

    public async Task<int> DeleteAsync(string sectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
        return await _connection.ExecuteAsync(SectionSqlQueries.Delete, new { SectionId = sectionId });
    }

    public async Task<int> AddSeatAsync(Seat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatRow);
        seat.SeatId = string.IsNullOrWhiteSpace(seat.SeatId) ? Guid.NewGuid().ToString() : seat.SeatId;
        return await _connection.ExecuteAsync(SectionSqlQueries.AddSeat, seat);
    }

    public async Task<int> UpdateSeatAsync(Seat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(seat.SeatRow);
        return await _connection.ExecuteAsync(SectionSqlQueries.UpdateSeat, seat);
    }

    public async Task<int> DeleteSeatAsync(string seatId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);
        return await _connection.ExecuteAsync(SectionSqlQueries.DeleteSeat, new { SeatId = seatId });
    }
}

public static class SectionSqlQueries
{
    public const string GetAll = "SELECT SectionId, VenueId, Name FROM Sections;";
    public const string GetById = "SELECT SectionId, VenueId, Name FROM Sections WHERE SectionId = @SectionId;";
    public const string GetByVenueId = "SELECT SectionId, VenueId, Name FROM Sections WHERE VenueId = @VenueId;";
    public const string GetSeats = "SELECT SeatId, SectionId, SeatNumber, SeatRow FROM Seats WHERE SectionId = @SectionId;";
    public const string Add = "INSERT INTO Sections (SectionId, VenueId, Name) VALUES (@SectionId, @VenueId, @Name);";
    public const string Update = "UPDATE Sections SET VenueId = @VenueId, Name = @Name WHERE SectionId = @SectionId;";
    public const string Delete = "DELETE FROM Sections WHERE SectionId = @SectionId;";
    public const string AddSeat = "INSERT INTO Seats (SeatId, SectionId, SeatNumber, SeatRow) VALUES (@SeatId, @SectionId, @SeatNumber, @SeatRow);";
    public const string UpdateSeat = "UPDATE Seats SET SectionId = @SectionId, SeatNumber = @SeatNumber, SeatRow = @SeatRow WHERE SeatId = @SeatId;";
    public const string DeleteSeat = "DELETE FROM Seats WHERE SeatId = @SeatId;";
}
