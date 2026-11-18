using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class BookingItem
{
  public string ItemId { get; set; } = Guid.NewGuid().ToString();
  public string BookingId { get; set; } = string.Empty;
  public string EventSeatId { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string TicketQrCode { get; set; } = string.Empty;
  public TicketStatus TicketStatus { get; set; } = TicketStatus.Valid;
  public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ValidUntil { get; set; }
  public Booking Booking { get; set; } = null!;
  public EventSeat EventSeat { get; set; } = null!;
}