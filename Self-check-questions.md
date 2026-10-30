# self-check-questions

## 1. What diagram types do you have on your current project?

* **Component Diagram (Task 1):** Illustrates the high-level system components (API Gateway, Auth Service, Booking, Inventory, Payment, etc.).

* **State Machine Diagram (Task 2):** Depicts the lifecycle states of a seat (`Available`, `Booked`, `Sold`) and the transitions triggered by customer interactions or event manager actions.
* **Sequence Diagram (Task 3):** Details the step-by-step sequential interactions, message exchanges, and API calls between the frontend, and backend microservices during a purchase workflow.

---

## 2. What are the pros and cons of UML diagrams?

### Pros
* **Standardization:** Provides a universal, industry-recognized notation that engineers, architects, and stakeholders can easily interpret.
* **Clarity on Complexity:** Visualizes complex business logic, concurrency constraints, and distributed workflows much more effectively than descriptive text.
* **Early Validation:** Helps identify architectural bottlenecks, security boundaries (like missing authentication or payment flows), and missing components *before* writing code.
* **Living Documentation:** Serves as a baseline reference for onboarding new team members and system maintenance.

### Cons
* **Maintenance Overhead:** Diagrams can quickly become outdated ("documentation drift") if codebases evolve without updating the visual models.
* **Over-Engineering:** Spending excessive time detailing every minor class or method can diminish returns on investment.
* **Clutter:** Dense diagrams can become difficult to read if they attempt to capture too many concerns in a single view.

---

## 3. What is the difference between a structural diagram and a behavioral diagram in UML?

* **Structural Diagrams:** Focus on the **static** architecture of a system—the physical or logical components and how they are organized and connected. They answer the question *what* parts exist (e.g., Component Diagrams, Class Diagrams, Deployment Diagrams).
* **Behavioral Diagrams:** Focus on the **dynamic** aspects of a system—how it changes, executes processes, or responds to stimuli over time. They answer the question *how* the system behaves in action (e.g., Sequence Diagrams, State Machine Diagrams, Activity Diagrams).

---

## 4. Name the relationship types in a use case diagram

In a UML use case diagram, the four primary relationship types are:

1. **Association:** Connects an Actor to a Use Case, indicating participation in that behavior.
2. **Include (`<<include>>`):** Indicates that a base use case unconditionally incorporates the behavior of another sub-use case (promoting modular reuse).
3. **Extend (`<<extend>>`):** Indicates that a use case optionally extends the behavior of another under specific conditions or extension points.
4. **Generalization:** Represents inheritance hierarchies between actors (e.g., a "Registered Customer" inheriting from a general "User" actor) or between use cases.

---

## 5. What is a sequence diagram?

A **sequence diagram** is a behavioral UML diagram that illustrates how participants (objects, services, or actors) interact with one another in a specific scenario. It emphasizes **time ordering**, displaying messages, HTTP requests, and events passed back and forth vertically down a timeline, making it ideal for visualizing distributed request-response lifecycles and transaction flows.