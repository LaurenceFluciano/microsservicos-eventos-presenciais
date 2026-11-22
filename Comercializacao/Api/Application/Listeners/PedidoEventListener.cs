
using Api.Application.Events;
using Api.Application.Services;
using MediatR;

namespace Api.Application.Listeners;

public class PedidoEventListener : 
    INotificationHandler<PedidoCriadoEvent>,
    INotificationHandler<PagamentoConfirmadoEvent>
{

    private readonly ProcessarPedido _processarPedido;

    public PedidoEventListener(ProcessarPedido processarPedido)
    {
        _processarPedido = processarPedido;
    }

    public async Task Handle(PedidoCriadoEvent notification, CancellationToken cancellationToken)
    {
        await _processarPedido.Processar(notification, cancellationToken);
    }

    public async Task Handle(PagamentoConfirmadoEvent notification, CancellationToken cancellationToken)
    {
        await _processarPedido.Processar(notification, cancellationToken);
    }
}