using System;
using System.Collections.Generic;
using System.Text;
using Ticketing.Core.Enums;

namespace Ticketing.Core.Model;

public class Payment
{
  public string PaymentId { get; set; } = Guid.NewGuid().ToString();
  public string BookingId { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string Currency { get; set; } = "USD";
  public PaymentStatus Status { get; set; } = PaymentStatus.Success;
  public string GatewayReference { get; set; } = string.Empty;
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  public Booking Booking { get; set; } = null!;
}