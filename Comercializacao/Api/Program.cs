
using System.Security.Cryptography;
using System.Text.Json.Serialization;

using Api;
using Api.Error;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure;
using Api.Infrastructure.Authentication;
using Api.Infrastructure.Client;
using Api.Infrastructure.Payments;
using Api.Infrastructure.Messaging;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


using RabbitMQ.Client;
using Scalar.AspNetCore;
using Stripe;

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

#region QR CODE

builder.Services.AddScoped<IQRCodeService, QRCodeService>();

#endregion

#region MEDIATR

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly
    )
);

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

#region SERVIÇOS DA APLICAÇÃO

builder.Services.AddScoped<OfertaIngressoService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<ProcessarPedido>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<IngressoService>();
builder.Services.AddScoped<ConfirmarPedido>();
builder.Services.AddScoped<IngressoDigitalService>();

#endregion

#region MAPPERS

builder.Services.AddScoped<OfertaIngressoMapper>();
builder.Services.AddScoped<PedidoMapper>();

#endregion

#region CLIENTES HTTP

builder.Services.AddTransient<InternalApiKeyHandler>();

// Eventos
builder.Services
    .AddHttpClient<IEventoClient, HttpEventoClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration[
                    "Services:Eventos:BaseUrl"
                ] ?? throw new InvalidOperationException(
                    "URL de Eventos não configurada.")
            );

            client.Timeout = TimeSpan.FromSeconds(5);
        })
    .AddHttpMessageHandler<InternalApiKeyHandler>();

// Portaria
builder.Services
    .AddHttpClient<IPortariaClient, HttpPortariaClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration[
                    "Services:Portaria:BaseUrl"
                ] ?? throw new InvalidOperationException(
                    "URL de Portaria não configurada.")
            );

            client.Timeout = TimeSpan.FromSeconds(10);
        })
    .AddHttpMessageHandler<InternalApiKeyHandler>();


// Usuários
builder.Services
    .AddHttpClient<IUsuarioClient, HttpUsuarioClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration[
                    "Services:Usuarios:BaseUrl"
                ] ?? throw new InvalidOperationException(
                    "URL de Usuários não configurada."
                )
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

    var environment =
        sp.GetRequiredService<IHostEnvironment>();

    if (environment.IsDevelopment())
    {
        return new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"]
                ?? "localhost",

            Port = configuration.GetValue<int>(
                "RabbitMQ:Port", 5672
            ),

            UserName = configuration["RabbitMQ:UserName"]
                ?? throw new InvalidOperationException(
                    "Usuário RabbitMQ não configurado."
                ),

            Password = configuration["RabbitMQ:Password"]
                ?? throw new InvalidOperationException(
                    "Senha RabbitMQ não configurada."
                ),

            AutomaticRecoveryEnabled = true
        };
    }

    var uri = configuration["RabbitMQ:Uri"];

    if (string.IsNullOrWhiteSpace(uri))
    {
        throw new InvalidOperationException(
            "URI do RabbitMQ não configurada."
        );
    }

    return new ConnectionFactory
    {
        Uri = new Uri(uri),

        AutomaticRecoveryEnabled = true,

        NetworkRecoveryInterval =
            TimeSpan.FromSeconds(5)
    };
});

builder.Services.AddHostedService<EventoIniciadoConsumer>();

#endregion

#region STRIPE

builder.Services.AddSingleton<StripeClient>(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var secretKey = configuration["Stripe:SecretKey"];

    if (string.IsNullOrWhiteSpace(secretKey))
    {
        throw new InvalidOperationException(
            "Stripe:SecretKey não configurada.");
    }

    return new StripeClient(secretKey);
});

builder.Services.AddScoped<
    IGatewayPagamento,
    StripeGatewayPagamento>();

#endregion

#region AUTENTICAÇÃO

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IUsuarioAtual,
    UsuarioAtualHttp>();

// JWT é o esquema padrão para [Authorize].
// Endpoints internos selecionam ApiKey explicitamente.

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

#region AUTORIZAÇÃO

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

#region CONTROLLERS E JSON

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

#endregion

#region TRATAMENTO GLOBAL DE EXCEÇÕES

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

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

#region BUILD

var app = builder.Build();

// Libera o objeto RSA quando a aplicação parar.
app.Lifetime.ApplicationStopped.Register(
    publicRsa.Dispose);

#endregion

#region MIGRATIONS

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    db.Database.Migrate();
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

// Configure proxies confiáveis no ambiente
// quando utilizar um proxy reverso.
app.UseForwardedHeaders(forwardedHeadersOptions);

#endregion

#region EXCEÇÕES

app.UseExceptionHandler();

#endregion

#region DOCUMENTAÇÃO


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

app.Run();

#endregion
