using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TimeProvider timeProvider)
        : base(options, logger, encoder, new SystemClock()) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

        try
        {
            var authHeaderValue = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeaderValue))
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

            var authHeader = AuthenticationHeaderValue.Parse(authHeaderValue);
            if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? "");
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

            var username = credentials[0];
            var password = credentials[1];

            // TODO: Replace with your own validation logic or configuration
            if (username != "admin" || password != "password")
                return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
    }
}