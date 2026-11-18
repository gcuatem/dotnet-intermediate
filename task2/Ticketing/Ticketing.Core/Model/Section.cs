using System;
using System.Collections.Generic;
using System.Text;

namespace Ticketing.Core.Model;

public class Section
{
  public string SectionId { get; set; } = Guid.NewGuid().ToString();
  public string VenueId { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public Venue Venue { get; set; } = null!;
  public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
