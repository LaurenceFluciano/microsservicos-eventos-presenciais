
using Api.Application.DTO.Client;
using Api.Application.Ports;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Api.Infrastructure.Payments;

public sealed class StripeGatewayPagamento : IGatewayPagamento
{
    private readonly StripeClient _stripe;
    private readonly string _successUrl;
    private readonly string _cancelUrl;

    public StripeGatewayPagamento(
        StripeClient stripe,
        IConfiguration configuration)
    {
        _stripe = stripe;

        _successUrl = configuration["Stripe:SuccessUrl"]
            ?? throw new InvalidOperationException(
                "Stripe:SuccessUrl não configurada."
            );

        _cancelUrl = configuration["Stripe:CancelUrl"]
            ?? throw new InvalidOperationException(
                "Stripe:CancelUrl não configurada."
            );
    }

    public async Task<SessaoPagamentoResponse> CriarSessaoPagamentoAsync(
        CriarSessaoPagamentoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Valor <= 0m)
            throw new ArgumentException(
                "O valor do checkout deve ser positivo."
            );

        // BRL: R$ 100,00 corresponde a 10000 centavos.
        var valorCentavos = checked((long)(
            request.Valor * 100m
        ));

        var options = new SessionCreateOptions
        {
            Mode = "payment",

            SuccessUrl = _successUrl,
            CancelUrl = _cancelUrl,

            ClientReferenceId = request.PedidoId.ToString(),

            Metadata = new Dictionary<string, string>
            {
                ["pedido_id"] = request.PedidoId.ToString(),
                ["pagamento_id"] = request.PagamentoId.ToString()
            },

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,

                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        UnitAmount = valorCentavos,

                        ProductData =
                            new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Pedido {request.PedidoId}"
                            }
                    }
                }
            }
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey =
                $"checkout-pagamento-{request.PagamentoId}"
        };

        var session = await _stripe.V1.Checkout.Sessions.CreateAsync(
            options,
            requestOptions,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(session.Url))
            throw new InvalidOperationException(
                "Stripe não retornou a URL do checkout."
            );

        return new SessaoPagamentoResponse(
            session.Id,
            session.Url
        );
    }


    public async Task CancelarSessaoPagamentoAsync(
        string idSessaoStripe,
        CancellationToken cancellationToken = default)
    {
        await _stripe.V1.Checkout.Sessions.ExpireAsync(
            idSessaoStripe,
            cancellationToken: cancellationToken
        );
    }

}
