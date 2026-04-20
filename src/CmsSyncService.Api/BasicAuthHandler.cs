using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;


public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly List<UserRecord> _users;

    private class UserRecord
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }

    public BasicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider timeProvider,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _users = new List<UserRecord>();
        var usersSection = configuration.GetSection("Users");
        if (usersSection.Exists())
        {
            foreach (var userSection in usersSection.GetChildren())
            {
                var user = new UserRecord
                {
                    Username = userSection["Username"] ?? string.Empty,
                    PasswordHash = userSection["PasswordHash"] ?? string.Empty,
                    Roles = userSection.GetSection("Roles").Get<List<string>>() ?? new List<string>()
                };
                if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(user.PasswordHash))
                {
                    _users.Add(user);
                }
            }
        }
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }
        
        try
        {
            var authHeaderValue = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeaderValue))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }
            
            var authHeader = AuthenticationHeaderValue.Parse(authHeaderValue);
            if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));
            }
            
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? "");
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }
            
            var username = credentials[0];
            var password = credentials[1];
            
            // Hash the provided password for comparison
            var providedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            
            var user = _users.FirstOrDefault(u => u.Username == username && u.PasswordHash == providedHash);
            if (user == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
            }
            
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
    }
}
