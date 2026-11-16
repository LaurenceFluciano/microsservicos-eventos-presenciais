
using Api;
using Api.Application.DTO;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure.Authentication;
using Api.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.UnitTest;

public class EventoServiceTest
{

    public static async Task<(DataContext Context, SqliteConnection Conexao)>
    GetContextoBanco()
    {
        var conexao = new SqliteConnection(
            "Data Source=:memory:"
        );

        await conexao.OpenAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(conexao)
            .Options;

        var context = new DataContext(options);

        await context.Database.EnsureCreatedAsync();

        return (context, conexao);
    }

    [Fact(DisplayName = "EventoService: deve cadastrar evento válido")]
    public async Task Cadastrar_ComDadosValidos_DevePersistirEvento()
    {
        // Arrange
       
        var (context, conexao) = await GetContextoBanco();

        var usuarioFake = new FakeUsuarioAtual();

        var service = new EventoService(
            context,
            usuarioFake
        );

        var inicio = DateTime.UtcNow.AddDays(5);

        var request = new CriarEventoRequest
        {
            Nome = "Evento Musical",
            Inicio = inicio,
            Fim = inicio.AddHours(3),
            Descricao = "Apresentação musical",
            Local = "Auditório"
        };

        // Act
        var resultado = await service.Cadastrar(request);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Evento Musical", resultado.Nome);
        Assert.Equal("evento-musical", resultado.Slug);

        Assert.Equal(
            usuarioFake.GetId(),
            resultado.OrganizadorId
        );

        Assert.Equal(
            1,
            await context.Set<Evento>().CountAsync()
        );
    }

    [Fact(DisplayName = "EventoService: deve diferenciar slugs duplicados")]
    public async Task Cadastrar_ComSlugDuplicado_DeveGerarNumeroDiferente()
    {
        // Arrange
        var (context, conexao) = await GetContextoBanco();

        var service = new EventoService(
            context,
            new FakeUsuarioAtual()
        );

        var inicio = DateTime.UtcNow.AddDays(5);

        var request = new CriarEventoRequest
        {
            Nome = "Evento Musical",
            Inicio = inicio,
            Fim = inicio.AddHours(3)
        };

        // Act
        var primeiro = await service.Cadastrar(request);
        var segundo = await service.Cadastrar(request);

        // Assert
        Assert.NotEqual(primeiro.Id, segundo.Id);

        Assert.NotEqual(
            primeiro.Slug,
            segundo.Slug
        );

        Assert.Equal(
            "evento-musical",
            primeiro.Slug
        );

        Assert.Equal(
            "evento-musical-2",
            segundo.Slug
        );

        Assert.Equal(
            2,
            await context.Set<Evento>().CountAsync()
        );
    }
}
