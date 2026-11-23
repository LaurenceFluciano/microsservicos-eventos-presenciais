namespace Api.Application.DTO;

public sealed record ConsultarOfertasEventoResponse(
    Guid IdEvento,
    string NomeEvento,
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<OfertaIngressoResponse> Ofertas
);