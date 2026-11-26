using Api.Application.Ports;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class ExpirarPedido
{
    private readonly DataContext _dataContext;
    private readonly IGatewayPagamento _gatewayPagamento;

    public ExpirarPedido(
        DataContext dataContext,
        IGatewayPagamento gatewayPagamento)
    {
        _dataContext = dataContext;
        _gatewayPagamento = gatewayPagamento;
    }

    public async Task<bool> Expirar(
        Guid pedidoId,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTimeOffset.UtcNow;

        var pedido = await _dataContext.Pedidos
            .Include(p => p.Pagamentos)
            .FirstOrDefaultAsync(
                p => p.Id == pedidoId,
                cancellationToken
            );

        if (pedido is null ||
            !pedido.EstaPendente() ||
            !pedido.EstaExpirado(agora))
        {
            return false;
        }

        if (pedido.Pagamentos.Any(p =>
            p.Status == StatusPagamento.CONFIRMADO ||
            p.Status == StatusPagamento.ESTORNADO))
        {
            return false;
        }

        foreach (var pagamento in pedido.Pagamentos)
        {
            if (pagamento.Status == StatusPagamento.CANCELADO)
                continue;

            if (!string.IsNullOrWhiteSpace(pagamento.IdExterno))
            {
                await _gatewayPagamento.CancelarSessaoPagamentoAsync(
                    pagamento.IdExterno,
                    cancellationToken
                );
            }

            pagamento.Cancelar();
        }

        pedido.Expirar(agora);

        await _dataContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}