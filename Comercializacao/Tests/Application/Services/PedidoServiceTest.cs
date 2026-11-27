
using Api;
using Api.Application.DTO.Pedidos;
using Api.Application.Events;
using Api.Application.Mappers;
using Api.Application.Services;
using Api.Error;
using Api.Infrastructure;
using Api.Infrastructure.Authentication;
using Api.Infrastructure.Client;
using Api.Model;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Infrastructure;

namespace Tests.Application.Services;

public sealed class PedidoServiceTest
{
    private static readonly Guid EventoId =
        Guid.Parse("01a1f4d0-b140-7372-84cd-f2ff6f1af9f5");

    private static readonly Guid OrganizadorId =
        Guid.Parse("019a6f34-8c12-7000-8000-000000000001");

    private readonly Guid _compradorId = Guid.CreateVersion7();

    public sealed class Cenario : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public DataContext Db { get; }
        public PedidoService Service { get; }
        public Mock<IPublisher> Publisher { get; }

        public Cenario()
        {
            _connection = new SqliteConnection(
                "Data Source=:memory:"
            );

            _connection.Open();

            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(_connection)
                .Options;

            Db = new DataContext(options);
            Db.Database.EnsureCreated();

            var eventoClient = new FakeEventoClient();

            var usuarioAtual = new FakeUsuarioAtual
            {
                UsuarioId = OrganizadorId
            };

            var ofertaMapper = new OfertaIngressoMapper();

            var ofertaService = new OfertaIngressoService(
                Db,
                ofertaMapper,
                eventoClient,
                usuarioAtual
            );

            Publisher = new Mock<IPublisher>();

            Publisher.Setup(p => p.Publish(
                    It.IsAny<PedidoCriadoEvent>(),
                    It.IsAny<CancellationToken>()
                ))
                .Returns(Task.CompletedTask);

            Service = new PedidoService(
                Db,
                new PedidoMapper(),
                ofertaService,
                eventoClient,
                Publisher.Object
            );
        }

        [Fact]
        public async Task ObterPorId_DeveRetornarNull_QuandoPedidoNaoExiste()
        {
            // Arrange
            await using var database = new SqliteTestDatabase();

            var publisher = new Mock<IPublisher>();

            // O método ObterPorId utiliza apenas DataContext.
            var service = new PedidoService(
                database.Context,
                null!,
                null!,
                null!,
                publisher.Object
            );

            // Act
            var resultado = await service.ObterPorId(
                Guid.CreateVersion7()
            );

            // Assert
            Assert.Null(resultado);
        }

        public async Task<OfertaIngresso> CriarOferta(
            int quantidade = 10,
            decimal preco = 50m,
            bool encerrada = false)
        {
            var oferta = new OfertaIngresso(
                nome: "Ingresso VIP",
                preco: preco,
                quantidade: quantidade,
                idEvento: EventoId,
                idOrganizador: OrganizadorId
            );

            if (encerrada)
                oferta.Encerrar();

            Db.OfertasIngresso.Add(oferta);
            await Db.SaveChangesAsync();

            return oferta;
        }

        public async Task<Pedido> Reservar(
            OfertaIngresso oferta,
            Guid compradorId,
            int quantidade)
        {
            var pedido = new Pedido(
                oferta.Id,
                compradorId,
                quantidade,
                oferta.Preco
            );

            Db.Pedidos.Add(pedido);
            await Db.SaveChangesAsync();

            return pedido;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    #region Criacao

    [Fact]
    public async Task DeveCriarPedidoComOfertaValida()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta();

        var request = new CriarPedidoRequest(
            oferta.Id,
            _compradorId,
            2
        );

        var response = await cenario.Service
            .CriarPedido(request);

        var pedido = await cenario.Db.Pedidos
            .AsNoTracking()
            .SingleAsync(p => p.Id == response.PedidoId);

        Assert.Equal(oferta.Id, pedido.OfertaId);
        Assert.Equal(_compradorId, pedido.CompradorId);
        Assert.Equal(2, pedido.Quantidade);
        Assert.Equal(50m, pedido.ValorUnitario);
        Assert.Equal(StatusPedido.PENDENTE, pedido.Status);
        Assert.True(pedido.ExpiraEm > DateTime.UtcNow);
    }

    [Fact]
    public async Task DevePublicarPedidoCriadoEvent()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta();

        var response = await cenario.Service.CriarPedido(
            new CriarPedidoRequest(
                oferta.Id,
                _compradorId,
                1
            )
        );

        cenario.Publisher.Verify(
            p => p.Publish(
                It.Is<PedidoCriadoEvent>(
                    e => e.PedidoId == response.PedidoId
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task NaoDeveCriarPedidoQuandoOfertaNaoExiste()
    {
        await using var cenario = new Cenario();

        await Assert.ThrowsAsync<NotFoundException>(
            () => cenario.Service.CriarPedido(
                new CriarPedidoRequest(
                    Guid.CreateVersion7(),
                    _compradorId,
                    1
                )
            )
        );

        Assert.Empty(cenario.Db.Pedidos);
    }

    [Fact]
    public async Task NaoDeveCriarPedidoQuandoOfertaEncerrada()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            encerrada: true
        );

        await Assert.ThrowsAsync<DomainException>(
            () => cenario.Service.CriarPedido(
                new CriarPedidoRequest(
                    oferta.Id,
                    _compradorId,
                    1
                )
            )
        );

        Assert.Empty(cenario.Db.Pedidos);
    }

    [Fact]
    public async Task NaoDeveCriarPedidoQuandoOfertaEsgotada()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            quantidade: 1
        );

        oferta.Esgotado();
        await cenario.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => cenario.Service.CriarPedido(
                new CriarPedidoRequest(
                    oferta.Id,
                    _compradorId,
                    1
                )
            )
        );
    }

    [Fact]
    public async Task NaoDeveCriarPedidoSemVagasDisponiveis()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            quantidade: 2
        );

        await cenario.Reservar(
            oferta,
            Guid.CreateVersion7(),
            2
        );

        var erro = await Assert.ThrowsAsync<DomainException>(
            () => cenario.Service.CriarPedido(
                new CriarPedidoRequest(
                    oferta.Id,
                    _compradorId,
                    1
                )
            )
        );

        Assert.Equal(
            "Não há mais ingressos disponíveis.",
            erro.Message
        );

        Assert.Equal(
            1,
            await cenario.Db.Pedidos.CountAsync()
        );
    }

    [Fact]
    public async Task NaoDeveCriarPedidoAcimaDaDisponibilidade()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            quantidade: 10
        );

        await cenario.Reservar(
            oferta,
            Guid.CreateVersion7(),
            7
        );

        var erro = await Assert.ThrowsAsync<DomainException>(
            () => cenario.Service.CriarPedido(
                new CriarPedidoRequest(
                    oferta.Id,
                    _compradorId,
                    4
                )
            )
        );

        Assert.Contains("Restam 3 ingressos", erro.Message);
    }

    [Fact]
    public async Task DeveCriarPedidoComQuantidadeRestanteExata()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            quantidade: 10
        );

        await cenario.Reservar(
            oferta,
            Guid.CreateVersion7(),
            7
        );

        var response = await cenario.Service.CriarPedido(
            new CriarPedidoRequest(
                oferta.Id,
                _compradorId,
                3
            )
        );

        var pedido = await cenario.Db.Pedidos
            .SingleAsync(p => p.Id == response.PedidoId);

        Assert.Equal(3, pedido.Quantidade);
        Assert.Equal(2, await cenario.Db.Pedidos.CountAsync());
    }

    [Fact]
    public async Task DeveCriarPedidoGratuito()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta(
            preco: 0m
        );

        var response = await cenario.Service.CriarPedido(
            new CriarPedidoRequest(
                oferta.Id,
                _compradorId,
                2
            )
        );

        var pedido = await cenario.Db.Pedidos
            .SingleAsync(p => p.Id == response.PedidoId);

        Assert.True(pedido.IsGratis);
        Assert.Equal(0m, pedido.ValorTotal());
        Assert.Equal(StatusPedido.PENDENTE, pedido.Status);
    }

    #endregion

    #region Consultas

    [Fact]
    public async Task DeveRetornarNullQuandoPedidoNaoExiste()
    {
        await using var cenario = new Cenario();

        var response = await cenario.Service.ObterPorId(
            Guid.CreateVersion7()
        );

        Assert.Null(response);
    }

    [Fact]
    public async Task DeveRetornarDetalhesDoPedido()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta();

        var pedido = await cenario.Reservar(
            oferta,
            _compradorId,
            3
        );

        var response = await cenario.Service.ObterPorId(
            pedido.Id
        );

        Assert.NotNull(response);

        Assert.Equal(EventoId, response.EventoId);
        Assert.Equal(pedido.Id, response.PedidoId);
        Assert.Equal(oferta.Id, response.OfertaId);
        Assert.Equal(3, response.Quantidade);
        Assert.Equal(StatusPedido.PENDENTE, response.Status);
        Assert.Equal(pedido.ExpiraEm, response.ExpiraEm);

        Assert.Null(response.Pagamento);
        Assert.Empty(response.Ingressos);
    }

    [Fact]
    public async Task DeveRetornarPagamentoDoPedido()
    {
        await using var cenario = new Cenario();

        var oferta = await cenario.CriarOferta();

        var pedido = await cenario.Reservar(
            oferta,
            _compradorId,
            2
        );

        var pagamento = new Pagamento(
            pedido.Id,
            pedido.ValorTotal()
        )
        {
            UrlCheckout = "https://checkout.stripe.com/teste"
        };

        cenario.Db.Pagamentos.Add(pagamento);
        await cenario.Db.SaveChangesAsync();

        var response = await cenario.Service.ObterPorId(
            pedido.Id
        );

        Assert.NotNull(response);
        Assert.NotNull(response.Pagamento);
        Assert.Equal(
            pagamento.Id,
            response.Pagamento.PagamentoId
        );
        Assert.Equal(
            pagamento.UrlCheckout,
            response.Pagamento.UrlPagamento
        );
    }

    #endregion
}
