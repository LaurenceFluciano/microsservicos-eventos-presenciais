
using Api.Application.DTO.Client;

namespace Api.Application.Ports;

public interface IComercializacaoClient
{
    Task<IngressoClientResponse> ObterIngressoPorCodigo(
        string Codigo, 
        CancellationToken cancellationToken = default
    );
}