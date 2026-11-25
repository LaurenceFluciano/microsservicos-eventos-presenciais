using System.ComponentModel.DataAnnotations;

namespace Api.Application.DTO;

public sealed record CheckInRequest(
    [Required]
    string CodigoIngresso
);