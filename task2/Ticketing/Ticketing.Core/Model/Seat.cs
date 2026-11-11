using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class Seat
{
  public string SeatId { get; set; } = Guid.NewGuid().ToString();
  public string EventId { get; set; } = string.Empty;
  public string SeatNumber { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public SeatStatus Status { get; set; } = SeatStatus.Available;
  public Event Event { get; set; } = null!;
}