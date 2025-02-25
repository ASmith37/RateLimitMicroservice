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


    [HttpGet("messages/by-account")]
    public async Task<IActionResult> GetMessagesByAccount(string? account = null)
    {
        var stats = await _rateLimitService.GetMessagesByAccount(account);
        return Ok(stats);
    }

    [HttpGet("messages/by-phone")]
    public async Task<IActionResult> GetMessagesByPhoneNumber(string? phoneNumber = null)
    {
        var stats = await _rateLimitService.GetMessagesByPhoneNumber(phoneNumber);
        return Ok(stats);
    }
} 