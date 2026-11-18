using Api.Model;

namespace Api.Application.DTO;

public class DetalheEventoResponse
{
    // Identificação
    public Guid Id { get; init; }
    public Guid OrganizadorId { get; init; }

    // Informações gerais
    public string Nome { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public string Local { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;

    // Período
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }

    // Estados
    public EnumStatusPublicacaoEvento StatusPublicacao { get; init; }
    public EnumStatusEvento Status { get; init; }

    // Datas de controle
    public DateTimeOffset CriadoEm { get; init; }
    public DateTime? PublicadoEm { get; init; }
    public DateTime? PublicacaoEncerradaEm { get; init; }
}
