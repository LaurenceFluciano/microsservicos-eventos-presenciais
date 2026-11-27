namespace Api.Application.DTO;

public sealed record IngressoValidacaoResponse(
    Guid Id,
    Guid PedidoId,
    Guid EventoId,
    Guid OrganizadorId,
    string NomeEvento,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string CodigoIngresso,
    DateTime EmitidoEm
);