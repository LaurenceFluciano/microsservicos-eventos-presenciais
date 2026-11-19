using Api.Config;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Api.Middlewares;

public class InternalApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InternalApiConfig _config;

    public InternalApiMiddleware(
        RequestDelegate next,
        IOptions<InternalApiConfig> config)
    {
        _next = next;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(
                InternalApiConfig.HeaderName,
                out var apiKey)
            || apiKey.Count != 1
            || !ValidarApiKey(apiKey.ToString()))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return;
        }

        await _next(context);
    }

    private bool ValidarApiKey(string apiKey)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(apiKey),
            Encoding.UTF8.GetBytes(_config.ApiKey)
        );
    }
}