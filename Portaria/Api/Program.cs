
using System.Security.Cryptography;

using Api;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure.Authentication;
using Api.Application.Mappers;
using Api.Error;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;
using Api.Infrastructure.Client;

var builder = WebApplication.CreateBuilder(args);

#region Configurações obrigatórias

var apiKey = builder.Configuration["Authentication:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "Configure Authentication:ApiKey.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Configure Jwt:Issuer.");
}

var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Configure Jwt:Audience.");
}

var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"];

if (string.IsNullOrWhiteSpace(publicKeyPem))
{
    throw new InvalidOperationException(
        "Configure Jwt:PublicKeyPem.");
}

#endregion

#region Chave pública RSA

var publicRsa = RSA.Create();

try
{
    publicRsa.ImportFromPem(publicKeyPem);

    if (publicRsa.KeySize < 2048)
    {
        throw new InvalidOperationException(
            "A chave RSA precisa ter pelo menos 2048 bits.");
    }
}
catch
{
    publicRsa.Dispose();
    throw;
}

var rsaSecurityKey = new RsaSecurityKey(publicRsa);

#endregion

#region Controllers

builder.Services.AddControllers();
builder.Services.AddOpenApi();

#endregion

#region Serviços da aplicação

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();

builder.Services.AddScoped<PortariaService>();

#endregion


#region Mappers

builder.Services.AddScoped<PortariaMapper>();

#endregion

#region Tratamento global de exceções

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

#endregion

#region Autenticação

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,

                    ValidAlgorithms =
                    [
                        SecurityAlgorithms.RsaSha256
                    ],

                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,

                    ValidateAudience = true,
                    ValidAudience = jwtAudience,

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = "sub"
                };
        })
    .AddScheme<
        AuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        _ => { });

#endregion

#region Autorização

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ServiceApiKey", policy =>
    {
        policy.AddAuthenticationSchemes(
            ApiKeyAuthenticationHandler.SchemeName);

        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("UserJwt", policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
    });
});

#endregion


#region CLIENTES HTTP

builder.Services.AddTransient<InternalApiKeyHandler>();

builder.Services
    .AddHttpClient<
        IComercializacaoClient,
        HttpComercializacaoClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration[
                    "Services:Comercializacao:BaseUrl"
                ] ?? throw new InvalidOperationException(
                    "URL de Comercialização não configurada.")
            );

            client.Timeout = TimeSpan.FromSeconds(5);
        })
    .AddHttpMessageHandler<InternalApiKeyHandler>();

#endregion


#region CORS

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

#endregion

#region Banco de dados

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")
    )
);

#endregion

#region Build

var app = builder.Build();

app.Lifetime.ApplicationStopped.Register(
    publicRsa.Dispose);

#endregion

#region Migrations

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    dataContext.Database.Migrate();
}

#endregion

#region Forwarded Headers

var forwardedHeadersOptions =
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto
    };

app.UseForwardedHeaders(forwardedHeadersOptions);

#endregion

#region Documentação


app.MapOpenApi();
app.MapScalarApiReference();


#endregion

#region Pipeline HTTP

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

#endregion
