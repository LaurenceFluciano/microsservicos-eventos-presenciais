using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Application.Ports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.Authentication;

public sealed class FakeAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{

    private static readonly Guid UsuarioDevId =
        Guid.Parse("019a6f34-8c12-7000-8000-000000000001");

    public FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                UsuarioDevId.ToString()
            ),
            new Claim(ClaimTypes.Name, "Usuario Dev"),
            new Claim(ClaimTypes.Email, "dev@example.com"),
            new Claim(ClaimTypes.Role, "ADMIN")
        };

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: Scheme.Name
        );

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name
        );

        return Task.FromResult(
            AuthenticateResult.Success(ticket)
        );
    }
}