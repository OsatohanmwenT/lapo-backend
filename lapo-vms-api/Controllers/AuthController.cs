using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using lapo_vms_api.Data;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using lapo_vms_api.API.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace lapo_vms_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDBContext _db;
    private readonly IConfiguration _config;
    private readonly AdAuthHelper _adAuth;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDBContext db,
        IConfiguration config,
        AdAuthHelper adAuth,
        IAuditService auditService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _config = config;
        _adAuth = adAuth;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var localAuth = _config.GetSection("LocalAuth");
        if (localAuth.GetValue<bool>("Enabled") &&
            request.Email.Equals(localAuth["Email"], StringComparison.OrdinalIgnoreCase))
        {
            return Ok(BuildLocalToken(localAuth));
        }

        var (adValid, message) = await _adAuth.ADLogin(new Login
        {
            Username = request.Email,
            Password = request.Password
        });

        if (!adValid)
        {
            _logger.LogWarning("Authentication failed. Reason={Reason}", message);
            return Unauthorized(new { message = "Invalid Staff ID or password" });
        }


        var user = await _db.User
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            _logger.LogWarning("Authentication failed. Reason={Reason}", "UserNotAuthorized");
            return Unauthorized(new { message = "You are not authorized to access this portal" });
        }


        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Name    ?? ""),
            new Claim(ClaimTypes.Email,          user.Email   ?? ""),
            new Claim("staffId",                 user.StaffId ?? ""),
            new Claim(ClaimTypes.Role,           user.Role?.ToString() ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        await _auditService.LogEventAsync(new AuditLog
        {
            EventType = "SIGN_IN",
            ActorId = user.Id,
            ActorRole = user.Role?.ToString(),
            Timestamp = DateTime.UtcNow,
            Metadata = $"Email: {user.Email}; StaffId: {user.StaffId}"
        });

        _logger.LogInformation(
            "Authentication succeeded. UserId={UserId} StaffId={StaffId} Role={Role}",
            user.Id,
            user.StaffId,
            user.Role);

        return Ok(new
        {
            token = tokenString,
            user = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                staffId = user.StaffId,
                role = user.Role?.ToString()
            }
        });
    }

    private object BuildLocalToken(IConfigurationSection localAuth)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var id = localAuth["UserId"] ?? Guid.NewGuid().ToString();
        var name = localAuth["Name"] ?? "Local Developer";
        var email = localAuth["Email"] ?? "local@localhost";
        var staffId = localAuth["StaffId"] ?? "LOCAL";
        var role = localAuth["Role"] ?? "Admin";

        _logger.LogInformation(
            "Local authentication succeeded. UserId={UserId} StaffId={StaffId} Role={Role}",
            id,
            staffId,
            role);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name,           name),
            new Claim(ClaimTypes.Email,          email),
            new Claim("staffId",                 staffId),
            new Claim(ClaimTypes.Role,           role),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new { id, name, email, staffId, role }
        };
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
