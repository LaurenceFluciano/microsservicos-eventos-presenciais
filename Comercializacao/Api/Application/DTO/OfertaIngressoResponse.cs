using Api.Model;


namespace Api.Application.DTO;

public sealed record OfertaIngressoResponse
(
    Guid Id,
    Guid IdEvento,
    string Nome,
    decimal PrecoUnitario,
    int QuantidadeTotal,
    int QuantidadeDisponivel,
    StatusOfertaIngresso Status,
    DateTimeOffset CriadoEm
);