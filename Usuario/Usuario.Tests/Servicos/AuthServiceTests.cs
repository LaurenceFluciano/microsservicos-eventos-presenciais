using Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Usuario.DTO;
using Usuario.Models;
using Usuario.Servicos;
using Usuario.Tests.Suporte;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_DeveEmitirBearerToken()
    {
        using var database = new SqliteTestDatabase();
        var user = new UsuarioModel
        {
            Id = Guid.CreateVersion7(),
            Nome = "Ana",
            Email = "ana@example.com"
        };
        var account = new ContaModel
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = user.Id,
            Usuario = user,
            EmailVerificado = true
        };
        account.SenhaHash = new PasswordHasher<ContaModel>().HashPassword(account, "Senha-Segura-123");
        database.Context.Usuarios.Add(user);
        database.Context.Contas.Add(account);
        await database.Context.SaveChangesAsync();
        var tokenService = new FakeTokenJwtService();
        var service = new AuthService(database.Context, new PasswordHasher<ContaModel>(), tokenService);

        var result = await service.LoginAsync(new LoginInputDTO
        {
            Email = " ANA@EXAMPLE.COM ",
            Senha = "Senha-Segura-123"
        });

        Assert.Equal("token-de-teste", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(user.Id, tokenService.UsuarioId);
        Assert.Equal(user.Email, tokenService.Email);
    }

    [Theory]
    [InlineData("ana@example.com", "Senha-Errada-123")]
    [InlineData("naoexiste@example.com", "Senha-Segura-123")]
    public async Task LoginAsync_ComCredenciaisInvalidas_DeveRejeitarGenericamente(
        string email,
        string senha)
    {
        using var database = new SqliteTestDatabase();
        await AdicionarContaAsync(database.Context, email: "ana@example.com", emailVerificado: true);
        var service = new AuthService(
            database.Context,
            new PasswordHasher<ContaModel>(),
            new FakeTokenJwtService());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(
            new LoginInputDTO { Email = email, Senha = senha }));

        Assert.Equal("Credenciais inválidas.", error.Message);
    }

    [Fact]
    public async Task LoginAsync_ComContaNaoVerificada_DeveRejeitarComMensagemGenerica()
    {
        using var database = new SqliteTestDatabase();
        await AdicionarContaAsync(database.Context, email: "ana@example.com", emailVerificado: false);
        var service = new AuthService(
            database.Context,
            new PasswordHasher<ContaModel>(),
            new FakeTokenJwtService());

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(
            new LoginInputDTO { Email = "ana@example.com", Senha = "Senha-Segura-123" }));

        Assert.Equal("Credenciais inválidas.", error.Message);
    }

    private static async Task AdicionarContaAsync(
        DataContext context,
        string email,
        bool emailVerificado)
    {
        var user = new UsuarioModel
        {
            Id = Guid.CreateVersion7(),
            Nome = "Ana",
            Email = email
        };
        var account = new ContaModel
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = user.Id,
            Usuario = user,
            EmailVerificado = emailVerificado
        };
        account.SenhaHash = new PasswordHasher<ContaModel>().HashPassword(account, "Senha-Segura-123");
        context.Usuarios.Add(user);
        context.Contas.Add(account);
        await context.SaveChangesAsync();
    }

    private sealed class FakeTokenJwtService : ITokenJwtService
    {
        public Guid UsuarioId { get; private set; }
        public string? Email { get; private set; }

        public TokenGeradoDto GerarToken(Guid usuarioId, string email)
        {
            UsuarioId = usuarioId;
            Email = email;
            return new TokenGeradoDto("token-de-teste", DateTimeOffset.UtcNow.AddMinutes(15));
        }

        public TokenValidationParameters CriarParametrosValidacao() => new();
    }
}
