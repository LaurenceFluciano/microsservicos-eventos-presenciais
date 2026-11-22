using Api.Application.DTO.Client;
using Api.Application.Ports;

namespace Api.Infrastructure.Client;

public class HttpPortariaClient : IPortariaClient
{
    public Task CriarCredencialAcesso(CriarCredencialAcessoRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}