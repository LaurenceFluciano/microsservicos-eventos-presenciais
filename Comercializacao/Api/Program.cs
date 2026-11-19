using Api;
using Api.Infrastructure;
using Api.Config;
using Api.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Api.Application.Ports;
using Api.Infrastructure.Client;
using Microsoft.AspNetCore.Authentication;
using Api.Infrastructure.Authentication;
using Api.Application.Services;
using System.Text.Json.Serialization;
using Api.Application.Mappers;
using Api.Error;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();
builder.Services.AddScoped<OfertaIngressoService>();
builder.Services.AddScoped<OfertaIngressoMapper>();

// Add services to the container.

builder.Services.AddOptions<InternalApiConfig>()
    .Bind(
        builder.Configuration.GetSection(
            InternalApiConfig.SectionName
        )
    )
    .Validate(
        config => !string.IsNullOrWhiteSpace(config.ApiKey),
        "InternalApi:ApiKey é obrigatório para a inicialização."
    )
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEventoClient, FakeEventoClient>();

    builder.Services
        .AddAuthentication("Fake")
        .AddScheme<
            AuthenticationSchemeOptions,
            FakeAuthenticationHandler
        >("Fake", options => { });
    
}
else
{

    builder.Services.AddHttpClient<IEventoClient, HttpEventoClient>(
        client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["Services:Eventos:BaseUrl"]!
            );

            client.Timeout = TimeSpan.FromSeconds(5);
        }
    );
}


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });
    
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

app.UseExceptionHandler();

app.UseInternalApi();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
