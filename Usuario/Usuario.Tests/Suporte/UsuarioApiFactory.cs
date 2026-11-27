using System.Security.Cryptography;
using Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Usuario.Models;
using Usuario.Servicos;

namespace Usuario.Tests.Suporte;

public sealed class UsuarioApiFactory : IDisposable
{
    public const string ApiKey = "test-api-key-not-for-production";
    private const string Password = "Senha-Segura-123";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly string _connectionString =
        $"Data Source=UsuarioTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False";
    private readonly SqliteConnection _keepAliveConnection;
    private readonly WebApplicationFactory<Program> _factory;

    public CapturingEmailSender EmailSender { get; } = new();

    public IServiceProvider Services => _factory.Services;

    public UsuarioApiFactory()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        var settings = new Dictionary<string, string?>
        {
            ["Authentication:ApiKey"] = ApiKey,
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["Jwt:Issuer"] = "Usuario.Tests",
            ["Jwt:Audience"] = "Usuario.Tests.Client",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:PrivateKeyPem"] = _rsa.ExportPkcs8PrivateKeyPem(),
            ["Jwt:PublicKeyPem"] = _rsa.ExportSubjectPublicKeyInfoPem()
        };

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(EmailSender);
            });
        });
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public async Task<Guid> CriarUsuarioComContaAsync(bool emailVerificado = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var user = new UsuarioModel
        {
            Id = Guid.CreateVersion7(),
            Nome = "Ana Silva",
            Email = "ana@example.com",
            Telefone = "111-2222"
        };
        var account = new ContaModel
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = user.Id,
            Usuario = user,
            EmailVerificado = emailVerificado
        };
        account.SenhaHash = new PasswordHasher<ContaModel>().HashPassword(account, Password);
        context.Usuarios.Add(user);
        context.Contas.Add(account);
        await context.SaveChangesAsync();
        return user.Id;
    }

    public static object Credenciais(string password = Password) => new
    {
        email = "ana@example.com",
        senha = password
    };

    public void Dispose()
    {
        _factory.Dispose();
        _keepAliveConnection.Dispose();
        _rsa.Dispose();
    }

    public sealed class CapturingEmailSender : IEmailSender
    {
        public bool IsConfigured => true;
        public string? TokenEnviado { get; private set; }

        public Task SendVerificationAsync(
            string email,
            string token,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TokenEnviado = token;
            return Task.CompletedTask;
        }
    }
}
