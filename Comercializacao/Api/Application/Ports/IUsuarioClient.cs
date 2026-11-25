
using Api.Application.DTO.Client;

namespace Api.Application.Ports;

public interface IUsuarioClient
{
    Task<UsuarioClientResponse> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
}