
namespace Evento.Model;

public class Evento
{
    public Guid Id { get; set; }
    public Guid OrganizadorId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;

    public DateTimeOffset DataTempoInicio { get; set; }
    public DateTimeOffset DataTempoFim { get; set; }

    public string Slug { get; set; } = string.Empty;

    public DateTimeOffset? PublicadoEm { get; set; }
    public DateTimeOffset? PublicacaoEncerradaEm { get; set; }

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    public EnumStatusPublicacaoEvento GetStatusPublicacao()
    {
        return PublicadoEm.HasValue
            ? EnumStatusPublicacaoEvento.PUBLICADO
            : EnumStatusPublicacaoEvento.RASCUNHO;
    }

    public EnumStatusEvento GetStatus()
    {
        var agora = DateTimeOffset.UtcNow;

        if (agora < DataTempoInicio)
            return EnumStatusEvento.AGENDADO;

        if (agora < DataTempoFim)
            return EnumStatusEvento.EM_ANDAMENTO;

        return EnumStatusEvento.ENCERRADO;
    }
}

public enum EnumStatusPublicacaoEvento
{
    RASCUNHO,
    PUBLICADO
}

public enum EnumStatusEvento
{
    AGENDADO,
    EM_ANDAMENTO,
    ENCERRADO
}
