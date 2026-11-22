
using Api.Application.Events;
using Api.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Api.Controllers;

[ApiController]
[Route("api/webhooks/pagamentos")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly DataContext _dataContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    private readonly IPublisher _publisher;

    public StripeWebhookController(
        DataContext dataContext,
        IConfiguration configuration,
        IPublisher publisher,
        ILogger<StripeWebhookController> logger)
    {
        _dataContext = dataContext;
        _configuration = configuration;
        _logger = logger;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> Receber(
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);

        var json = await reader.ReadToEndAsync(
            cancellationToken);

        var signature = Request.Headers["Stripe-Signature"]
            .ToString();

        var secret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(503);

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                secret
            );
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(
                ex,
                "Assinatura do webhook Stripe inválida.");

            return BadRequest();
        }
        catch (Exception ex)
            when (ex is ArgumentException ||
                  ex is System.Text.Json.JsonException)
        {
            return BadRequest();
        }

        var relevante =
            stripeEvent.Type == "checkout.session.completed" ||
            stripeEvent.Type == "checkout.session.async_payment_succeeded" ||
            stripeEvent.Type == "checkout.session.async_payment_failed";

        if (!relevante)
            return Ok();

        if (stripeEvent.Data.Object is not Session sessao)
            return BadRequest();

        var aprovado =
            stripeEvent.Type ==
                "checkout.session.async_payment_succeeded" ||
            (
                stripeEvent.Type == "checkout.session.completed" &&
                sessao.PaymentStatus == "paid"
            );

        var recusado =
            stripeEvent.Type ==
                "checkout.session.async_payment_failed";

        if (!aprovado && !recusado)
            return Ok();

        var pagamento = await _dataContext.Pagamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.IdExterno == sessao.Id,
                cancellationToken);

        if (pagamento is null)
        {
            _logger.LogWarning(
                "Pagamento não encontrado para sessão {SessionId}",
                sessao.Id);


            return StatusCode(503);
        }

        if (aprovado)
        {
            await _publisher.Publish(
                new PagamentoAprovadoEvent(
                    pagamento.Id,
                    sessao.Id
                ),
                cancellationToken
            );
        }
        else
        {
            await _publisher.Publish(
                new PagamentoRecusadoEvent(
                    pagamento.Id,
                    sessao.Id
                ),
                cancellationToken);
        }

        return Accepted();
    }
}
