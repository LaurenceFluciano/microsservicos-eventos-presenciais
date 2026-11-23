using System.ComponentModel.DataAnnotations;

namespace Api.Application.DTO.Pedidos;

public record CriarPedidoRequest(
    [Required]
    Guid OfertaId,

    [Required]
    Guid CompradorId,

    [Required]
    int Quantidade
);