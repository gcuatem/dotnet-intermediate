```mermaid
stateDiagram-v2
    [*] --> Available : Manifest Created / Seat Released
    Available --> Locked : Customer Selects Seat (Cart Hold / Timeout)
    Locked --> Available : Reservation Timeout / Customer Removed from Cart
    Locked --> Sold : Payment Confirmed & Completed
    Sold --> Available : Event Manager Cancels Ticket / Refund Processed
    Sold --> [*] : Event Completed
```