namespace Api.Application.DTO;

public sealed record IngressoDigitalResponse(
    Guid Id,
    string CodigoIngresso,
    string UrlConfirmacao,
    string QrCodeDataUri
);