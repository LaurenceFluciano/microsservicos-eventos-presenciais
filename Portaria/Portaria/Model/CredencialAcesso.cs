namespace AppTemplate.Model;

public class CredencialAcesso
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // Referencias a entidades de outros microsservicos; nao sao FKs locais.
    public Guid IngressoId { get; set; }

    public Guid EventoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public StatusCredencialAcesso Status { get; set; } = StatusCredencialAcesso.ATIVA;

    public DateTimeOffset DataEmissao { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DataUtilizacao { get; set; }
}

public enum StatusCredencialAcesso
{
    ATIVA,
    UTILIZADA,
    CANCELADA
}
