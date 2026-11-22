using Api.Error;
using Api.Model;

namespace Tests.Model;

public class OfertaIngressoTest
{
    private readonly Guid _idEvento = Guid.CreateVersion7();
    private readonly Guid _idOrganizador = Guid.CreateVersion7();

    // ========================================
    // CENÁRIO 1 – CRIAÇÃO COM DADOS VÁLIDOS
    // ========================================

    [Fact]
    public void DeveCriarOfertaComDadosValidos()
    {
        // Arrange
        var nome = "Ingresso VIP";
        var preco = 150m;
        var quantidade = 100;

        // Act
        var oferta = new OfertaIngresso(
            nome,
            preco,
            quantidade,
            _idEvento,
            _idOrganizador
        );

        // Assert
        Assert.NotEqual(Guid.Empty, oferta.Id);
        Assert.Equal(_idEvento, oferta.IdEvento);
        Assert.Equal(_idOrganizador, oferta.IdOrganizador);
        Assert.Equal(nome, oferta.Nome);
        Assert.Equal(preco, oferta.Preco);
        Assert.Equal(quantidade, oferta.QuantidadeTotal);
        Assert.Equal(StatusOfertaIngresso.ATIVA, oferta.Status);
        Assert.False(oferta.IsGratis);
        Assert.NotEqual(default(DateTimeOffset), oferta.CriadoEm);
    }

    // ========================================
    // CENÁRIO 2 – OFERTA GRATUITA
    // ========================================

    [Fact]
    public void DevePermitirCriacaoDeOfertaGratuita()
    {
        // Act
        var oferta = new OfertaIngresso(
            "Ingresso gratuito",
            0m,
            100,
            _idEvento,
            _idOrganizador
        );

        // Assert
        Assert.True(oferta.IsGratis);
        Assert.Equal(0m, oferta.Preco);
        Assert.Equal(StatusOfertaIngresso.ATIVA, oferta.Status);
    }

    // ========================================
    // CENÁRIO 3 – PREÇO INVÁLIDO
    // ========================================

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NaoDeveCriarOfertaComPrecoNegativo(decimal preco)
    {
        // Arrange and Act
        var exception = Assert.Throws<DomainException>(() =>
            new OfertaIngresso(
                "Ingresso VIP",
                preco,
                100,
                _idEvento,
                _idOrganizador
            )
        );

        // Assert
        Assert.Equal(
            "O preço unitário deve ser maior ou igual a zero.",
            exception.Message
        );
    }

    // ========================================
    // CENÁRIO 4 – QUANTIDADE INVÁLIDA
    // ========================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NaoDeveCriarOfertaComQuantidadeInvalida(int quantidade)
    {
        // Act
        var exception = Assert.Throws<DomainException>(() =>
            new OfertaIngresso(
                "Ingresso VIP",
                150m,
                quantidade,
                _idEvento,
                _idOrganizador
            )
        );

        // Assert
        Assert.Equal(
            "A quantidade total de ingressos deve ser maior que zero.",
            exception.Message
        );
    }

    // ========================================
    // CENÁRIO 5 – NOME INVÁLIDO
    // ========================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    [InlineData("\t\n")]
    public void NaoDeveCriarOfertaComNomeInvalido(string? nome)
    {
        // Act
        var exception = Assert.Throws<DomainException>(() =>
            new OfertaIngresso(
                nome!,
                150m,
                100,
                _idEvento,
                _idOrganizador
            )
        );

        // Assert
        Assert.Equal(
            "O nome da oferta é obrigatório.",
            exception.Message
        );
    }

    // ========================================
    // NORMALIZAÇÃO DO NOME
    // ========================================

    [Fact]
    public void DeveNormalizarNomeDaOferta()
    {
        // Act
        var oferta = new OfertaIngresso(
            "   Ingresso VIP   ",
            150m,
            100,
            _idEvento,
            _idOrganizador
        );

        // Assert
        Assert.Equal("Ingresso VIP", oferta.Nome);
    }

    // ========================================
    // RESTRIÇÕES DE TRANSIÇÃO DE ESTADO
    // ========================================

    [Fact]
    public void NaoDeveEncerrarOfertaEsgotada()
    {
        // Arrange
        var oferta = CriarOfertaValida();
        oferta.Esgotado();

        // Act
        var exception = Assert.Throws<DomainException>(
            () => oferta.Encerrar()
        );

        // Assert
        Assert.Equal(
            "Os ingressos esgotados da oferta não podem ser encerrados.",
            exception.Message
        );

        Assert.Equal(StatusOfertaIngresso.ESGOTADO, oferta.Status);
    }

    [Fact]
    public void NaoDeveEsgotarOfertaEncerrada()
    {
        // Arrange
        var oferta = CriarOfertaValida();
        oferta.Encerrar();

        // Act
        var exception = Assert.Throws<DomainException>(
            () => oferta.Esgotado()
        );

        // Assert
        Assert.Equal(
            "Ofertas encerradas não podem ser esgotadas!",
            exception.Message
        );

        Assert.Equal(StatusOfertaIngresso.ENCERRADA, oferta.Status);
    }

    // ========================================
    // MÉTODOS AUXILIARES
    // ========================================

    private OfertaIngresso CriarOfertaValida()
    {
        return new OfertaIngresso(
            "Ingresso VIP",
            150m,
            100,
            _idEvento,
            _idOrganizador
        );
    }
}