namespace Api.Application.DTO.Client;

public sealed record CriarCredencialAcessoRequest
(
    Guid EventoId,
    Guid PedidoId,
    Guid IdIngresso
);