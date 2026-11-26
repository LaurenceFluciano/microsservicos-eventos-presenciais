

using Api.Application.DTO.Client;

namespace Api.Application.Ports;


// ATENÇÃO

// Na atual implementação adapatamos uma interface apenas para o caso do stripe
// Para interfaces mais gerais de Gateway de pagamento deve-se considerar um estudo mais aprofundado

public interface IGatewayPagamento
{
    Task<SessaoPagamentoResponse> CriarSessaoPagamentoAsync(CriarSessaoPagamentoRequest request, CancellationToken cancellationToken = default);
    Task CancelarSessaoPagamentoAsync(string idSessaoStripe, CancellationToken cancellationToken = default);
    
}