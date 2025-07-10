using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Service.Models;
using Service.Interfaces;
using Service.Helpers;

namespace Service.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, string> Codes = new(); // code -> userId
    private static readonly ConcurrentDictionary<string, string> Tokens = new(); // access_token -> userId
    private static Dictionary<string, string> _clients = new(); // client_id -> client_secret
    private readonly OAuthOptions _oauthOptions;
    private readonly IUserRepository _userRepository;

    public OAuthController(IUserRepository userRepository, IOptions<OAuthOptions> oauthOptions)
    {
        _userRepository = userRepository;
        _oauthOptions = oauthOptions.Value;
        _clients = _oauthOptions.Clients.ToDictionary(c => c.ClientId, c => c.ClientSecret);
    }

    [HttpGet]
    [Route("authorize")]
    public IActionResult Authorize(
        [FromQuery] string clientId,
        // [FromQuery] string redirectUri,
        [FromQuery] string userId = "")
    {
        if (_oauthOptions.Clients.All(c => c.ClientId != clientId))
            return NotFound($"Client {clientId} not found.");
        
        if (string.IsNullOrEmpty(userId))
        {
            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "html", "form.html");
            var html = System.IO.File.ReadAllText(htmlPath);
            html = html.Replace("{{client_id}}", clientId);
            html = html.Replace("{{redirect_uri}}", _oauthOptions.RedirectUrl);
            html = html.Replace("{{response_type}}", _oauthOptions.ResponseType);
            html = html.Replace("{{state}}", _oauthOptions.State);
            
            return Content(html, "text/html");
        }
            
        var code = GenerateCode();
        var uri = $"{_oauthOptions.RedirectUrl}?code={code}&state={_oauthOptions.State}";
        Codes[code] = userId;
            
        return Redirect(uri);
    }

    [HttpPost]
    [Route("token")]
    public IActionResult Token(
        [FromForm] string code,
        [FromForm] string clientId,
        [FromForm] string clientSecret,
        [FromForm] string redirectUri = "http://localhost:12345/callback",
        [FromForm] string grantType = "authorization_code")
    {
        if (grantType != "authorization_code")
            return BadRequest("grant_type должен быть 'authorization_code'");
        
        if (!_clients.TryGetValue(clientId, out var expectedSecret) || expectedSecret != clientSecret)
            return BadRequest("Неверные client_id или client_secret");

        if (redirectUri != _oauthOptions.RedirectUrl)
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
            
        return Ok(new { userId });
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
        // var auth = Request.Headers.Authorization.ToString();
        //     
        // if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
        //     return Unauthorized();

        var config = new
        {
            client_id = _oauthOptions.Clients.First().ClientId,
            client_secret = _oauthOptions.Clients.First().ClientSecret,
            authorization_endpoint = Url.ActionLink("Authorize", "OAuth", null, Request.Scheme),
            token_endpoint = Url.ActionLink("Token", "OAuth", null, Request.Scheme),
            redirect_url = redirectUrl ?? _oauthOptions.RedirectUrl
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
        var codeBytes = RandomNumberGenerator.GetBytes(32);
        var base64UrlCode = Convert.ToBase64String(codeBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        
        return base64UrlCode;
    }

    private static string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var base64UrlToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        
        return base64UrlToken;
    }
}