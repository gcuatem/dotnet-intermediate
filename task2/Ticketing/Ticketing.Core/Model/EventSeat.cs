using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class EventSeat
{
  public string EventSeatId { get; set; } = Guid.NewGuid().ToString();
  public string EventId { get; set; } = string.Empty;
  public string SeatId { get; set; } = string.Empty;
  public Guid PriceId { get; set; }
  public SeatStatus Status { get; set; } = SeatStatus.Available;

  public Event Event { get; set; } = null!;
  public Seat Seat { get; set; } = null!;
  public TicketPrice Price { get; set; } = null!;
}
