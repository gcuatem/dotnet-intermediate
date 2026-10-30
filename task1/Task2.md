```mermaid
stateDiagram-v2
    [*] --> Available : Manifest Created / Seat Released
    Available --> Booked : Customer Selects Seat (Cart Hold / Timeout)
    Booked --> Available : Reservation Timeout / Customer Removed from Cart
    Booked --> Sold : Payment Confirmed & Completed
    Sold --> Available : Event Manager Cancels Ticket / Refund Processed
    Sold --> [*] : Event Completed
```