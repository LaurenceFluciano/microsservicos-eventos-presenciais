namespace Api.Application.DTO;
public class CriarEventoRequest
{
    // Obrigatórios
    public required string Nome { get; init; }
    public required DateTime? Inicio { get; init; }
    public required DateTime? Fim { get; init; }

    // Opcionais
    public string? Descricao { get; init; }
    public string? Local { get; init; }
}