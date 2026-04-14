using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.ToErrorResponse());

        var exists = await _db.Users.AnyAsync(u => u.Email == req.Email.ToLowerInvariant());
        if (exists)
            return Conflict(new ValidationErrorResponse("email already registered",
                new Dictionary<string, string> { ["email"] = "already in use" }));

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = req.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered: {UserId} {Email}", user.Id, user.Email);

        var token = _tokenService.GenerateToken(user);
        return StatusCode(201, new AuthResponse(token, new UserDto(user.Id, user.Name, user.Email)));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.ToErrorResponse());

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponse("invalid email or password"));

        _logger.LogInformation("User logged in: {UserId}", user.Id);

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, new UserDto(user.Id, user.Name, user.Email)));
    }
}
