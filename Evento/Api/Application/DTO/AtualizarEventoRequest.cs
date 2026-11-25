namespace Api.Application.DTO;

// Todos os campos são opcionais: PATCH não deve apagar os demais valores.
public class AtualizarEventoRequest
{
    public string? Nome { get; init; }
    public string? Descricao { get; init; }
    public string? Local { get; init; }
    public DateTime? Inicio { get; init; }
    public DateTime? Fim { get; init; }
}
