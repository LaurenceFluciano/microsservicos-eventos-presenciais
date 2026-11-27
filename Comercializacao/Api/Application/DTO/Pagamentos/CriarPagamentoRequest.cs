namespace Api.Application.DTO.Pagamentos;

public record CriarPagamentoRequest(
    Guid PedidoId
);