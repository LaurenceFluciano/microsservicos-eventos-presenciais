using System.Net;
using System.Net.Http.Json;
using Api.Application.DTO.Client;
using Api.Application.Ports;
using Api.Error;

namespace Api.Infrastructure.Client;

public sealed class HttpUsuarioClient : IUsuarioClient
{
    private readonly HttpClient _httpClient;

    public HttpUsuarioClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UsuarioClientResponse> ObterPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = email.Trim().ToLowerInvariant();

        var emailEncoded = Uri.EscapeDataString(emailNormalizado);

        using var response = await _httpClient.GetAsync(
            $"api/users?email={emailEncoded}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(
                "Comprador não encontrado."
            );
        }

        response.EnsureSuccessStatusCode();

        var usuario = await response.Content
            .ReadFromJsonAsync<UsuarioClientResponse>(
                cancellationToken: cancellationToken
            );

        return usuario ?? throw new InvalidOperationException(
            "O microsserviço Usuários retornou uma resposta inválida."
        );
    }
}