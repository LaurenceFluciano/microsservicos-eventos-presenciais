

using Api.Application.DTO.Client;

namespace Api.Application.Ports;


public interface IGatewayPagamento
{
    Task<SessaoPagamentoResponse> CriarSessaoPagamentoAsync(CriarSessaoPagamentoRequest request, CancellationToken cancellationToken = default);
}