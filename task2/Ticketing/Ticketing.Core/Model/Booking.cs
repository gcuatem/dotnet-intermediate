using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class Booking
{
  public string BookingId { get; set; } = Guid.NewGuid().ToString();
  public string UserId { get; set; } = string.Empty;
  public BookingStatus Status { get; set; } = BookingStatus.Pending;
  public DateTime ExpiresAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public IdentityUser User { get; set; } = null!;
  public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
  public Payment? Payment { get; set; }
}