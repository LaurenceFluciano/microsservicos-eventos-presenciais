namespace Api.Application.DTO;

public record IngressoResponse(
    Guid Id,
    string CodigoIngresso,
    DateTimeOffset EmitidoEm
);