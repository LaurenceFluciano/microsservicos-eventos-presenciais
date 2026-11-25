
using Api.Error;
using Api.Infrastructure.Authentication;
using Api.Model;

namespace Tests.UnitTest;

public class EventoTest
{
    /*
     * Cadastro realizado com sucesso:
     *
     * Dado que o organizador está autenticado e informou dados válidos,
     * Quando solicitar o cadastro do evento,
     * Então o sistema deverá registrar o evento, associá-lo
     * ao organizador autenticado e retornar sua identificação.
     */
    [Fact(DisplayName = "Evento: deve criar com dados válidos")]
    public void Criar_ComDadosValidos_DeveCriarEvento()
    {
        // Arrange
        var usuarioAtual = new FakeUsuarioAtual();
        var organizadorId = usuarioAtual.GetId();
        var nome = "Evento exemplo";
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddHours(2);

        // Act
        var evento = new Evento(
            organizadorId,
            nome,
            inicio,
            fim
        );

        // Assert
        Assert.NotEqual(Guid.Empty, evento.Id);
        Assert.Equal(nome, evento.Nome);
        Assert.Equal(organizadorId, evento.OrganizadorId);
        Assert.Equal(inicio, evento.Inicio);
        Assert.Equal(fim, evento.Fim);
    }

    /*
     * Estado inicial do evento:
     *
     * Dado que um evento foi cadastrado com sucesso,
     * Quando suas informações forem consultadas,
     * Então seu estado de publicação deverá ser RASCUNHO.
     */
    [Fact(DisplayName = "Evento: deve iniciar como rascunho")]
    public void CriarEvento_ComDadosValidos_DeveIniciarComoRascunho()
    {
        // Arrange
        var organizadorId = Guid.NewGuid();
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddHours(2);

        // Act
        var evento = new Evento(
            organizadorId,
            "Evento exemplo",
            inicio,
            fim
        );

        // Assert
        Assert.Equal(
            EnumStatusPublicacaoEvento.RASCUNHO,
            evento.GetStatusPublicacao()
        );
    }

    /*
     * Dados obrigatórios:
     *
     * Dado que o organizador não informou um ou mais campos obrigatórios,
     * Quando solicitar o cadastro,
     * Então o sistema deverá recusar a operação
     * e informar os campos inválidos.
     */
    [Theory(DisplayName = "Evento: deve rejeitar nome inválido")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarEvento_ComNomeInvalido_DeveRejeitar(string? nome)
    {
        // Arrange
        var inicio = DateTime.UtcNow;

        // Act
        var exception = Assert.Throws<DomainException>(
            () => new Evento(
                Guid.NewGuid(),
                nome!,
                inicio,
                inicio.AddHours(2)
            )
        );

        // Assert
        Assert.Equal(
            "O nome do evento é obrigatório.",
            exception.Message
        );
    }

    [Fact(DisplayName = "Evento: deve recusar cadastro sem organizador")]
    public void CriarEvento_SemOrganizador_DeveLancarExcecao()
    {
        // Arrange
        var organizadorId = Guid.Empty;
        var nome = "Evento exemplo";
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddHours(2);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => new Evento(
                organizadorId,
                nome,
                inicio,
                fim
            )
        );

        // Assert
        Assert.Equal(
            "O organizador do evento é obrigatório.",
            exception.Message
        );
    }

    /*
     * Período obrigatório:
     *
     * Dado que somente a data e horário de término é informada,
     * Quando o organizador solicitar o cadastro,
     * Então o sistema deverá recusar a operação
     * e informar que o período deve ser definido.
     */
    [Fact(DisplayName = "Evento: deve recusar cadastro sem data de início")]
    public void CriarEvento_SemInicio_DeveLancarExcecao()
    {
        // Arrange
        var organizadorId = Guid.NewGuid();
        var nome = "Evento exemplo";
        var inicio = default(DateTime);
        var fim = DateTime.UtcNow.AddHours(2);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => new Evento(
                organizadorId,
                nome,
                inicio,
                fim
            )
        );

        // Assert
        Assert.Equal(
            "O evento deve ter um período de início e término definido.",
            exception.Message
        );
    }

    /*
     * Período obrigatório:
     *
     * Dado que somente a data e horário de início é informada,
     * Quando o organizador solicitar o cadastro,
     * Então o sistema deverá recusar a operação
     * e informar que o período deve ser definido.
     */
    [Fact(DisplayName = "Evento: deve recusar cadastro sem data de término")]
    public void CriarEvento_SemFim_DeveLancarExcecao()
    {
        // Arrange
        var organizadorId = Guid.NewGuid();
        var nome = "Evento exemplo";
        var inicio = DateTime.UtcNow;
        var fim = default(DateTime);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => new Evento(
                organizadorId,
                nome,
                inicio,
                fim
            )
        );

        // Assert
        Assert.Equal(
            "O evento deve ter um período de início e término definido.",
            exception.Message
        );
    }

    /*
     * Período inválido:
     *
     * Dado que a data e horário de término é anterior
     * ou igual à data e horário de início,
     * Quando o organizador solicitar o cadastro,
     * Então o sistema deverá recusar a operação
     * e informar a inconsistência.
     */
    [Theory(DisplayName = "Evento: deve recusar cadastro com período inválido")]
    [InlineData(0)]   // Término igual ao início
    [InlineData(-1)]  // Término uma hora antes
    [InlineData(-24)] // Término um dia antes
    [InlineData(-44)] // Término 44 horas antes
    public void CriarEvento_ComPeriodoInvalido_DeveLancarExcecao(
        int diferencaHoras)
    {
        // Arrange
        var organizadorId = Guid.NewGuid();
        var nome = "Evento exemplo";
        var inicio = DateTime.UtcNow;
        var fim = inicio.AddHours(diferencaHoras);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => new Evento(
                organizadorId,
                nome,
                inicio,
                fim
            )
        );

        // Assert
        Assert.Equal(
            "A data de término deve ser posterior à data de início.",
            exception.Message
        );
    }



    [Theory(DisplayName = "Evento: deve formatar o slug público")]
    [InlineData(1, "evento-musical")]
    [InlineData(2, "evento-musical-2")]
    [InlineData(3, "evento-musical-3")]
    public void DefinirNumeroSlug_DeveFormatarSlugPublico(
        int numero,
        string esperado)
    {
        // Arrange
        var inicio = DateTime.UtcNow;

        var evento = new Evento(
            Guid.NewGuid(),
            "Evento Musical",
            inicio,
            inicio.AddHours(2)
        );

        // Act
        evento.DefinirNumeroSlug(numero);

        // Assert
        Assert.Equal(esperado, evento.SlugPublico);
    }

    [Fact(DisplayName = "Evento: deve rejeitar número de slug inválido")]
    public void DefinirNumeroSlug_ComNumeroInvalido_DeveRejeitar()
    {
        // Arrange
        var inicio = DateTime.UtcNow;

        var evento = new Evento(
            Guid.NewGuid(),
            "Evento Musical",
            inicio,
            inicio.AddHours(2)
        );

        // Act e Assert
        Assert.Throws<DomainException>(
            () => evento.DefinirNumeroSlug(0)
        );
    }
}
