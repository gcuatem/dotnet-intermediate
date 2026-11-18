using System;
using System.Collections.Generic;
using System.Text;
using Ticketing.Core.Model;

namespace Ticketing.Core.Repositories;

public interface IVenueRepository
{
    Task<IEnumerable<Venue>> GetAllAsync();
    Task<Venue?> GetByIdAsync(string venueId);
    Task<IEnumerable<Section>> GetSectionsAsync(string venueId);
    Task<int> AddAsync(Venue venue);
    Task<int> UpdateAsync(Venue venue);
    Task<int> DeleteAsync(string venueId);
}
