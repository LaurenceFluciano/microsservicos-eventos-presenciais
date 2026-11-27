using System.ComponentModel.DataAnnotations;

namespace Api.Application.DTO;

public sealed record CriarCredencialAcessoRequest
(
    [Required]
    Guid EventoId,

    [Required]
    Guid IdIngresso
);