### 1. Benefits and drawbacks of async programming
* **Benefits:**
- Improves scalability by freeing threads while waiting for I/O operations.

- Increases application responsiveness.

- Handles more concurrent requests with fewer threads.

- Prevents thread blocking during database calls, HTTP requests, and file operations.

* **Drawbacks:** 
- More complex code and debugging.

- Can introduce race conditions if shared state is not handled correctly.

- Requires proper use of async and await.

- Provides little benefit for CPU-intensive operations by itself.

---

### 2. How to make an ASP.NET controller action support async flow
* Mark the action method with the `async` keyword.
* Return `Task<IActionResult>` or `Task<ActionResult<T>>` (or `Task` if returning no content).
* Use `await` on asynchronous operations (e.g., `await _service.GetAsync(cancellationToken)`) and propagate `CancellationToken` down the call stack.

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken)
{
    var order = await _orderService.GetByIdAsync(id, cancellationToken);
    return order is null ? NotFound() : Ok(order);
}
```
> Is Mandatory to use both `async` and `await`, if you miss any of them you will get an error
---

### 3. How async flow influences ASP.NET request execution (life cycle)
- ASP.NET receives the HTTP request.

- A thread starts executing the controller action.

- When execution reaches an incomplete await, the thread is released to handle other requests.

- The asynchronous operation continues in the background.

- Once the operation completes, ASP.NET schedules the continuation.

- The controller resumes execution and returns the HTTP response.

> Async does not necessarily make an individual request faster; it improves server scalability by avoiding blocked threads while waiting for I/O.
---

### 4. At least 5 tips on ASP.NET API performance best practices
1. **Use `async`/`await` end-to-end** for I/O-bound work without blocking calls.
2. **Optimize database queries** Avoid unnecessary queries and use indexes.
3. **Implement caching** for frequently accessed data.
4. **Optimize serialization** Return only the necessary data.
5. **Use logging efficiently**: Avoid excessive synchronous logging.

---

### 5. Vertical vs. Horizontal scalability (and where to use each)
* **Vertical (*scale up*):** Adding more CPU, RAM, or storage to a single machine.
  * *Where to use:* Monolithic apps, relational databases that are hard to shard, or early-stage systems where operational simplicity matters.
* **Horizontal (*scale out*):** Adding more instances/nodes behind a load balancer.
  * *Where to use:* Stateless APIs, high or spiky traffic, distributed microservices, and systems requiring high availability and fault tolerance.

---

### 6. Why the `PUT` method was suggested for the `book` action on the order

**Reasons:**

- `PUT` is idempotent: sending the same request multiple times produces the same result.

- It clearly identifies the resource being modified.

- It follows RESTful API conventions.

**Important distinction:**

`PUT`: Replace or set the resource state.

`PATCH`: Partially update an existing resource.

`POST`: Create a resource or perform an action that is not necessarily idempotent.