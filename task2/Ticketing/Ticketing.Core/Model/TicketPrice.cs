namespace Ticketing.Core.Model;

public class TicketPrice
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }

  public EventSeat? EventSeat { get; set; }
}
