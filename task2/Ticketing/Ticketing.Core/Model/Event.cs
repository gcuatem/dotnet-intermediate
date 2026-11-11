namespace Ticketing.Core.Model;

public class Event
{
  public string EventId { get; set; } = Guid.NewGuid().ToString();
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime EventDate { get; set; }
  public bool IsActive { get; set; } = true;
  public string Venue { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}