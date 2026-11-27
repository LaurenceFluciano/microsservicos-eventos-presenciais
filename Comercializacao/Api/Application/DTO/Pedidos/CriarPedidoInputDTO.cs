using System.ComponentModel.DataAnnotations;

namespace Api.Application.DTO.Pedidos;

public record CriarPedidoInputDTO(
    [Required]
    Guid OfertaId,

    [Required]
    [EmailAddress]
    string EmailComprador,

    [Range(1, int.MaxValue)]
    int Quantidade
);