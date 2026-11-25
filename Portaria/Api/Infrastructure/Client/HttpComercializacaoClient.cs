
using System.Net;
using System.Net.Http.Json;
using Api.Application.Ports;
using Api.Application.DTO.Client;
using Api.Error;

namespace Api.Infrastructure.Client;

public sealed class HttpComercializacaoClient : IComercializacaoClient
{
    private readonly HttpClient _httpClient;

    public HttpComercializacaoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IngressoClientResponse> ObterIngressoPorCodigo(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/ingressos/codigo/{Uri.EscapeDataString(codigo)}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(
                "Ingresso não encontrado.");
        }

        response.EnsureSuccessStatusCode();

        var ingresso = await response.Content
            .ReadFromJsonAsync<IngressoClientResponse>(
                cancellationToken: cancellationToken
            );

        return ingresso ?? throw new InvalidOperationException(
            "Resposta inválida do serviço de Comercialização.");
    }
}
