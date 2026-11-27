using System.ComponentModel.DataAnnotations;

namespace Api.Application.DTO.Pedidos;

public record CancelarPedidoRequest(
    [Required]
    Guid PedidoId
);