using Api.Error;

namespace Api.Model;

public class CredencialAcesso
{
    public Guid Id { get; init; }

    // Referencias a entidades de outros microsservicos; nao sao FKs locais.
    public Guid IngressoId { get; init; }

    public Guid EventoId { get; init; }

    public StatusCredencialAcesso Status { get; set; } = StatusCredencialAcesso.ATIVA;

    public DateTime DataEmissao { get; init; }

    public DateTime? DataUtilizacao { get; set; }

    #region Criar Credencial Acesso

    private CredencialAcesso() {
        
    } 

    public CredencialAcesso(
        Guid ingressoId,
        Guid eventoId
    )
    {
        Id =  Guid.CreateVersion7();
        DataEmissao = DateTime.UtcNow;

        if (ingressoId == Guid.Empty)
            throw new DomainException("O id do ingresso deve estar definido.");

        IngressoId = ingressoId;

        if (eventoId == Guid.Empty)
            throw new DomainException("O id do evento deve estar definido.");

        EventoId = eventoId;

    }

    #endregion 
}

public enum StatusCredencialAcesso
{
    ATIVA,
    UTILIZADA,
    CANCELADA
}
