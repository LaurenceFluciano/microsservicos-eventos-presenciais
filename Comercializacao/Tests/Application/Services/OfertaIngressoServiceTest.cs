using Api.Application.DTO;
using Api.Application.DTO.Client;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Application.Services;
using Api.Infrastructure;
using Api.Model;
using Api.Error;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Api.Tests.Application.Services;

public class OfertaIngressoServiceTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DataContext _dataContext;

    private readonly Mock<IEventoClient> _eventoClient;
    private readonly Mock<IUsuarioAtual> _usuarioAtual;

    private readonly OfertaIngressoService _service;

    private readonly Guid _idEvento = Guid.CreateVersion7();
    private readonly Guid _idOrganizador = Guid.CreateVersion7();

    public OfertaIngressoServiceTest()
    {
        // Banco de dados
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .Options;

        _dataContext = new DataContext(options);
        _dataContext.Database.EnsureCreated();

        // Dependências
        _eventoClient = new Mock<IEventoClient>();
        _usuarioAtual = new Mock<IUsuarioAtual>();

        _usuarioAtual
            .Setup(u => u.GetId())
            .Returns(_idOrganizador);

        ConfigurarEvento(_idOrganizador);

        _service = new OfertaIngressoService(
            _dataContext,
            new OfertaIngressoMapper(),
            _eventoClient.Object,
            _usuarioAtual.Object
        );
    }

    // ========================================
    // CENÁRIO 1 – CADASTRO COM SUCESSO
    // ========================================

    [Fact]
    public async Task DeveCadastrarOfertaComSucesso()
    {
        // Arrange
        var request = CriarRequestValido();

        // Act
        var response = await _service.Criar(request);

        // Assert
        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync();

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(oferta.Id, response.Id);
        Assert.Equal(_idEvento, oferta.IdEvento);
        Assert.Equal(_idOrganizador, oferta.IdOrganizador);

        Assert.Equal(request.Nome, response.Nome);
        Assert.Equal(request.PrecoUnitario, response.PrecoUnitario);
        Assert.Equal(request.QuantidadeTotal, response.QuantidadeTotal);

        Assert.Equal(StatusOfertaIngresso.ATIVA, oferta.Status);
        Assert.Equal(StatusOfertaIngresso.ATIVA, response.Status);
        Assert.Equal(oferta.CriadoEm, response.CriadoEm);

        _eventoClient.Verify(
            client => client.ObterEventoAsync(
                _idEvento,
                It.IsAny<CancellationToken>()
            ),
            Times.Once()
        );
    }

    // ========================================
    // CENÁRIO 2 – OFERTA GRATUITA
    // ========================================

    [Fact]
    public async Task DeveCadastrarOfertaGratuita()
    {
        // Arrange
        var request = CriarRequestValido() with
        {
            PrecoUnitario = 0m
        };

        // Act
        var response = await _service.Criar(request);

        // Assert
        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(0m, response.PrecoUnitario);
        Assert.Equal(0m, oferta.Preco);
        Assert.True(oferta.IsGratis);
    }

    // ========================================
    // CENÁRIO 3 – PREÇO INVÁLIDO
    // ========================================

    [Fact]
    public async Task NaoDevePersistirOfertaComPrecoInvalido()
    {
        // Arrange
        var request = CriarRequestValido() with
        {
            PrecoUnitario = -10m
        };

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 4 – QUANTIDADE INVÁLIDA
    // ========================================

    [Fact]
    public async Task NaoDevePersistirOfertaComQuantidadeInvalida()
    {
        // Arrange
        var request = CriarRequestValido() with
        {
            QuantidadeTotal = 0
        };

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 5 – NOME INVÁLIDO
    // ========================================

    [Fact]
    public async Task NaoDevePersistirOfertaComNomeInvalido()
    {
        // Arrange
        var request = CriarRequestValido() with
        {
            Nome = ""
        };

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 6 – EVENTO DE OUTRO ORGANIZADOR
    // ========================================

    [Fact]
    public async Task NaoDeveCadastrarOfertaParaEventoDeOutroOrganizador()
    {
        // Arrange
        var outroOrganizador = Guid.CreateVersion7();

        ConfigurarEvento(outroOrganizador);

        var request = CriarRequestValido();

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.Equal(
            "Você não tem permissão para adicionar ofertas de ingressos nesse evento.",
            exception.Message
        );

        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 7 – EVENTO INEXISTENTE
    // ========================================

    [Fact]
    public async Task NaoDeveCadastrarOfertaParaEventoInexistente()
    {
        // Arrange
        var request = CriarRequestValido();

        _eventoClient
            .Setup(client => client.ObterEventoAsync(
                request.IdEvento,
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(
                new NotFoundException("Evento não encontrado.")
            );

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.Equal("Evento não encontrado.", exception.Message);

        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 8 – DISPONIBILIDADE INICIAL
    // ========================================

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task DeveRetornarDisponibilidadeInicialCorreta(
        int quantidade)
    {
        // Arrange
        var request = CriarRequestValido() with
        {
            QuantidadeTotal = quantidade
        };

        // Act
        var response = await _service.Criar(request);

        // Assert
        Assert.Equal(quantidade, response.QuantidadeTotal);
        Assert.Equal(quantidade, response.QuantidadeDisponivel);
    }

    // ========================================
    // CENÁRIO 11 – SERVIÇO INDISPONÍVEL
    // ========================================

    [Fact]
    public async Task NaoDevePersistirOfertaQuandoServicoEstiverIndisponivel()
    {
        // Arrange
        var request = CriarRequestValido();

        _eventoClient
            .Setup(client => client.ObterEventoAsync(
                request.IdEvento,
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(
                new HttpRequestException(
                    "Microsserviço de Eventos indisponível."
                )
            );

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.Criar(request)
        );

        // Assert
        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // MÉTODOS AUXILIARES
    // ========================================

    private CriarOfertaIngressoRequest CriarRequestValido()
    {
        return new CriarOfertaIngressoRequest(
            IdEvento: _idEvento,
            Nome: "Ingresso VIP",
            PrecoUnitario: 150m,
            QuantidadeTotal: 100
        );
    }

    private void ConfigurarEvento(Guid idOrganizador)
    {
        _eventoClient
            .Setup(client => client.ObterEventoAsync(
                _idEvento,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(
                new EventoClientResponse(
                    _idEvento,
                    idOrganizador,
                    "Evento Exemplo"
                )
            );
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _connection.Dispose();
    }
}