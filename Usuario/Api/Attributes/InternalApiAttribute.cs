using Microsoft.AspNetCore.Authorization;
using Api.Autenticacao;

namespace Api.Attributes;

public sealed class InternalApiAttribute : AuthorizeAttribute
{
    public InternalApiAttribute()
    {
        AuthenticationSchemes =
            ApiKeyAuthenticationHandler.SchemeName;
    }
}