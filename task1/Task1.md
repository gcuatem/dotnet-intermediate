```mermaid
graph TD
    Client[Frontend:<br>Customer & Event Manager UI] --> APIGateway[API Gateway]
    
    APIGateway -->|Token Validated| EventMgmtSvc["Catalog & Search Service"]
    APIGateway -->|Token Validated| InventorySvc["Inventory & Seat Management Service"]
    APIGateway -->|Token Validated| BookingSvc[ Booking/Cart Service]
    APIGateway -->|Token Validated| TicketSvc["Ticketing Service"]
    APIGateway -->|Token Validated| PaymentSvc[Payment Service]
    
    PaymentSvc --> ExtGateway[External Payment Gateways<br/>Stripe / PayPal / etc.]
    
    subgraph Backend
    BookingSvc --> NotifSvc[Notification Service]
        APIGateway --> AuthSvc[Auth & Identity Service<br/>]
        InventorySvc --> Redis[(Redis Cache: Seat Locks)]
        EventMgmtSvc --> DB[(Relational Database)]
        InventorySvc --> DB
        BookingSvc --> DB
        PaymentSvc --> DB
        TicketSvc --> DB
        AuthSvc --> DB
    end
```
