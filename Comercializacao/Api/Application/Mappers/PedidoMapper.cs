using Api.Application.DTO.Pedidos;
using Api.Model;

namespace Api.Application.Mappers;

public class PedidoMapper
{
    public Pedido ToEntity(
        CriarPedidoRequest request,
        OfertaIngresso oferta)
    {
        return new Pedido(
            idOferta: oferta.Id,
            idComprador: request.CompradorId,
            quantidade: request.Quantidade,
            valorUnitario: oferta.Preco
        );
    }


    public CriarPedidoResponse ToCriarPedidoResponse(
        Pedido entity
    )
    {
        return new CriarPedidoResponse(
            PedidoId: entity.Id
        );
    }
}