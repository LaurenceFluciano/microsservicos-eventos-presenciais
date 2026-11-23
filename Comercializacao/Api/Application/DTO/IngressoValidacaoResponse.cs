namespace Api.Application.DTO;

public sealed record IngressoValidacaoResponse(
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