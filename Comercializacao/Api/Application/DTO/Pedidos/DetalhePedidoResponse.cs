using Api.Application.DTO;
using Api.Application.DTO.Pagamentos;
using Api.Model;

namespace Api.Application.DTO.Pedidos;

public record DetalhePedidoResponse(
    Guid EventoId,
    Guid PedidoId,
    Guid OfertaId,
    int Quantidade,
    StatusPedido Status,
    DateTimeOffset ExpiraEm,
    PagamentoResponse? Pagamento,
    List<IngressoResponse> Ingressos
);