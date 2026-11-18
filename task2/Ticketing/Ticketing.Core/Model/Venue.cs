namespace Ticketing.Core.Model;

public class Venue
{
  public string VenueId { get; set; } = Guid.NewGuid().ToString();
  public string Name { get; set; } = string.Empty;
  public string Address { get; set; } = string.Empty;
  public ICollection<Section> Sections { get; set; } = new List<Section>();
  public ICollection<Event> Events { get; set; } = new List<Event>();
}
