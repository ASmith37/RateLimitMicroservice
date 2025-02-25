using Microsoft.AspNetCore.Mvc;
using MessageRateLimiter.Data;
using MessageRateLimiter.Models;
using MessageRateLimiter.Services;
using Microsoft.EntityFrameworkCore;

namespace MessageRateLimiter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<MessageController> _logger;
    private readonly ApplicationDbContext _context;

    public MessageController(
        IRateLimitService rateLimitService,
        ILogger<MessageController> logger,
        ApplicationDbContext context)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
        _context = context;
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

    [HttpGet("messages/{account}")]
    public async Task<ActionResult<MessageLogResponse>> GetMessages(
        string account,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        if ((startTime == null) != (endTime == null))
        {
            return BadRequest("Both start and end time must be provided together");
        }

        var response = await _rateLimitService.GetMessages(account, startTime, endTime);
        return Ok(response);
    }
} 