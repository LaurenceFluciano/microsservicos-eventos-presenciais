
using Api.Application.Events;
using Api.Application.Services;
using MediatR;

namespace Api.Application.Listeners;

public class PedidoEventListener : 
    INotificationHandler<PedidoCriadoEvent>,
    INotificationHandler<CredencialGeradaEvent>
{

    private readonly ConfirmarPedido _confirmarPedido;

    private readonly ProcessarPedido _processarPedido;

    public PedidoEventListener(ProcessarPedido processarPedido, ConfirmarPedido confirmarPedido)
    {
        _processarPedido = processarPedido;
        _confirmarPedido = confirmarPedido;
    }

    public async Task Handle(PedidoCriadoEvent notification, CancellationToken cancellationToken)
    {
        await _processarPedido.Processar(notification, cancellationToken);
    }

    public async Task Handle(CredencialGeradaEvent notification, CancellationToken cancellationToken)
    {
        await _confirmarPedido.Confirmar(notification, cancellationToken);
    }
}