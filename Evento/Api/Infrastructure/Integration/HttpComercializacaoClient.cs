
using System.Net.Http.Json;
using Api.Application.Ports;

namespace Api.Infrastructure.Integration;

public sealed class HttpComercializacaoClient
    : IComercializacaoClient
{
    private readonly HttpClient _httpClient;

    public HttpComercializacaoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> PossuiOfertaValidaAsync(
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/ofertas/eventos/{eventoId}/possui-oferta-valida",
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var possuiOferta = await response.Content
            .ReadFromJsonAsync<bool>(
                cancellationToken: cancellationToken
            );

        return possuiOferta;
    }
}
