using Microsoft.AspNetCore.Mvc;
using Service.DbContext;
using Service.Models;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UserDbContext _db;

    public UserController(ILogger<UserController> logger, UserDbContext db)
    {
        _db = db;
        _logger = logger;
    }
    
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        user.Id = Guid.NewGuid();
        user.IsVerified = false;

        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        return Ok("User registered successfully.");
    }

    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> Verify([FromBody] VerificationRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);

        if (user is null)
            return NotFound("User not found.");
        
        // TODO: какая-то проверка, жду сервака от Айка
        user.IsVerified = true;
        await _db.SaveChangesAsync();

        return Ok("User documents verified successfully.");
    }
}