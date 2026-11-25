namespace Api.Application.DTO.Client;

public sealed record IngressoClientResponse(
    Guid Id,
    Guid PedidoId,
    Guid EventoId,
    Guid OrganizadorId,
    string NomeEvento,
    DateTime Inicio,
    DateTime Fim,
    string CodigoIngresso,
    DateTime EmitidoEm
);