using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class BookingItem
{
  public string ItemId { get; set; } = Guid.NewGuid().ToString();
  public string BookingId { get; set; } = string.Empty;
  public string SeatId { get; set; } = string.Empty;
  public string EventId { get; set; } = string.Empty;
  public string TicketQrCode { get; set; } = string.Empty;
  public TicketStatus TicketStatus { get; set; } = TicketStatus.Valid;
  public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ValidUntil { get; set; }
  public Booking Booking { get; set; } = null!;
  public Seat Seat { get; set; } = null!;
}