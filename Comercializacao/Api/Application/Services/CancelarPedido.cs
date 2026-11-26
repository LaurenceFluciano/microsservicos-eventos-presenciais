using Api.Application.DTO.Pedidos;
using Api.Application.Ports;
using Api.Error;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class CancelarPedido
{
    private readonly DataContext _dataContext;
    private readonly IGatewayPagamento _gatewayPagamento;

    public CancelarPedido(DataContext dataContext, IGatewayPagamento gatewayPagamento)
    {
        _dataContext = dataContext;
        _gatewayPagamento = gatewayPagamento;
    }

    public async Task Cancelar(
        CancelarPedidoRequest request,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _dataContext.Pedidos
            .Include(p => p.Pagamentos)
            .FirstOrDefaultAsync(
                p => p.Id == request.PedidoId,
                cancellationToken
            ) ?? throw new NotFoundException("Pedido não encontrado.");

        if (!pedido.EstaPendente())
            throw new DomainException("Somente pedidos pendentes podem ser cancelados");

        if (pedido.Pagamentos.Any(p =>
            p.Status == StatusPagamento.CONFIRMADO ||
            p.Status == StatusPagamento.ESTORNADO))
        {
            throw new DomainException(
                "O pedido possui pagamentos que impedem seu cancelamento."
            );
        }

        foreach (var pagamento in pedido.Pagamentos)
        {
            if (!string.IsNullOrWhiteSpace(pagamento.IdExterno) && (
                pagamento.Status == StatusPagamento.COBRANCA_GERADA ||
                pagamento.Status == StatusPagamento.RECUSADO
                )
            )
            {
                await _gatewayPagamento.CancelarSessaoPagamentoAsync(
                    pagamento.IdExterno, 
                    cancellationToken
                );

                pagamento.Cancelar();
            }

            if (pagamento.Status == StatusPagamento.PENDENTE)
            {
                pagamento.Cancelar();
            }
        }

        pedido.Cancelar();

        await _dataContext.SaveChangesAsync(cancellationToken);
    }
}
