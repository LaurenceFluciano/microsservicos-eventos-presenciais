
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using RabbitMQ.Client;


using Api;
using Api.Workers;
using Api.Error;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure.Authentication;
using Api.Infrastructure.Integration;
using Api.Infrastructure.Messaging;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

#region CONFIGURAÇÕES OBRIGATÓRIAS

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

#region CHAVE PÚBLICA RSA

var publicRsa = RSA.Create();

try
{
    publicRsa.ImportFromPem(publicKeyPem);

    if (publicRsa.KeySize < 2048)
    {
        throw new InvalidOperationException(
            "A chave RSA deve possuir pelo menos 2048 bits.");
    }
}
catch
{
    publicRsa.Dispose();
    throw;
}

var rsaSecurityKey = new RsaSecurityKey(publicRsa);

#endregion

#region SERVIÇOS DA APLICAÇÃO

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IUsuarioAtual,
    UsuarioAtualHttp>();

builder.Services.AddScoped<EventoService>();

#endregion

#region INTEGRAÇÕES EXTERNAS

builder.Services.AddTransient<InternalApiKeyHandler>();

// Comercialização
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
                    "Configure Services:Comercializacao:BaseUrl.")
            );

            client.Timeout = TimeSpan.FromSeconds(5);
        })
    .AddHttpMessageHandler<InternalApiKeyHandler>();

#endregion

#region RABBITMQ

builder.Services.AddSingleton(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    return new ConnectionFactory
    {
        HostName = configuration["RabbitMQ:HostName"]
            ?? "localhost",

        Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),

        UserName = configuration["RabbitMQ:UserName"]
            ?? throw new InvalidOperationException(
                "Usuário RabbitMQ não configurado."
            ),

        Password = configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException(
                "Senha RabbitMQ não configurada."
            )
    };
});

builder.Services.AddScoped<
    IEventoPublisher,
    RabbitMqEventoPublisher>();

builder.Services.AddHostedService<EventoLifecycleWorker>();

#endregion

#region CONTROLLERS E JSON

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

#endregion

#region AUTENTICAÇÃO


builder.Services
    .AddAuthentication()
    .AddScheme<
        AuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        _ => { })
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
        });

#endregion

#region AUTORIZAÇÃO

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ServiceApiKey", policy =>
    {
        policy.AddAuthenticationSchemes(
            ApiKeyAuthenticationHandler.SchemeName
        );

        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("UserJwt", policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme
        );

        policy.RequireAuthenticatedUser();
    });
});

#endregion

#region OPENAPI E SCALAR

builder.Services.AddOpenApi();

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

#region BANCO DE DADOS

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

#endregion

#region TRATAMENTO GLOBAL DE EXCEÇÕES

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

#endregion

#region CONSTRUÇÃO DA APLICAÇÃO

var app = builder.Build();

// Mantém a chave RSA disponível durante a execução.
app.Lifetime.ApplicationStopped.Register(
    publicRsa.Dispose
);

#endregion

#region MIGRATIONS

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    dataContext.Database.Migrate();
}

#endregion

#region FORWARDED HEADERS

var forwardedHeadersOptions =
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto
    };

// Configure explicitamente os proxies confiáveis
// quando utilizar um proxy reverso.

app.UseForwardedHeaders(forwardedHeadersOptions);

#endregion

#region TRATAMENTO DE EXCEÇÕES

app.UseExceptionHandler();

#endregion

#region DOCUMENTAÇÃO DA API

app.MapOpenApi();
app.MapScalarApiReference();


#endregion

#region PIPELINE HTTP

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();
