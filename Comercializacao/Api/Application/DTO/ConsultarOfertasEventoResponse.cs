namespace Api.Application.DTO;

public sealed record ConsultarOfertasEventoResponse(
    Guid IdEvento,
    string NomeEvento,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    IReadOnlyList<OfertaIngressoResponse> Ofertas
);