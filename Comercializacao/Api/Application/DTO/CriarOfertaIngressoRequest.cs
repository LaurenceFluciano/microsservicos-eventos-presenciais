namespace Api.Application.DTO;

public sealed record CriarOfertaIngressoRequest(
    Guid IdEvento,
    string Nome,
    decimal PrecoUnitario,
    int QuantidadeTotal
);