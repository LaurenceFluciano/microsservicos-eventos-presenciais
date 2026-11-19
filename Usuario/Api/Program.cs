using AppTemplate;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Usuario.Autenticacao;
using Usuario.Models;
using Usuario.Servicos;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:ApiKey"]))
{
    throw new InvalidOperationException(
        "Configure Authentication:ApiKey antes de iniciar o serviço.");
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordHasher<ContaModel>, PasswordHasher<ContaModel>>();
builder.Services.AddScoped<TokenVerificacaoEmailService>();
builder.Services.AddScoped<CadastroContaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioServ>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

var tokenJwtService = new TokenJwtService(builder.Configuration);
builder.Services.AddSingleton<ITokenJwtService>(tokenJwtService);



if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, FakeEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, EmailSenderNotConfigured>();
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            ApiKeyAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme =
            ApiKeyAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        _ => { })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokenJwtService.CriarParametrosValidacao();
    });

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
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
};

forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
