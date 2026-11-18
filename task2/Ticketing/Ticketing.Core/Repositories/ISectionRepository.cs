using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface ISectionRepository
{
    Task<IEnumerable<Section>> GetAllAsync();
    Task<Section?> GetByIdAsync(string sectionId);
    Task<IEnumerable<Section>> GetByVenueIdAsync(string venueId);
    Task<IEnumerable<Seat>> GetSeatsAsync(string sectionId);
    Task<int> AddAsync(Section section);
    Task<int> UpdateAsync(Section section);
    Task<int> DeleteAsync(string sectionId);
    Task<int> AddSeatAsync(Seat seat);
    Task<int> UpdateSeatAsync(Seat seat);
    Task<int> DeleteSeatAsync(string seatId);
}
