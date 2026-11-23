using Api.Model;

namespace Api.Application.DTO;

public sealed record EventoIntegracaoResponse(
    Guid Id,
    Guid OrganizadorId,
    string Nome,
    EnumStatusEvento Status,
    EnumStatusPublicacaoEvento StatusPublicacao,
    DateTime Inicio,
    DateTime Fim
);