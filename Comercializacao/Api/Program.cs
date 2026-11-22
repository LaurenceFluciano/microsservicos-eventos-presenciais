
using Api;
using Api.Config;
using Api.Error;
using Api.Middlewares;
using Api.Model;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure;
using Api.Infrastructure.Authentication;
using Api.Infrastructure.Client;
using Api.Infrastructure.Payments;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Stripe;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// MEDIATR
// ======================================

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly
    )
);

// ======================================
// BANCO DE DADOS
// ======================================

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

// ======================================
// SERVIÇOS DA APLICAÇÃO
// ======================================

builder.Services.AddScoped<OfertaIngressoService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<ProcessarPedido>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<IngressoService>();
builder.Services.AddScoped<ConfirmarPedido>();

// ======================================
// MAPPERS
// ======================================

builder.Services.AddScoped<OfertaIngressoMapper>();
builder.Services.AddScoped<PedidoMapper>();

// ======================================
// CLIENTES EXTERNOS
// ======================================

builder.Services.AddHttpClient<
    IEventoClient,
    HttpEventoClient
>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration[
            "Services:Eventos:BaseUrl"
        ] ?? throw new InvalidOperationException(
            "URL do serviço Eventos não configurada."
        )
    );

    client.Timeout = TimeSpan.FromSeconds(5);
});


// API de Portaria
builder.Services.AddHttpClient<
    IPortariaClient,
    HttpPortariaClient
>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration[
            "Services:Portaria:BaseUrl"
        ] ?? throw new InvalidOperationException(
            "URL do serviço Portaria não configurada."
        )
    );

    client.Timeout = TimeSpan.FromSeconds(10);
});

// ======================================
// STRIPE
// ======================================

// Funciona em desenvolvimento com sk_test_
// e em produção com sk_live_.
builder.Services.AddSingleton<StripeClient>(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var secretKey = configuration[
        "Stripe:SecretKey"
    ] ?? throw new InvalidOperationException(
        "Stripe:SecretKey não configurada."
    );

    return new StripeClient(secretKey);
});

builder.Services.AddScoped<
    IGatewayPagamento,
    StripeGatewayPagamento
>();

// ======================================
// AUTENTICAÇÃO E AUTORIZAÇÃO
// ======================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IUsuarioAtual,
    UsuarioAtualHttp
>();

builder.Services.AddOptions<InternalApiConfig>()
    .Bind(
        builder.Configuration.GetSection(
            InternalApiConfig.SectionName
        )
    )
    .Validate(
        config => !string.IsNullOrWhiteSpace(
            config.ApiKey
        ),
        "InternalApi:ApiKey é obrigatório."
    )
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication("Fake")
        .AddScheme<
            AuthenticationSchemeOptions,
            FakeAuthenticationHandler
        >("Fake", _ => { });
}
else
{
    // Registrar aqui o esquema REAL de autenticação
    // da sua API antes de exigir [Authorize].
}

builder.Services.AddAuthorization();

// ======================================
// CONTROLLERS E JSON
// ======================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler
>();

builder.Services.AddProblemDetails();

// ======================================
// CORS
// ======================================

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// ======================================
// BUILD
// ======================================

var app = builder.Build();

// ======================================
// MIGRATIONS
// ======================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    db.Database.Migrate();
}

// ======================================
// HTTP PIPELINE
// ======================================

var forwardedHeadersOptions =
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto
    };

// Configure KnownProxies/KnownIPNetworks
// com os proxies realmente confiáveis.
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

// O middleware InternalApi deve permitir
// o webhook Stripe validado por assinatura.
app.UseInternalApi();

app.MapControllers();

app.Run();
