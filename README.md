# Microservice Rate Limiter

This is a microservice in C#/.NET Core that acts as a rate limiter. It enforces limits per-account and per-phone number.

It automatically clears data for accounts and phone numbers that have not been used for a period of 1 hour.

### Software versions:
- .NET 8
- Angular 15
- Node 18.

### To run:
- Run `dotnet run` in the MessageRateLimiter directory to start the .NET service.
- Run `npm start` in the message-monitor directory to start the Angular application (or `ng serve`).
- (Optionally) Run 1+ copies of LoadTestScript.ps1 to create traffic.
- `dotnet test` to run the tests.

### Screenshot
![image](assets/Screenshot1.png)

### Example requests (e.g. for use with Postman):
- `localhost:5000/api/message/check-sendability` `{"accountNumber": "123456", "phoneNumber": "1234567890" }`
- `http://localhost:5000/api/message/messages/by-account`
- `http://localhost:5000/api/message/messages/by-account?account=123456`
- `http://localhost:5000/api/message/messages/by-phone`

### Notes:
- Made heavy use of Cursor.
- If anything is wonky it's probably because I accidentally deleted the entire codebase and restored it from dangling git blobs. I don't do that often.