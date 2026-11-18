namespace Ticketing.Core.Model;

public class Seat
{
  public string SeatId { get; set; } = Guid.NewGuid().ToString();
  public string SectionId { get; set; } = string.Empty;
  public string SeatNumber { get; set; } = string.Empty;
  public string SeatRow { get; set; } = string.Empty;
  public Section Section { get; set; } = null!;
  public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}