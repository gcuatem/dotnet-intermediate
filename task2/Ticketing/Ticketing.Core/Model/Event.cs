namespace Ticketing.Core.Model;

public class Event
{
  public string EventId { get; set; } = Guid.NewGuid().ToString();
  public string VenueId { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime EventDate { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Venue Venue { get; set; } = null!;
  public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}