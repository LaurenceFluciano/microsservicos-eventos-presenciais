
using Api;
using Api.Autenticacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Usuario.Models;
using Usuario.Servicos;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Configuração
// =====================================================

if (string.IsNullOrWhiteSpace(
        builder.Configuration["Authentication:ApiKey"]))
{
    throw new InvalidOperationException(
        "Configure Authentication:ApiKey antes de iniciar o serviço.");
}

// =====================================================
// Controllers e serviços
// =====================================================

builder.Services.AddControllers();

builder.Services.AddScoped<
    IPasswordHasher<ContaModel>,
    PasswordHasher<ContaModel>>();

builder.Services.AddScoped<TokenVerificacaoEmailService>();
builder.Services.AddScoped<CadastroContaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioServ>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

// =====================================================
// JWT
// =====================================================

var tokenJwtService = new TokenJwtService(
    builder.Configuration);

builder.Services.AddSingleton<ITokenJwtService>(
    tokenJwtService);

// =====================================================
// E-mail
// =====================================================

builder.Services.AddSingleton<IEmailSender, FakeEmailSender>();


// =====================================================
// Autenticação
// =====================================================

// Não definimos um esquema padrão.
// Cada endpoint deverá selecionar sua política.

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
                tokenJwtService.CriarParametrosValidacao();
        });

// =====================================================
// Autorização
// =====================================================

builder.Services.AddAuthorization(options =>
{
    // Comunicação interna entre microsserviços.
    options.AddPolicy("ServiceApiKey", policy =>
    {
        policy.AddAuthenticationSchemes(
            ApiKeyAuthenticationHandler.SchemeName);

        policy.RequireAuthenticatedUser();
    });

    // Autenticação de usuários por JWT.
    options.AddPolicy("UserJwt", policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
    });
});

// =====================================================
// OpenAPI
// =====================================================

builder.Services.AddOpenApi();

// =====================================================
// CORS
// =====================================================

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

// =====================================================
// Banco de dados
// =====================================================

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")
    )
);

// =====================================================
// Construção da aplicação
// =====================================================

var app = builder.Build();

// =====================================================
// Migrations
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    dataContext.Database.Migrate();
}

// =====================================================
// Forwarded Headers
// =====================================================

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
};

// Configure KnownProxies ou KnownIPNetworks com os
// proxies confiáveis do ambiente, quando necessário.
// Não remova indiscriminadamente essas restrições.

app.UseForwardedHeaders(forwardedHeadersOptions);

// =====================================================
// OpenAPI / Scalar
// =====================================================

app.MapOpenApi();
app.MapScalarApiReference();

// =====================================================
// Pipeline HTTP
// =====================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

