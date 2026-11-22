namespace Api.Application.DTO.Pedidos;

public record CriarPedidoRequest(
    Guid OfertaId,
    Guid CompradorId,
    int Quantidade
);