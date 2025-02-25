using Microsoft.AspNetCore.Mvc;
using MessageRateLimiter.Models;
using MessageRateLimiter.Services;

namespace MessageRateLimiter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(
        IRateLimitService rateLimitService,
        ILogger<MessageController> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    [HttpPost("check-sendability")]
    public async Task<ActionResult<MessageSendabilityResponse>> CheckSendability(
        [FromBody] MessageSendabilityRequest request)
    {
        var response = await _rateLimitService.CheckMessageSendability(
            request.AccountNumber, 
            request.PhoneNumber);

        return Ok(response);
    }
} 