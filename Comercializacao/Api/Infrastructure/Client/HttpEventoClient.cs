
using System.Net;
using System.Net.Http.Json;
using Api.Application.DTO.Client;
using Api.Application.Ports;
using Api.Error;

namespace Api.Infrastructure.Client;

public sealed class HttpEventoClient : IEventoClient
{
    private readonly HttpClient _httpClient;

    public HttpEventoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventoClientResponse> ObterEventoAsync(
        Guid idEvento,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/eventos/{idEvento}/integracao",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(
                "Evento não encontrado.");
        }

        response.EnsureSuccessStatusCode();

        var evento = await response.Content
            .ReadFromJsonAsync<EventoClientResponse>(
                cancellationToken: cancellationToken
            );

        return evento ?? throw new InvalidOperationException(
            "Resposta inválida do serviço de Eventos.");
    }
}
