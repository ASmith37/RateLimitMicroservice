# **Message Rate Limiting Service Specification**

### **Overview**
This microservice is a C#/.NET Core project written in C# designed to enforce rate limits for sending messages from business phone numbers. Its primary responsibility is to determine if a message can be sent without exceeding the provider’s per-phone-number and per-account limits. The service will be called by various applications across the infrastructure, ensuring that unnecessary API calls to external providers are avoided when limits are exceeded.

---

### **API Specification**

- **Endpoint**: `/api/message/check-sendability` (POST)
- **Purpose**: Checks if a message can be sent from a given phone number without exceeding the rate limits, and records the request.
  
**Request Payload**:
```json
{
  "accountNumber": "12345",
  "phoneNumber": "555-1234"
}
```

**Response**:
- **Success (`true`)**: The message can be sent within the limits.
- **Failure (`false`)**: The message cannot be sent because a rate limit is exceeded. The response includes which limit was exceeded (either `per-account` or `per-phone-number`).

Example:
```json
{
  "canSend": false,
  "exceededLimit": "per-account"
}
```

---

### **Rate Limit Definitions**

1. **Per-phone-number limit**: The maximum number of messages that can be sent from a single business phone number per second.
2. **Per-account limit**: The maximum number of messages that can be sent from the entire account per second.

---

### **Data Storage**

**Database**: SQLite  
Used for tracking message logs and storing rate limits.

#### **Tables**

1. **MessageLog**  
   - **Purpose**: Tracks each message request for auditing and enforcement.
   - **Columns**:
     - `id` (Primary Key)
     - `timestamp` (UTC timestamp of the request)
     - `accountNumber` (Text)
     - `phoneNumber` (Text)

**Indexing**:
- **MessageLog**: Indexes on `timestamp`, `accountNumber`, and `phoneNumber` to optimize real-time queries.

---

### **Rate Limit Enforcement**

1. **Lookup Rate Limits**:
   - Use hard-coded limits:
     - Per-phone-number limit: 10 messages per second.
     - Per-account limit: 20 messages per second.
   
2. **Count Recent Messages**:
   - Query the `MessageLog` table to count the number of messages sent within the last second.
   
3. **Decision Logic**:
   - **Allow**: If the count is below the defined limit.
   - **Reject**: If the count equals or exceeds the limit, returning which limit was exceeded (either `per-phone-number` or `per-account`).

---

### **Data Cleanup Strategy**

- **Hybrid Approach**:
  - **Real-time Enforcement**: Only records from the last second are used to enforce limits.
  - **Periodic Cleanup**: A scheduled background task (using an `IHostedService` or a similar timer mechanism) runs every hour to purge records older than 1 hour from the `MessageLog` table.


---

### **Error Handling**

- **Rate Limit Exceeded**:  
  The API response will indicate which limit was exceeded using the `exceededLimit` field.
  
- **Database or Internal Errors**:  
  Return a `500 Internal Server Error` with a descriptive error message if an internal issue occurs (e.g., database connectivity issues).

---

### **Performance and High Traffic Considerations**

- **Optimized Queries**:  
  Use efficient SQL queries with proper indexes on `MessageLog` and `RateLimits` to ensure the service can handle high traffic loads.
  
- **Non-blocking Design**:  
  The cleanup process is handled in the background to prevent it from impacting the real-time rate check performance.

---

### **Testing Plan**

#### **Unit Tests**
- **Rate Limit Logic**:
  - Validate that the service correctly enforces rate limits for both per-phone-number and per-account scenarios.
  - Test boundary conditions (e.g., counts exactly at the limit).
  
- **Database Interaction**:
  - Ensure that the `MessageLog` table is updated correctly when requests are processed.
  - Verify that rate limits are correctly retrieved from the `RateLimits` table.
  - Simulate cleanup operations to confirm that messages older than 1 day are purged.

#### **Integration Tests**
- **API Behavior**:
  - Simulate API calls with different `accountNumber` and `phoneNumber` values.
  - Test scenarios where the rate limits are both within and exceeding the allowed thresholds.
  
- **End-to-End Workflow**:
  - Validate that a full workflow from receiving an API request, processing it against rate limits, logging the message, and returning the correct response functions correctly.

#### **Optional Stress/Load Tests**
- Simulate high traffic by generating numerous simultaneous API requests to ensure that the service maintains performance under load.

---

### **Developer Next Steps**

1. **Environment Setup**:
   - Configure the C#/.NET Core project.
   - Set up SQLite as the data store.
   
2. **Implement the Database Schema**:
   - Create the `MessageLog` and `RateLimits` tables along with the necessary indexes.
   
3. **Develop API Endpoint**:
   - Implement the `/api/message/check-sendability` POST endpoint.
   - Implement the logic for rate limit checks based on the data from SQLite.
   
4. **Background Cleanup Task**:
   - Create a scheduled task using `IHostedService` (or an equivalent mechanism) to run the cleanup query every hour.
   
5. **Implement Error Handling**:
   - Ensure proper handling and logging of errors (e.g., database errors, rate limit breaches).
   
6. **Testing**:
   - Write unit tests for the rate limit logic and database interactions.
   - Create integration tests simulating API calls and full workflows.
   - Optionally, perform stress/load tests.

This complete specification provides everything needed—a clear API definition, detailed data handling, architecture choices, error strategies, and testing plans—to get started on the implementation of this microservice in a C#/.NET Core environment.