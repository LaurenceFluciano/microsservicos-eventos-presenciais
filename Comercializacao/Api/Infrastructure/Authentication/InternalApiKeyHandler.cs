using System.Net.Http;

namespace Api.Infrastructure.Authentication;

public sealed class InternalApiKeyHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public InternalApiKeyHandler(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Authentication:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Authentication:ApiKey não configurada.");
        }

        request.Headers.Remove(
            ApiKeyAuthenticationHandler.HeaderName);

        request.Headers.TryAddWithoutValidation(
            ApiKeyAuthenticationHandler.HeaderName,
            apiKey);

        return base.SendAsync(
            request,
            cancellationToken);
    }
}