using Microsoft.AspNetCore.Authorization;
using Api.Infrastructure.Authentication;

namespace Api.Attributes;

public sealed class InternalApiAttribute : AuthorizeAttribute
{
    public InternalApiAttribute()
    {
        AuthenticationSchemes =
            ApiKeyAuthenticationHandler.SchemeName;
    }
}