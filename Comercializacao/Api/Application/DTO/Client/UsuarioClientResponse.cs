namespace Api.Application.DTO.Client;

public record UsuarioClientResponse(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone
);