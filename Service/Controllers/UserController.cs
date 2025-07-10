using Microsoft.AspNetCore.Mvc;
using Service.Helpers;
using Service.Interfaces;
using Service.Models;

namespace Service.Controllers;

[Obsolete]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserRepository _userRepository;

    public UserController(ILogger<UserController> logger, IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        user.Id = Guid.NewGuid().ToString();
        user.Password = PasswordHasher.HashPassword(user.Password);
        user.IsVerified = false;

        await _userRepository.AddUserAsync(user);
        
        return Ok($"User {user.Id} registered successfully.");
    }

    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> Verify([FromBody] VerificationRequest request)
    {
        var user = await _userRepository.FindUserAsync(request.UserId);

        if (user is null)
            return NotFound("User not found.");

        // TODO: какая-то логика от Айка
        await _userRepository.UpdateUserVerifyingAsync(request.UserId);
        
        return Ok($"User {user.Id} documents verified successfully.");
    }
}