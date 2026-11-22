namespace Api.Application.DTO.Client;

public sealed record SessaoPagamentoResponse
(
    string IdSessaoStripe,
    string UrlCheckout
);