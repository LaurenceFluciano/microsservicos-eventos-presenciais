namespace Api.Application.DTO;

public sealed record DetalheIngressoResponse(
    Guid Id,
    Guid PedidoId,
    Guid EventoId,
    string NomeEvento,
    DateTime Inicio,
    DateTime Fim,
    string CodigoIngresso,
    DateTime EmitidoEm,
    string UrlValidacao,
    string QrCodeDataUri
);