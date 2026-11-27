
using Api.Application.DTO.Client;

namespace Api.Application.Ports;

public interface IPortariaClient
{
    Task CriarCredencialAcesso(CriarCredencialAcessoRequest request, CancellationToken cancellationToken);
}