
using Api.Error;
using Api.Model;

namespace Tests.Model;

public class PedidoTest
{
    private readonly Guid _ofertaId = Guid.CreateVersion7();
    private readonly Guid _compradorId = Guid.CreateVersion7();

    private Pedido CriarPedido(
        int quantidade = 2,
        decimal valorUnitario = 50m,
        DateTime? expiraEm = null)
    {
        return new Pedido(
            idOferta: _ofertaId,
            idComprador: _compradorId,
            quantidade: quantidade,
            valorUnitario: valorUnitario
        )
        {
            ExpiraEm = expiraEm ?? DateTime.UtcNow.AddMinutes(30)
        };
    }

    #region Construtor

    [Fact]
    public void DeveCriarPedidoPendenteComDadosValidos()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var pedido = new Pedido(
            _ofertaId,
            _compradorId,
            2,
            50m
        );

        var depois = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, pedido.Id);
        Assert.Equal(_ofertaId, pedido.OfertaId);
        Assert.Equal(_compradorId, pedido.CompradorId);
        Assert.Equal(2, pedido.Quantidade);
        Assert.Equal(50m, pedido.ValorUnitario);
        Assert.Equal(StatusPedido.PENDENTE, pedido.Status);
        Assert.InRange(pedido.CriadoEm, antes, depois);


        Assert.InRange(
            pedido.ExpiraEm,
            antes.AddHours(1),
            depois.AddHours(1)
        );
        Assert.Empty(pedido.Pagamentos);
        Assert.Empty(pedido.Ingressos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void NaoDeveCriarPedidoComQuantidadeInvalida(
        int quantidade)
    {
        Assert.Throws<DomainException>(() =>
            new Pedido(_ofertaId, _compradorId, quantidade, 50m));
    }

    [Fact]
    public void NaoDeveCriarPedidoComPrecoNegativo()
    {
        Assert.Throws<DomainException>(() =>
            new Pedido(_ofertaId, _compradorId, 2, -0.01m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(150)]
    public void DeveCriarPedidoComPrecoNaoNegativo(
        decimal preco)
    {
        var pedido = CriarPedido(valorUnitario: preco);

        Assert.Equal(preco, pedido.ValorUnitario);
    }

    [Fact]
    public void DeveIdentificarPedidoGratuito()
    {
        var pedido = CriarPedido(valorUnitario: 0m);

        Assert.True(pedido.IsGratis);
        Assert.Equal(StatusPedido.PENDENTE, pedido.Status);
    }

    [Fact]
    public void DeveIdentificarPedidoPago()
    {
        var pedido = CriarPedido(valorUnitario: 50m);

        Assert.False(pedido.IsGratis);
    }

    [Fact]
    public void NaoDeveAlterarQuantidadeParaValorInvalido()
    {
        var pedido = CriarPedido();

        Assert.Throws<DomainException>(
            () => pedido.Quantidade = 0
        );

        Assert.Equal(2, pedido.Quantidade);
    }

    [Fact]
    public void NaoDeveAlterarPrecoParaValorNegativo()
    {
        var pedido = CriarPedido();

        Assert.Throws<DomainException>(
            () => pedido.ValorUnitario = -1m
        );

        Assert.Equal(50m, pedido.ValorUnitario);
    }

    #endregion

    #region Expiracao

    [Fact]
    public void PedidoPendenteDentroDoPrazoNaoEstaExpirado()
    {
        // Arrange
        var agora = DateTime.UtcNow;

        var pedido = CriarPedido(
            expiraEm: agora.AddMinutes(1)
        );

        // Act & Assert
        Assert.False(pedido.EstaExpirado(agora));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void PedidoPendenteComPrazoAtingidoEstaExpirado(
        int minutos)
    {
        // Arrange
        var agora = DateTime.UtcNow;

        var pedido = CriarPedido(
            expiraEm: agora.AddMinutes(minutos)
        );

        // Act & Assert
        Assert.True(pedido.EstaExpirado(agora));
    }

    [Fact]
    public void PedidoDeveTerExpiracaoAutomatica()
    {
        var antes = DateTime.UtcNow;

        var pedido = new Pedido(
            _ofertaId,
            _compradorId,
            1,
            10m
        );

        var depois = DateTime.UtcNow;

        Assert.InRange(
            pedido.ExpiraEm,
            antes.AddHours(1),
            depois.AddHours(1)
        );

        Assert.False(pedido.EstaExpirado(depois));
    }

    [Fact]
    public void ConsultarExpiracaoNaoDeveAlterarStatusPersistido()
    {
        // Arrange
        var agora = DateTime.UtcNow;

        var pedido = CriarPedido(
            expiraEm: agora.AddMinutes(-1)
        );

        // Act
        var expirado = pedido.EstaExpirado(agora);

        // Assert
        Assert.True(expirado);
        Assert.Equal(StatusPedido.PENDENTE, pedido.Status);
    }

    [Fact]
    public void PedidoEmProcessamentoNaoDeveExpirarPeloPrazoDaReserva()
    {
        // Arrange
        var pedido = CriarPedido(
            expiraEm: DateTime.UtcNow.AddMinutes(1)
        );

        pedido.MarcarEmProcessamento();

        // Simula consulta após o prazo da reserva.
        var depois = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        Assert.False(pedido.EstaExpirado(depois));
        Assert.Equal(
            StatusPedido.EM_PROCESSAMENTO,
            pedido.Status
        );
    }

    [Fact]
    public void PedidoConfirmadoNaoDeveExpirar()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();
        pedido.Confirmar();

        // Act & Assert
        Assert.False(
            pedido.EstaExpirado(DateTime.UtcNow.AddDays(1))
        );
    }

    #endregion

    #region Processamento

    [Fact]
    public void DeveMarcarPedidoPendenteEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        // Act
        pedido.MarcarEmProcessamento();

        // Assert
        Assert.Equal(
            StatusPedido.EM_PROCESSAMENTO,
            pedido.Status
        );

        Assert.True(pedido.EstaEmProcessamento());
        Assert.False(pedido.EstaPendente());
        Assert.False(pedido.EstaConfirmado());
    }

    [Fact]
    public void DeveMarcarPedidoPagoEmProcessamentoQuandoSolicitado()
    {
        // A validação de pagamento confirmado pertence a ProcessarPedido,
        // que consulta PagamentoService, não à entidade Pedido.
        var pedido = CriarPedido(valorUnitario: 50m);

        pedido.MarcarEmProcessamento();

        Assert.True(pedido.EstaEmProcessamento());
        Assert.Equal(StatusPedido.EM_PROCESSAMENTO, pedido.Status);
    }

    [Fact]
    public void DevePermitirProcessamentoDePedidoGratuito()
    {
        // Arrange
        var pedido = CriarPedido(valorUnitario: 0m);

        // Act
        pedido.MarcarEmProcessamento();

        // Assert
        Assert.True(pedido.IsGratis);

        Assert.Equal(
            StatusPedido.EM_PROCESSAMENTO,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveMarcarPedidoJaEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.MarcarEmProcessamento()
        );

        Assert.True(pedido.EstaEmProcessamento());
    }

    [Fact]
    public void NaoDeveMarcarPedidoConfirmadoEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();
        pedido.Confirmar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.MarcarEmProcessamento()
        );

        Assert.Equal(
            StatusPedido.CONFIRMADO,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveMarcarPedidoExpiradoEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido(
            expiraEm: DateTime.UtcNow.AddMinutes(-1)
        );

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.MarcarEmProcessamento()
        );

        Assert.Equal(
            StatusPedido.PENDENTE,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveMarcarPedidoCanceladoEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.Cancelar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.MarcarEmProcessamento()
        );

        Assert.Equal(
            StatusPedido.CANCELADO,
            pedido.Status
        );
    }

    #endregion

    #region Confirmacao

    [Fact]
    public void DeveConfirmarPedidoEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();

        // Act
        pedido.Confirmar();

        // Assert
        Assert.Equal(
            StatusPedido.CONFIRMADO,
            pedido.Status
        );

        Assert.True(pedido.EstaConfirmado());
        Assert.False(pedido.EstaEmProcessamento());
    }

    [Fact]
    public void DeveConfirmarPedidoGratuitoAposProcessamento()
    {
        // Arrange
        var pedido = CriarPedido(valorUnitario: 0m);

        pedido.MarcarEmProcessamento();

        // Act
        pedido.Confirmar();

        // Assert
        Assert.True(pedido.IsGratis);
        Assert.True(pedido.EstaConfirmado());
    }

    [Fact]
    public void NaoDeveConfirmarPedidoPendente()
    {
        // Arrange
        var pedido = CriarPedido();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Confirmar()
        );

        Assert.Equal(
            StatusPedido.PENDENTE,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveConfirmarPedidoComPrazoExpirado()
    {
        // Arrange
        var pedido = CriarPedido(
            expiraEm: DateTime.UtcNow.AddMinutes(-1)
        );

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Confirmar()
        );

        Assert.Equal(
            StatusPedido.PENDENTE,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveConfirmarPedidoCancelado()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.Cancelar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Confirmar()
        );

        Assert.Equal(
            StatusPedido.CANCELADO,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveConfirmarPedidoJaConfirmado()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();
        pedido.Confirmar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Confirmar()
        );

        Assert.Equal(
            StatusPedido.CONFIRMADO,
            pedido.Status
        );
    }

    #endregion

    #region Cancelamento

    [Fact]
    public void DeveCancelarPedidoPendenteDentroDoPrazo()
    {
        // Arrange
        var pedido = CriarPedido();

        // Act
        pedido.Cancelar();

        // Assert
        Assert.Equal(
            StatusPedido.CANCELADO,
            pedido.Status
        );

        Assert.True(pedido.EstaCancelado());
        Assert.False(pedido.EstaConfirmado());
    }

    [Fact]
    public void NaoDeveCancelarPedidoEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Cancelar()
        );

        Assert.Equal(
            StatusPedido.EM_PROCESSAMENTO,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveCancelarPedidoConfirmado()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();
        pedido.Confirmar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Cancelar()
        );

        Assert.Equal(
            StatusPedido.CONFIRMADO,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveCancelarPedidoExpirado()
    {
        // Arrange
        var pedido = CriarPedido(
            expiraEm: DateTime.UtcNow.AddMinutes(-1)
        );

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Cancelar()
        );

        Assert.Equal(
            StatusPedido.PENDENTE,
            pedido.Status
        );
    }

    [Fact]
    public void NaoDeveCancelarPedidoJaCancelado()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.Cancelar();

        // Act & Assert
        Assert.Throws<DomainException>(
            () => pedido.Cancelar()
        );

        Assert.Equal(
            StatusPedido.CANCELADO,
            pedido.Status
        );
    }

    #endregion

    #region ConsultasDeDominio

    [Fact]
    public void DeveIdentificarEstadoInicialDoPedido()
    {
        var pedido = CriarPedido();

        Assert.True(pedido.EstaPendente());
        Assert.False(pedido.EstaEmProcessamento());
        Assert.False(pedido.EstaConfirmado());
        Assert.False(pedido.EstaCancelado());
        Assert.False(pedido.EstaExpirado(DateTime.UtcNow));
    }

    [Fact]
    public void PredicadoDeveConsiderarPedidosPendentesValidos()
    {
        var agora = DateTime.UtcNow;

        var pedidos = new[]
        {
            CriarPedido(expiraEm: agora.AddMinutes(1)),
            CriarPedido(expiraEm: agora.AddMinutes(-1)),
            new Pedido(_ofertaId, _compradorId, 1, 10m)
        };

        var comprometidos = pedidos
            .AsQueryable()
            .Where(Pedido.ComprometidosEm(agora))
            .ToList();

        Assert.Equal(2, comprometidos.Count);

        Assert.Contains(pedidos[0], comprometidos);
        Assert.DoesNotContain(pedidos[1], comprometidos);
        Assert.Contains(pedidos[2], comprometidos);
    }

    [Fact]
    public void PredicadoDeveConsiderarPedidosEmProcessamento()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();

        // Simula um instante posterior ao prazo.
        var agora = DateTime.UtcNow.AddHours(1);

        // Act
        var comprometidos = new[] { pedido }
            .AsQueryable()
            .Where(Pedido.ComprometidosEm(agora))
            .ToList();

        // Assert
        Assert.Single(comprometidos);
        Assert.Same(pedido, comprometidos[0]);
    }

    [Fact]
    public void PredicadoDeveConsiderarPedidosConfirmados()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.MarcarEmProcessamento();
        pedido.Confirmar();

        // Act
        var comprometidos = new[] { pedido }
            .AsQueryable()
            .Where(Pedido.ComprometidosEm(DateTime.UtcNow))
            .ToList();

        // Assert
        Assert.Single(comprometidos);
        Assert.Same(pedido, comprometidos[0]);
    }

    [Fact]
    public void PredicadoNaoDeveConsiderarPedidosCancelados()
    {
        // Arrange
        var pedido = CriarPedido();

        pedido.Cancelar();

        // Act
        var comprometidos = new[] { pedido }
            .AsQueryable()
            .Where(Pedido.ComprometidosEm(DateTime.UtcNow))
            .ToList();

        // Assert
        Assert.Empty(comprometidos);
    }

    [Fact]
    public void PredicadoDeveConsiderarSomentePedidosComprometidos()
    {
        // Arrange
        var agora = DateTime.UtcNow;

        var pendente = CriarPedido(
            expiraEm: agora.AddMinutes(10)
        );

        var expirado = CriarPedido(
            expiraEm: agora.AddMinutes(-10)
        );

        var processamento = CriarPedido();
        processamento.MarcarEmProcessamento();

        var confirmado = CriarPedido();
        confirmado.MarcarEmProcessamento();
        confirmado.Confirmar();

        var cancelado = CriarPedido();
        cancelado.Cancelar();

        var pedidos = new[]
        {
            pendente,
            expirado,
            processamento,
            confirmado,
            cancelado
        };

        // Act
        var comprometidos = pedidos
            .AsQueryable()
            .Where(Pedido.ComprometidosEm(agora))
            .ToList();

        // Assert
        Assert.Equal(3, comprometidos.Count);

        Assert.Contains(pendente, comprometidos);
        Assert.Contains(processamento, comprometidos);
        Assert.Contains(confirmado, comprometidos);

        Assert.DoesNotContain(expirado, comprometidos);
        Assert.DoesNotContain(cancelado, comprometidos);
    }

    #endregion
}