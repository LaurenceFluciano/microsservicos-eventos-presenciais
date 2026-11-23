namespace Api.Application.DTO.Client;

public sealed record EventoClientResponse(
    Guid Id,
    Guid OrganizadorId,
    string Nome,
    string Status,
    string StatusPublicacao,
    DateTime Inicio,
    DateTime Fim
)
{
    public bool EstaPublicado =>
        string.Equals(
            StatusPublicacao,
            "PUBLICADO",
            StringComparison.OrdinalIgnoreCase);
}