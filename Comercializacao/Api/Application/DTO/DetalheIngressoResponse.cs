namespace Api.Application.DTO;

public sealed record DetalheIngressoResponse(
    Guid Id,
    Guid PedidoId,
    Guid EventoId,
    string NomeEvento,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string CodigoIngresso,
    DateTimeOffset EmitidoEm,
    string UrlValidacao,
    string QrCodeDataUri
);