using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Api.Attributes;

public sealed class UserJwtAttribute : AuthorizeAttribute
{
    public UserJwtAttribute()
    {
        AuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme;
    }
}