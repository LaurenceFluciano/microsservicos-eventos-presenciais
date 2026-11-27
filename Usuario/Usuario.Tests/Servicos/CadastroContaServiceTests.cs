using Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Usuario.DTO;
using Usuario.Models;
using Usuario.Servicos;
using Usuario.Tests.Suporte;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class CadastroContaServiceTests
{
    [Fact]
    public async Task SolicitarCadastro_DeveGuardarHashesESomenteCriarSolicitacaoPendente()
    {
        using var database = new SqliteTestDatabase();
        var sender = new CapturingEmailSender();
        var service = CriarService(database.Context, sender);

        await service.SolicitarCadastroAsync(NovoCadastro());

        var request = await database.Context.SolicitacoesCadastro.SingleAsync();
        Assert.Equal("ana@example.com", request.Email);
        Assert.NotEqual("Senha-Segura-123", request.SenhaHash);
        Assert.NotEqual(sender.TokenEnviado, request.TokenHash);
        Assert.Null(request.UtilizadoEm);
        Assert.Empty(await database.Context.Usuarios.ToListAsync());
        Assert.Empty(await database.Context.Contas.ToListAsync());
        Assert.False(string.IsNullOrWhiteSpace(sender.TokenEnviado));
    }

    [Fact]
    public async Task VerificarEmail_ComTokenValido_DeveCriarUsuarioEContaVerificada()
    {
        using var database = new SqliteTestDatabase();
        var sender = new CapturingEmailSender();
        var service = CriarService(database.Context, sender);
        await service.SolicitarCadastroAsync(NovoCadastro());

        var result = await service.VerificarEmailAsync(sender.TokenEnviado!);

        var user = await database.Context.Usuarios.SingleAsync();
        var account = await database.Context.Contas.SingleAsync();
        var request = await database.Context.SolicitacoesCadastro.SingleAsync();
        Assert.Equal(user.Id, result.UsuarioId);
        Assert.Equal(account.Id, result.ContaId);
        Assert.Equal(user.Id, account.UsuarioId);
        Assert.True(account.EmailVerificado);
        Assert.NotNull(request.UtilizadoEm);
    }

    [Fact]
    public async Task VerificarEmail_ComUsuarioExistenteSemConta_DevePreservarUsuario()
    {
        using var database = new SqliteTestDatabase();
        var existingUser = new UsuarioModel
        {
            Id = Guid.CreateVersion7(),
            Nome = "Nome original",
            Email = "ana@example.com",
            Telefone = "111-2222"
        };
        database.Context.Usuarios.Add(existingUser);
        await database.Context.SaveChangesAsync();
        var sender = new CapturingEmailSender();
        var service = CriarService(database.Context, sender);
        await service.SolicitarCadastroAsync(NovoCadastro());

        var result = await service.VerificarEmailAsync(sender.TokenEnviado!);

        var savedUser = await database.Context.Usuarios.SingleAsync();
        Assert.Equal(existingUser.Id, result.UsuarioId);
        Assert.Equal("Nome original", savedUser.Nome);
        Assert.Equal("ana@example.com", savedUser.Email);
        Assert.Equal("111-2222", savedUser.Telefone);
        Assert.Single(await database.Context.Contas.ToListAsync());
    }

    [Fact]
    public async Task VerificarEmail_ComTokenInvalidoOuJaUtilizado_DeveRejeitar()
    {
        using var database = new SqliteTestDatabase();
        var sender = new CapturingEmailSender();
        var service = CriarService(database.Context, sender);

        await Assert.ThrowsAsync<InvalidVerificationTokenException>(
            () => service.VerificarEmailAsync("token-invalido"));

        await service.SolicitarCadastroAsync(NovoCadastro());
        await service.VerificarEmailAsync(sender.TokenEnviado!);

        await Assert.ThrowsAsync<InvalidVerificationTokenException>(
            () => service.VerificarEmailAsync(sender.TokenEnviado!));
    }

    [Fact]
    public async Task VerificarEmail_ComSolicitacaoExpirada_DeveRejeitarESemCriarUsuario()
    {
        using var database = new SqliteTestDatabase();
        var sender = new CapturingEmailSender();
        var service = CriarService(database.Context, sender);
        await service.SolicitarCadastroAsync(NovoCadastro());
        var request = await database.Context.SolicitacoesCadastro.SingleAsync();
        request.ExpiraEm = DateTime.UtcNow.AddMinutes(-1);
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidVerificationTokenException>(
            () => service.VerificarEmailAsync(sender.TokenEnviado!));

        Assert.Empty(await database.Context.Usuarios.ToListAsync());
        Assert.Empty(await database.Context.Contas.ToListAsync());
    }

    [Fact]
    public async Task SolicitarCadastro_ComEmailQueJaTemConta_DeveRejeitarDuplicata()
    {
        using var database = new SqliteTestDatabase();
        var user = new UsuarioModel
        {
            Id = Guid.CreateVersion7(),
            Nome = "Ana",
            Email = "ana@example.com"
        };
        database.Context.Usuarios.Add(user);
        database.Context.Contas.Add(new ContaModel
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = user.Id,
            Usuario = user,
            SenhaHash = "hash",
            EmailVerificado = true
        });
        await database.Context.SaveChangesAsync();
        var service = CriarService(database.Context, new CapturingEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SolicitarCadastroAsync(NovoCadastro()));

        Assert.Single(await database.Context.Contas.ToListAsync());
    }

    private static CadastroContaService CriarService(DataContext context, IEmailSender sender)
    {
        return new CadastroContaService(
            context,
            new PasswordHasher<ContaModel>(),
            new TokenVerificacaoEmailService(),
            sender);
    }

    private static CriarContaInputDTO NovoCadastro() => new()
    {
        Nome = "Ana Silva",
        Email = " ANA@EXAMPLE.COM ",
        Telefone = " 111-2222 ",
        Senha = "Senha-Segura-123"
    };

    private sealed class CapturingEmailSender : IEmailSender
    {
        public bool IsConfigured => true;
        public string? TokenEnviado { get; private set; }

        public Task SendVerificationAsync(
            string email,
            string token,
            CancellationToken cancellationToken = default)
        {
            TokenEnviado = token;
            return Task.CompletedTask;
        }
    }
}
