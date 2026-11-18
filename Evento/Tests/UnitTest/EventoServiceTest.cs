
using Api;
using Api.Error;
using Api.Application.DTO;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure.Authentication;
using Api.Infrastructure.Integration;
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

    [Fact(DisplayName = "Publicar: evento com oferta valida deve publicar e retornar URL")]
    public async Task Publicar_ComOfertaValida_DevePublicar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var usuario = new FakeUsuarioAtual();
            var fake = new FakeComercializacaoClient { PossuiOfertaValida = true };
            var service = new EventoService(context, usuario, fake);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Festival de Cinema",
                Inicio = inicio,
                Fim = inicio.AddHours(3)
            });

            var publicado = await service.Publicar(
                criado.Id,
                publicBaseUrl: "https://eventos.exemplo.com:9443/portal/");
            Assert.Equal(EnumStatusPublicacaoEvento.PUBLICADO, publicado.StatusPublicacao);
            Assert.NotNull(publicado.PublicadoEm);
            Assert.Equal(
                $"https://eventos.exemplo.com:9443/portal/api/eventos/publicado/{publicado.Slug}",
                publicado.UrlEvento);

            var gravado = await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id);
            Assert.NotNull(gravado.PublicadoEm);
        }
    }

    [Fact(DisplayName = "Publicar: sem host informado deve devolver caminho relativo")]
    public async Task Publicar_SemBaseUrl_DeveRetornarCaminhoRelativo()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(
                context,
                new FakeUsuarioAtual(),
                new FakeComercializacaoClient());

            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento com endereco relativo",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            var publicado = await service.Publicar(criado.Id);

            Assert.Equal(
                $"/api/eventos/publicado/{publicado.Slug}",
                publicado.UrlEvento);
        }
    }

    [Fact(DisplayName = "Publicar: evento sem oferta valida deve ser recusado")]
    public async Task Publicar_SemOfertasValidas_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var fake = new FakeComercializacaoClient { PossuiOfertaValida = false };
            var service = new EventoService(context, new FakeUsuarioAtual(), fake);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento sem Ofertas",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            await Assert.ThrowsAsync<DomainException>(() => service.Publicar(criado.Id));
            Assert.Null((await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id)).PublicadoEm);
        }
    }

    [Fact(DisplayName = "Publicar: outro organizador nao pode publicar")]
    public async Task Publicar_OutroOrganizador_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var usuario = new FakeUsuarioAtual();
            var fake = new FakeComercializacaoClient { SimularIndisponibilidade = true };
            var service = new EventoService(context, usuario, fake);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Meu Evento",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            usuario.AlterarUsuario(Guid.NewGuid());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.Publicar(criado.Id));
            Assert.Null((await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id)).PublicadoEm);
        }
    }

    [Fact(DisplayName = "Publicar: evento inexistente deve retornar nao encontrado")]
    public async Task Publicar_EventoInexistente_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(
                context, new FakeUsuarioAtual(), new FakeComercializacaoClient());
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.Publicar(Guid.NewGuid()));
        }
    }

    [Fact(DisplayName = "Publicar: evento ja publicado deve preservar a data")]
    public async Task Publicar_EventoJaPublicado_DeveSerIdempotente()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var fake = new FakeComercializacaoClient { PossuiOfertaValida = true };
            var service = new EventoService(context, new FakeUsuarioAtual(), fake);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento Publicado",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });
            var primeira = await service.Publicar(criado.Id);

            fake.PossuiOfertaValida = false;
            fake.SimularIndisponibilidade = true;
            var segunda = await service.Publicar(criado.Id);

            Assert.Equal(primeira.PublicadoEm, segunda.PublicadoEm);
            Assert.Equal(primeira.UrlEvento, segunda.UrlEvento);
            Assert.Equal(EnumStatusPublicacaoEvento.PUBLICADO, segunda.StatusPublicacao);
        }
    }

    [Fact(DisplayName = "Publicar: falha em Comercializacao nao altera evento")]
    public async Task Publicar_ComercializacaoIndisponivel_DevePreservarEstado()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var fake = new FakeComercializacaoClient { SimularIndisponibilidade = true };
            var service = new EventoService(context, new FakeUsuarioAtual(), fake);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento Indisponivel",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.Publicar(criado.Id));
            Assert.Null((await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id)).PublicadoEm);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH deve preservar campos ausentes")]
    public async Task Atualizar_SomenteDescricao_DevePreservarDemaisCampos()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var usuario = new FakeUsuarioAtual();
            var service = new EventoService(context, usuario);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Feira de Tecnologia",
                Inicio = inicio,
                Fim = inicio.AddHours(3),
                Descricao = "Original",
                Local = "Centro"
            });

            var atualizado = await service.Atualizar(
                criado.Id,
                new AtualizarEventoRequest { Descricao = "Descrição corrigida" }
            );

            Assert.Equal("Descrição corrigida", atualizado.Descricao);
            Assert.Equal(criado.Nome, atualizado.Nome);
            Assert.Equal(criado.Local, atualizado.Local);
            Assert.Equal(criado.Inicio, atualizado.Inicio);
            Assert.Equal(criado.Fim, atualizado.Fim);
            Assert.Equal(criado.Slug, atualizado.Slug);
            Assert.Equal(usuario.GetId(), atualizado.OrganizadorId);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH permite atualizar evento publicado")]
    public async Task Atualizar_EventoPublicado_DevePermitirAlteracao()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(context, new FakeUsuarioAtual());
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Congresso",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            var entidade = await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id);
            entidade.PublicadoEm = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var atualizado = await service.Atualizar(
                criado.Id,
                new AtualizarEventoRequest { Local = "Novo local" }
            );

            Assert.Equal("Novo local", atualizado.Local);
            Assert.Equal(EnumStatusPublicacaoEvento.PUBLICADO, atualizado.StatusPublicacao);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH recusa outro organizador")]
    public async Task Atualizar_OrganizadorDiferente_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var usuario = new FakeUsuarioAtual();
            var service = new EventoService(context, usuario);
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Meu Evento",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            usuario.AlterarUsuario(Guid.NewGuid());
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.Atualizar(
                    criado.Id,
                    new AtualizarEventoRequest { Nome = "Nome adulterado" }
                )
            );

            var entidade = await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id);
            Assert.Equal("Meu Evento", entidade.Nome);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH gera slug único ao trocar o nome")]
    public async Task Atualizar_ParaNomeExistente_DeveNumerarSlug()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(context, new FakeUsuarioAtual());
            var inicio = DateTime.UtcNow.AddDays(5);
            var primeiro = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento Musical",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });
            var segundo = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Evento Cultural",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            var atualizado = await service.Atualizar(
                segundo.Id,
                new AtualizarEventoRequest { Nome = "Evento Musical" }
            );

            Assert.Equal("evento-musical-2", atualizado.Slug);
            Assert.Equal("evento-musical", primeiro.Slug);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH rejeita período resultante inválido")]
    public async Task Atualizar_ApenasFimInvalido_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(context, new FakeUsuarioAtual());
            var inicio = DateTime.UtcNow.AddDays(5);
            var criado = await service.Cadastrar(new CriarEventoRequest
            {
                Nome = "Feira de Ciências",
                Inicio = inicio,
                Fim = inicio.AddHours(2)
            });

            await Assert.ThrowsAsync<DomainException>(
                () => service.Atualizar(
                    criado.Id,
                    new AtualizarEventoRequest { Fim = inicio.AddHours(-1) }
                )
            );

            var entidade = await context.Set<Evento>().SingleAsync(e => e.Id == criado.Id);
            Assert.Equal(inicio.AddHours(2), entidade.Fim);
        }
    }

    [Fact(DisplayName = "EventoService: PATCH recusa evento inexistente")]
    public async Task Atualizar_EventoInexistente_DeveRecusar()
    {
        var (context, conexao) = await GetContextoBanco();
        using (context)
        using (conexao)
        {
            var service = new EventoService(context, new FakeUsuarioAtual());

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.Atualizar(
                    Guid.NewGuid(),
                    new AtualizarEventoRequest { Local = "Nova localização" }
                )
            );
        }
    }
}
