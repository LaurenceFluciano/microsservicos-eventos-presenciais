using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Usuario.DTO;
using Usuario.Models;
using Usuario.Servicos;
using Usuario.Tests.Suporte;
using Xunit;

namespace Usuario.Tests.Servicos;

public sealed class UsuarioServTests
{
    [Fact]
    public async Task Criar_DevePersistirUsuarioSemCriarContaENormalizarEmail()
    {
        using var database = new SqliteTestDatabase();
        var service = new UsuarioServ(database.Context);

        var result = await service.Criar(new UsuarioInputDTO
        {
            Nome = "Ana Silva",
            Email = "  ANA@EXAMPLE.COM  "
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ana@example.com", result.Email);
        Assert.Equal(1, await database.Context.Usuarios.CountAsync());
        Assert.Empty(await database.Context.Contas.ToListAsync());
    }

    [Fact]
    public async Task Criar_ComEmailDuplicado_DeveRejeitarCadastro()
    {
        using var database = new SqliteTestDatabase();
        database.Context.Usuarios.Add(CriarUsuario("ana@example.com"));
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Criar(
            new UsuarioInputDTO
            {
                Nome = "Outra Ana",
                Email = " ANA@EXAMPLE.COM "
            }));

        Assert.Contains("e-mail", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await database.Context.Usuarios.ToListAsync());
    }

    [Fact]
    public async Task ObterPorId_DeveRetornarUsuarioMesmoSemConta()
    {
        using var database = new SqliteTestDatabase();
        var user = CriarUsuario("ana@example.com");
        database.Context.Usuarios.Add(user);
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        var result = await service.ObterPorId(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Null(result.Telefone);
        Assert.Empty(await database.Context.Contas.ToListAsync());
    }

    [Fact]
    public async Task AtualizarMeAsync_ComNomeApenas_DevePreservarTelefone()
    {
        using var database = new SqliteTestDatabase();
        var user = CriarUsuario("ana@example.com", "111-2222");
        database.Context.Usuarios.Add(user);
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        var result = await service.AtualizarMeAsync(user.Id, new AtualizarMeInputDTO
        {
            Nome = "  Ana Nova  "
        });

        Assert.NotNull(result);
        Assert.Equal("Ana Nova", result.Nome);
        Assert.Equal("111-2222", result.Telefone);
    }

    [Fact]
    public async Task AtualizarMeAsync_ComTelefoneNulo_DeveRemoverTelefoneEPreservarNome()
    {
        using var database = new SqliteTestDatabase();
        var user = CriarUsuario("ana@example.com", "111-2222");
        database.Context.Usuarios.Add(user);
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        var result = await service.AtualizarMeAsync(user.Id, new AtualizarMeInputDTO
        {
            Telefone = JsonValue("null")
        });

        Assert.NotNull(result);
        Assert.Equal("Ana", result.Nome);
        Assert.Null(result.Telefone);
    }

    [Fact]
    public async Task AtualizarMeAsync_ComNomeInvalido_NaoDevePersistirAlteracaoParcial()
    {
        using var database = new SqliteTestDatabase();
        var user = CriarUsuario("ana@example.com", "111-2222");
        database.Context.Usuarios.Add(user);
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        await Assert.ThrowsAsync<ValidationException>(() => service.AtualizarMeAsync(
            user.Id,
            new AtualizarMeInputDTO
            {
                Nome = "   ",
                Telefone = JsonValue("\"999-9999\"")
            }));

        var savedUser = await database.Context.Usuarios.SingleAsync();
        Assert.Equal("Ana", savedUser.Nome);
        Assert.Equal("111-2222", savedUser.Telefone);
    }

    [Fact]
    public async Task AtualizarMeAsync_ComTelefoneDeTipoInvalido_DeveRejeitar()
    {
        using var database = new SqliteTestDatabase();
        var user = CriarUsuario("ana@example.com", "111-2222");
        database.Context.Usuarios.Add(user);
        await database.Context.SaveChangesAsync();
        var service = new UsuarioServ(database.Context);

        await Assert.ThrowsAsync<ValidationException>(() => service.AtualizarMeAsync(
            user.Id,
            new AtualizarMeInputDTO { Telefone = JsonValue("123") }));

        Assert.Equal("111-2222", (await database.Context.Usuarios.SingleAsync()).Telefone);
    }

    private static UsuarioModel CriarUsuario(string email, string? telefone = null) => new()
    {
        Id = Guid.CreateVersion7(),
        Nome = "Ana",
        Email = email,
        Telefone = telefone
    };

    private static JsonElement JsonValue(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
