using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace lapo_vms_api.Helpers;

public class LocalAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "LocalAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                configuration["LocalAuth:UserId"] ?? "00000000-0000-0000-0000-000000000001"),
            new Claim(ClaimTypes.Name, configuration["LocalAuth:Name"] ?? "Local Developer"),
            new Claim(ClaimTypes.Email, configuration["LocalAuth:Email"] ?? "local@localhost"),
            new Claim("staffId", configuration["LocalAuth:StaffId"] ?? "LOCAL"),
            new Claim(ClaimTypes.Role, configuration["LocalAuth:Role"] ?? "Admin")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
