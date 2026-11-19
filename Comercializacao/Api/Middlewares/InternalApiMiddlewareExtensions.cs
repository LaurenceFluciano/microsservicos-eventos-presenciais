using Api.Attributes;

namespace Api.Middlewares;

public static class InternalApiMiddlewareExtensions
{
    public static IApplicationBuilder UseInternalApi(
        this IApplicationBuilder app)
    {
        return app.UseWhen(
            context => context
                .GetEndpoint()?
                .Metadata
                .GetMetadata<InternalApiAttribute>() 
                    is not null,

            branch => branch
                .UseMiddleware<InternalApiMiddleware>()
        );
    }
}