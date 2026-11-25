namespace Api.Application.DTO.Client;

public sealed record CriarSessaoPagamentoRequest
(
    Guid PedidoId,
    Guid PagamentoId,
    decimal Valor
);