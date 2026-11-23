
using System.Net.Http.Json;
using Api.Application.DTO.Client;
using Api.Application.Ports;

namespace Api.Infrastructure.Client;

public sealed class HttpPortariaClient : IPortariaClient
{
    private readonly HttpClient _httpClient;

    public HttpPortariaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CriarCredencialAcesso(
        CriarCredencialAcessoRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/portaria/credencial-acesso",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();
    }
}
