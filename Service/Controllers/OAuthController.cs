using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Service.Models;
using Service.Interfaces;
using Service.Helpers;

namespace Service.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController : ControllerBase
{
    // In-memory хранилище для прототипа
    private static ConcurrentDictionary<string, string> Codes = new(); // code -> userId
    private static ConcurrentDictionary<string, string> Tokens = new(); // access_token -> userId
    // In-memory список клиентов: client_id -> client_secret
    private static readonly Dictionary<string, string> Clients = new()
    {
        { "demo-client-id", "demo-client-secret" }
    };
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;

    public OAuthController(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    [HttpGet]
    [Route("authorize")]
    public IActionResult Authorize(
        [FromQuery] string clientId,
        // [FromQuery] string redirectUri,
        [FromQuery] string userId = "")
    {
        var redirectUri = _configuration.GetSection("OAuthOptions").GetSection("RedirectUrl").Value;
        var responseType = _configuration.GetSection("OAuthOptions").GetSection("ResponseType").Value;
        var state = _configuration.GetSection("OAuthOptions").GetSection("State").Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "html", "form.htm");
            var html = System.IO.File.ReadAllText(htmlPath);
            html = html.Replace("{{client_id}}", clientId);
            html = html.Replace("{{redirect_uri}}", redirectUri);
            html = html.Replace("{{response_type}}", responseType);
            html = html.Replace("{{state}}", state);
            return Content(html, "text/html");
        }
            
        var code = GenerateCode();
        var uri = $"{redirectUri}?code={code}&state={state}";
        Codes[code] = userId;
            
        return Redirect(uri);
    }

    // 2. /oauth/token (POST, application/x-www-form-urlencoded)
    [HttpPost]
    [Route("token")]
    public IActionResult Token(
        [FromForm] string code,
        [FromForm] string clientId,
        [FromForm] string clientSecret,
        [FromForm] string redirectUri,
        [FromForm] string grantType = "authorization_code")
    {
        if (grantType != "authorization_code")
            return BadRequest("grant_type должен быть 'authorization_code'");
        
        if (!Clients.TryGetValue(clientId, out var expectedSecret) || expectedSecret != clientSecret)
            return BadRequest("Неверные client_id или client_secret");
        
        var allowedRedirectUri = _configuration.GetSection("OAuthOptions").GetSection("RedirectUrl").Value;
        
        if (redirectUri != allowedRedirectUri)
            return BadRequest("Недопустимый redirect_uri");
        
        if (!Codes.TryRemove(code, out var userId))
            return BadRequest("Неверный или использованный code");

        var accessToken = GenerateToken();
        Tokens[accessToken] = userId;
        
        return Ok(new
        {
            access_token = accessToken,
            token_type = "bearer",
            expires_in = 3600
        });
    }

    // 3. /oauth/userinfo (GET, Authorization: Bearer ...)
    [HttpGet]
    [Route("userinfo")]
    public IActionResult UserInfo()
    {
        var auth = Request.Headers.Authorization.ToString();
            
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
            return Unauthorized();
            
        var token = auth.Substring("Bearer ".Length);
            
        if (!Tokens.TryGetValue(token, out var userId))
            return Unauthorized();
            
        return Ok(new { sub = userId });
    }

    // 4. /oauth/register (POST)
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        user.Id = Guid.NewGuid().ToString();
        user.Password = PasswordHasher.HashPassword(user.Password);
        user.IsVerified = false;
            
        await _userRepository.AddUserAsync(user);
            
        return Ok(new { user.Id, user.Name });
    }

    // 5. /oauth/config (GET) — параметры для фронта
    [HttpGet]
    [Route("config")]
    public IActionResult Config([FromQuery] string? redirectUrl = null)
    {
        var config = new
        {
            client_id = "demo-client-id",
            client_secret = "demo-client-secret",
            authorization_endpoint = Url.ActionLink("Authorize", "OAuth", null, Request.Scheme),
            token_endpoint = Url.ActionLink("Token", "OAuth", null, Request.Scheme),
            redirect_url = redirectUrl ?? "http://localhost:12345/callback"
        };
            
        return Ok(config);
    }
    
    // только для теста oauth на бэке
    [HttpGet]
    [Route("callback")]
    public IActionResult Callback([FromQuery] string code, [FromQuery] string state)
    {
        return Content($"Код авторизации: {code}, state: {state}");
    }

    private static string GenerateCode()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }
        
    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}