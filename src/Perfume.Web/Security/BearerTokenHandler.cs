using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Perfume.Web.Security;

public sealed class BearerTokenHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly JwtTokenService _tokens;

    public BearerTokenHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, JwtTokenService tokens) : base(options, logger, encoder) => _tokens = tokens;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization)) return Task.FromResult(AuthenticateResult.NoResult());
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = _tokens.ValidateAccessToken(authorization[7..].Trim());
        return Task.FromResult(principal == null
            ? AuthenticateResult.Fail("The access token is invalid or expired.")
            : AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
