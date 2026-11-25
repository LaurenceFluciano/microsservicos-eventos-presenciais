namespace Api.Application.DTO;

public sealed record AtualizarOfertaIngressoRequest(
    string Nome,
    decimal PrecoUnitario,
    int QuantidadeTotal
);