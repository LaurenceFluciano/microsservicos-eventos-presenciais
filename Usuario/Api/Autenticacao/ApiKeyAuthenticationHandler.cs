using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Usuario.Autenticacao
{
    public sealed class ApiKeyAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiKey";
        public const string HeaderName = "X-API-Key";

        private readonly IConfiguration _configuration;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("API Key ausente ou inválida."));
            }

            var expectedKey = _configuration["Authentication:ApiKey"];

            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                Logger.LogError("A configuração Authentication:ApiKey não foi definida.");

                return Task.FromResult(
                    AuthenticateResult.Fail("Autenticação do serviço não está configurada."));
            }

            var providedBytes = Encoding.UTF8.GetBytes(values[0]!);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

            var keysMatch =
                providedBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);

            if (!keysMatch)
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("API Key inválida."));
            }

            var identity = new ClaimsIdentity(
                Array.Empty<Claim>(),
                Scheme.Name);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}