namespace Api.Application.DTO;

public sealed record DetalheIngressoResponse(
    Guid Id,
    Guid PedidoId,
    Guid EventoId,
    string NomeEvento,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string CodigoIngresso,
    DateTime EmitidoEm,
    string UrlValidacao,
    string QrCodeDataUri
);