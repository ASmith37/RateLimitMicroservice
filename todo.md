# TODO: Message Rate Limiting Service Checklist

## Environment Setup
- [ ] **Project Initialization**
  - [x] Create a new .NET Core Web API project named "MessageRateLimiter" using the command:
    ```
    dotnet new webapi -n MessageRateLimiter
    ```
- [ ] **NuGet Package Installation**
  - [x] Add the following NuGet packages:
    - Microsoft.EntityFrameworkCore.Sqlite
    - Microsoft.EntityFrameworkCore.Design
- [ ] **Project Verification**
  - [ ] Ensure the project builds successfully.
  - [ ] Verify initial run with a simple endpoint or health-check.

---

## Database Schema Implementation
- [ ] **Directory Structure**
  - [ ] Create a folder named `Data` for the DbContext and entity classes.
- [ ] **Entity Creation**
  - [ ] **MessageLog Entity**
    - [ ] Define properties:
      - `id` (Primary Key)
      - `timestamp` (UTC timestamp of the request)
      - `accountNumber` (Text)
      - `phoneNumber` (Text)
  - [ ] **RateLimits Entity**
    - [ ] Define properties:
      - `id` (Primary Key)
      - `accountNumber` (Text, Nullable – NULL indicates a global default)
      - `phoneNumber` (Text, Nullable – NULL indicates an account-wide setting)
      - `maxMessagesPerSecond` (Integer)
- [ ] **Indexing**
  - [ ] For `MessageLog`: Create indexes on `timestamp`, `accountNumber`, and `phoneNumber`.
  - [ ] For `RateLimits`: Create indexes on `accountNumber` and `phoneNumber`.
- [ ] **DbContext Configuration**
  - [ ] Configure the DbContext to include both entities.
  - [ ] Apply Fluent API or data annotations for keys and indexes.
- [ ] **Database Setup**
  - [ ] Run migrations and update the SQLite database.

---

## API Endpoint Implementation
- [ ] **Controller Setup**
  - [ ] Create a new controller named `MessageController`.
- [ ] **Endpoint Development**
  - [ ] Implement a POST endpoint at `/api/message/check-sendability`.
- [ ] **Data Transfer Objects (DTOs)**
  - [ ] Create a request DTO with properties:
    - `accountNumber`
    - `phoneNumber`
  - [ ] Create a response DTO with properties:
    - `canSend` (Boolean)
    - `exceededLimit` (String; values like "per-account" or "per-phone-number")
- [ ] **Controller Logic**
  - [ ] Parse the incoming JSON payload.
  - [ ] Forward the data to the RateLimitService (to be implemented next).
  - [ ] Return appropriate HTTP status codes and JSON responses.

---

## Rate Limit Check Logic Implementation
- [ ] **Service Setup**
  - [ ] Create a new service class named `RateLimitService` in a `Services` folder.
- [ ] **Rate Limit Retrieval**
  - [ ] Implement logic to:
    - Check for a per-phone-number limit.
    - If not found, check for an account-wide limit.
    - Fallback to global defaults if necessary.
- [ ] **Message Log Query**
  - [ ] Query the `MessageLog` table to count the number of messages in the last second.
- [ ] **Decision Logic**
  - [ ] Determine if a new message can be sent without exceeding the limit.
  - [ ] Record the message attempt in the `MessageLog` if allowed.
  - [ ] Return the result including whether sending is allowed and which limit (if any) was exceeded.
- [ ] **Dependency Injection**
  - [ ] Register the `RateLimitService` in the DI container.

---

## Background Cleanup Task Implementation
- [ ] **Service Creation**
  - [ ] Create a `CleanupService` class that implements `IHostedService`.
- [ ] **Timer Setup**
  - [ ] Configure a timer to execute the cleanup task every hour.
- [ ] **Cleanup Logic**
  - [ ] Implement the cleanup query:
    ```sql
    DELETE FROM MessageLog WHERE timestamp < datetime('now', '-1 day');
    ```
- [ ] **Service Registration**
  - [ ] Register the `CleanupService` in the DI container.

---

## Error Handling and Logging Implementation
- [ ] **Error Handling in Controllers**
  - [ ] Wrap controller actions in try-catch blocks.
  - [ ] Ensure that errors return a 500 Internal Server Error with descriptive messages.
- [ ] **Error Handling in Services**
  - [ ] Add try-catch blocks in the `RateLimitService` methods.
- [ ] **Logging Configuration**
  - [ ] Utilize `Microsoft.Extensions.Logging` for error logging.
  - [ ] Optionally, implement middleware for centralized exception handling.

---

## Unit and Integration Testing
- [ ] **Test Project Setup**
  - [ ] Create a separate test project (using xUnit, NUnit, etc.).
- [ ] **Unit Tests**
  - [ ] Write tests for `RateLimitService` covering:
    - Per-phone-number limit enforcement.
    - Per-account limit enforcement.
    - Boundary conditions (at limit, just below, and just above).
- [ ] **Integration Tests**
  - [ ] Write tests simulating API calls with various payloads.
  - [ ] Validate that the JSON responses correctly reflect the rate limit checks.
  - [ ] Use an in-memory SQLite database or a dedicated test SQLite instance.
- [ ] **MessageLog Verification**
  - [ ] Ensure that the `MessageLog` table is updated correctly during tests.
- [ ] **Cleanup Task Testing**
  - [ ] Simulate and verify the cleanup process.

---

## Final Wiring and Integration
- [ ] **Startup/Program.cs Configuration**
  - [ ] Register the DbContext with SQLite connection settings.
  - [ ] Register the `RateLimitService` and `CleanupService`.
  - [ ] Set up routing for controllers.
- [ ] **End-to-End Testing**
  - [ ] Verify that the entire flow (API call → rate check → logging → cleanup) works as expected.
- [ ] **Documentation and Run Instructions**
  - [ ] Document how to run the application.
  - [ ] Provide instructions for executing tests.
- [ ] **Code Review**
  - [ ] Review all integrated components for consistency and best practices.
  - [ ] Confirm there are no orphaned or unused code segments.
