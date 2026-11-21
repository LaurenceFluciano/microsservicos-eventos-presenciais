using Api.Model;

namespace Api.Application.DTO;

// Dados resumidos retornados na lista de eventos do organizador.
public sealed class ResumoEventoResponse
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Local { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
    public EnumStatusEvento Status { get; init; }
    public EnumStatusPublicacaoEvento StatusPublicacao { get; init; }
    public string? UrlEvento { get; init; }
}
