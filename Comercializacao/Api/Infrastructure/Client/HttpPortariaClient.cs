using System.Net.Http.Json;
using Api.Application.DTO.Client;
using Api.Application.Events;
using Api.Application.Ports;
using MediatR;

namespace Api.Infrastructure.Client;

public sealed class HttpPortariaClient : IPortariaClient
{
    private readonly HttpClient _httpClient;
    private readonly IPublisher _publisher;
    private readonly ILogger<HttpPortariaClient> _logger;

    public HttpPortariaClient(
        HttpClient httpClient,
        IPublisher publisher,
        ILogger<HttpPortariaClient> logger)
    {
        _httpClient = httpClient;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task CriarCredencialAcesso(
        CriarCredencialAcessoRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Solicitando credencial para pedido {PedidoId}",
            request.PedidoId
        );

        using var response = await _httpClient.PostAsJsonAsync(
            "api/portaria/credencial-acesso",
            request,
            cancellationToken
        );

        _logger.LogInformation(
            "Portaria retornou HTTP {StatusCode}",
            (int)response.StatusCode
        );

        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Publicando CredencialGeradaEvent para {PedidoId}",
            request.PedidoId
        );

        await _publisher.Publish(
            new CredencialGeradaEvent(request.PedidoId),
            cancellationToken
        );

        _logger.LogInformation(
            "CredencialGeradaEvent processado para {PedidoId}",
            request.PedidoId
        );
    }
}