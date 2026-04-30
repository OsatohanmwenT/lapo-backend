using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using lapo_vms_api.Data;
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

    public AuthController(ApplicationDBContext db, IConfiguration config, AdAuthHelper adAuth)
    {
        _db = db;
        _config = config;
        _adAuth = adAuth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {

        var (adValid, message) = await _adAuth.ADLogin(new Login
        {
            Username = request.Email,
            Password = request.Password
        });

        if (!adValid)
            return Unauthorized(new { message = "Invalid Staff ID or password" });


        var user = await _db.User
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "You are not authorized to access this portal" });


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
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
