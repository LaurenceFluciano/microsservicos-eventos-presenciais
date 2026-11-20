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

    #region Testando Método Criar

    // ========================================
    // CENÁRIO 1 - CADASTRO COM SUCESSO
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
    // CENÁRIO 2 - OFERTA GRATUITA
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
    // CENÁRIO 3 - PREÇO INVÁLIDO
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
    // CENÁRIO 4 - QUANTIDADE INVÁLIDA
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
    // CENÁRIO 5 - NOME INVÁLIDO
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
    // CENÁRIO 6 - EVENTO DE OUTRO ORGANIZADOR
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
    // CENÁRIO 7 - EVENTO INEXISTENTE
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
    // CENÁRIO 8 - DISPONIBILIDADE INICIAL
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
    // CENÁRIO 11 - SERVIÇO INDISPONÍVEL
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

    #endregion

        
    #region Testando Método Atualizar

    // ========================================
    // CENÁRIO 1 - Atualização bem-sucedida
    // ========================================

    [Fact]
    public async Task DeveAtualizarOfertaComSucesso()
    {
        // Arrange
        var criada = await _service.Criar(CriarRequestValido());

        var request = CriarRequestAtualizacao(
            quantidade: 150,
            preco: 200m
        );

        // Act
        var response = await _service.Atualizar(criada.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == criada.Id);

        Assert.Equal(request.Nome, oferta.Nome);
        Assert.Equal(request.PrecoUnitario, oferta.Preco);
        Assert.Equal(request.QuantidadeTotal, oferta.QuantidadeTotal);

        Assert.Equal(criada.Id, response.Id);
        Assert.Equal(_idEvento, oferta.IdEvento);
        Assert.Equal(_idOrganizador, oferta.IdOrganizador);
    }

    // ========================================
    // CENÁRIO 2 - Restrição de responsabilidade
    // ========================================

    [Fact]
    public async Task NaoDeveAtualizarOfertaDeOutroOrganizador()
    {
        // Arrange
        var criada = await _service.Criar(CriarRequestValido());

        _usuarioAtual
            .Setup(u => u.GetId())
            .Returns(Guid.CreateVersion7());

        var request = CriarRequestAtualizacao(
            quantidade: 150,
            preco: 200m
        );

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(criada.Id, request)
        );

        // Assert
        Assert.Equal(
            "Você não tem permissão para atualizar essa oferta.",
            exception.Message
        );

        _dataContext.ChangeTracker.Clear();

        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == criada.Id);

        Assert.Equal("Ingresso VIP", oferta.Nome);
        Assert.Equal(150m, oferta.Preco);
        Assert.Equal(100, oferta.QuantidadeTotal);
    }

    // ========================================
    // CENÁRIO 3 - Validação dos dados
    // ========================================

    [Theory]
    [InlineData(-10, 100)]
    [InlineData(150, 0)]
    [InlineData(150, -1)]
    public async Task NaoDeveAtualizarOfertaComDadosInvalidos(
        decimal preco,
        int quantidade)
    {
        // Arrange
        var criada = await _service.Criar(CriarRequestValido());

        var request = CriarRequestAtualizacao(
            quantidade: quantidade,
            preco: preco
        );

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(criada.Id, request)
        );

        // Assert
        _dataContext.ChangeTracker.Clear();

        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == criada.Id);

        Assert.Equal("Ingresso VIP", oferta.Nome);
        Assert.Equal(150m, oferta.Preco);
        Assert.Equal(100, oferta.QuantidadeTotal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NaoDeveAtualizarOfertaComNomeInvalido(
        string nome)
    {
        // Arrange
        var criada = await _service.Criar(CriarRequestValido());

        var request = CriarRequestAtualizacao() with
        {
            Nome = nome
        };

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(criada.Id, request)
        );

        // Assert
        _dataContext.ChangeTracker.Clear();

        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == criada.Id);

        Assert.Equal("Ingresso VIP", oferta.Nome);
    }

    // ========================================
    // CENÁRIO 4 - Oferta gratuita
    // ========================================

    [Fact]
    public async Task DevePermitirAtualizarOfertaParaGratuita()
    {
        // Arrange
        var criada = await _service.Criar(CriarRequestValido());

        var request = CriarRequestAtualizacao(preco: 0m);

        // Act
        var response = await _service.Atualizar(criada.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var oferta = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == criada.Id);

        Assert.Equal(0m, response.PrecoUnitario);
        Assert.Equal(0m, oferta.Preco);
        Assert.True(oferta.IsGratis);
    }

    // ========================================
    // CENÁRIO 5 - Proteção de ingressos comprometidos
    // ========================================

    [Theory]
    [InlineData(1)]
    [InlineData(45)]
    [InlineData(30)]
    [InlineData(-10)]
    public async Task NaoDeveAtualizarQuantidadeTotalComIngressosComprometidos(
        int quantidade)
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 40
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 10,
            expiraEm: DateTime.UtcNow.AddHours(1)
        );

        var request = CriarRequestAtualizacao(quantidade);

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(oferta.Id, request)
        );

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(100, persistida.QuantidadeTotal);
        Assert.Equal(150m, persistida.Preco);
        Assert.Equal("Ingresso VIP", persistida.Nome);
    }

    [Fact]
    public async Task DevePermitirQuantidadeIgualAosIngressosComprometidos()
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 40
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 10,
            expiraEm: DateTime.UtcNow.AddHours(1)
        );

        var request = CriarRequestAtualizacao(quantidade: 50);

        // Act
        var response = await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(50, persistida.QuantidadeTotal);
        Assert.Equal(50, response.QuantidadeTotal);
    }

    // ========================================
    // CENÁRIO 6 - Atualização da disponibilidade
    // ========================================

    [Theory]
    [InlineData(150)]
    [InlineData(200)]
    public async Task DeveAtualizarQuantidadeTotalAoAumentar(
        int quantidade)
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 40
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 10,
            expiraEm: DateTime.UtcNow.AddHours(1)
        );

        var request = CriarRequestAtualizacao(quantidade);

        // Act
        var response = await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(quantidade, persistida.QuantidadeTotal);
        Assert.Equal(quantidade, response.QuantidadeTotal);

        // Disponibilidade esperada = total - 50 comprometidos.
        var disponibilidadeEsperada = quantidade - 50;

        Assert.Equal(
            quantidade - 50,
            response.QuantidadeDisponivel
        );
    }

    // ========================================
    // CENÁRIO 7 - Preservação dos pedidos
    // ========================================

    [Theory]
    [InlineData(70)]
    [InlineData(40)]
    [InlineData(200)]
    public async Task DeveAtualizarPrecoOfertaEManterPrecoNoPedido(
        decimal preco)
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        var pedidoPago = await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 2,
            valorUnitario: 150m
        );

        var pedidoPendente = await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 3,
            valorUnitario: 150m,
            expiraEm: DateTime.UtcNow.AddHours(1)
        );

        var request = CriarRequestAtualizacao(preco: preco);

        // Act
        await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var ofertaPersistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        var pedidos = await _dataContext.Pedidos
            .AsNoTracking()
            .Where(p =>
                p.Id == pedidoPago.Id ||
                p.Id == pedidoPendente.Id
            )
            .ToListAsync();

        Assert.Equal(preco, ofertaPersistida.Preco);

        Assert.Equal(2, pedidos.Count);

        Assert.All(pedidos, pedido =>
        {
            Assert.Equal(150m, pedido.ValorUnitario);
        });

        Assert.Equal(
            2,
            pedidos.Single(p => p.Id == pedidoPago.Id).Quantidade
        );

        Assert.Equal(
            3,
            pedidos.Single(p => p.Id == pedidoPendente.Id).Quantidade
        );
    }

    // ========================================
    // CENÁRIO 8 - Oferta inexistente
    // ========================================

    [Fact]
    public async Task DeveLancarOfertaInexistenteQuandoNaoEncontrado()
    {
        // Arrange
        var idInexistente = Guid.CreateVersion7();
        var request = CriarRequestAtualizacao();

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Atualizar(idInexistente, request)
        );

        // Assert
        Assert.Equal("Oferta não encontrada.", exception.Message);

        Assert.False(
            await _dataContext.OfertasIngresso.AnyAsync()
        );
    }

    // ========================================
    // CENÁRIO 9 - Atualização de oferta esgotada
    // ========================================

    [Theory]
    [InlineData(60)]
    [InlineData(70)]
    public async Task DeveFicarAtivoQuandoAtualizarQuantidadeTotalEEstiverEsgotada(
        int quantidade)
    {
        // Arrange
        var oferta = await _service.Criar(
            CriarRequestValido() with
            {
                QuantidadeTotal = 50
            }
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 40
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 10,
            expiraEm: DateTime.UtcNow.AddHours(1)
        );

        var request = CriarRequestAtualizacao(quantidade);

        // Act
        var response = await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(quantidade, persistida.QuantidadeTotal);
        Assert.Equal(StatusOfertaIngresso.ATIVA, persistida.Status);
        Assert.Equal(StatusOfertaIngresso.ATIVA, response.Status);

        // A oferta tinha 50 ingressos comprometidos.
        Assert.True(quantidade - 50 > 0);
    }

    // ========================================
    // CENÁRIO 10 - Atualização de oferta encerrada
    // ========================================

    [Theory]
    [InlineData(60)]
    [InlineData(70)]
    public async Task NaoDeveAtualizarOfertaEncerrada(
        int quantidade)
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        var entidade = await _dataContext.OfertasIngresso
            .SingleAsync(o => o.Id == oferta.Id);

        entidade.Encerrar();

        await _dataContext.SaveChangesAsync();

        var request = CriarRequestAtualizacao(quantidade);

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(oferta.Id, request)
        );

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(StatusOfertaIngresso.ENCERRADA, persistida.Status);
        Assert.Equal(100, persistida.QuantidadeTotal);
        Assert.Equal(150m, persistida.Preco);
    }

    // ========================================
    // CENÁRIO 11 - Pedidos cancelados e expirados
    // ========================================

    [Fact]
    public async Task NaoDeveConsiderarPedidosCanceladosOuExpirados()
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 20
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.CANCELADO,
            quantidade: 30
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.EXPIRADO,
            quantidade: 40
        );

        var request = CriarRequestAtualizacao(quantidade: 20);

        // Act
        var response = await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(20, persistida.QuantidadeTotal);
        Assert.Equal(20, response.QuantidadeTotal);
    }

    // ========================================
    // CENÁRIO 12 - Reserva pendente fora do prazo
    // ========================================

    [Fact]
    public async Task NaoDeveConsiderarPedidoPendenteComPrazoExpirado()
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 20
        );

        await CriarPedido(
            oferta.Id,
            StatusPedido.PENDENTE,
            quantidade: 30,
            expiraEm: DateTime.UtcNow.AddHours(-1)
        );

        var request = CriarRequestAtualizacao(quantidade: 20);

        // Act
        var response = await _service.Atualizar(oferta.Id, request);

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(20, persistida.QuantidadeTotal);
        Assert.Equal(20, response.QuantidadeTotal);
    }

    // ========================================
    // CENÁRIO 13 - Falha não deve persistir alterações
    // ========================================

    [Fact]
    public async Task NaoDevePersistirAlteracoesParciaisQuandoQuantidadeForInvalida()
    {
        // Arrange
        var oferta = await _service.Criar(CriarRequestValido());

        await CriarPedido(
            oferta.Id,
            StatusPedido.PAGO,
            quantidade: 80
        );

        var request = new AtualizarOfertaIngressoRequest(
            Nome: "Novo nome",
            PrecoUnitario: 200m,
            QuantidadeTotal: 50
        );

        // Act
        await Assert.ThrowsAsync<DomainException>(
            () => _service.Atualizar(oferta.Id, request)
        );

        // Assert
        _dataContext.ChangeTracker.Clear();

        var persistida = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal("Ingresso VIP", persistida.Nome);
        Assert.Equal(150m, persistida.Preco);
        Assert.Equal(100, persistida.QuantidadeTotal);
    }

    #endregion


    #region Métodos Auxiliares

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

    private async Task<Pedido> CriarPedido(
        Guid ofertaId,
        StatusPedido status,
        int quantidade,
        decimal valorUnitario = 150m,
        DateTime? expiraEm = null)
    {
        var pedido = new Pedido
        {
            CompradorId = Guid.CreateVersion7(),
            OfertaId = ofertaId,
            Quantidade = quantidade,
            ValorUnitario = valorUnitario,
            ExpiraEm = expiraEm
        };

        _dataContext.Entry(pedido)
            .Property(p => p.Status)
            .CurrentValue = status;

        _dataContext.Pedidos.Add(pedido);
        await _dataContext.SaveChangesAsync();

        return pedido;
    }

    private AtualizarOfertaIngressoRequest CriarRequestAtualizacao(
        int quantidade = 100,
        decimal preco = 150m)
    {
        return new AtualizarOfertaIngressoRequest(
            Nome: "Ingresso VIP Atualizado",
            PrecoUnitario: preco,
            QuantidadeTotal: quantidade
        );
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _connection.Dispose();
    }

    #endregion
}