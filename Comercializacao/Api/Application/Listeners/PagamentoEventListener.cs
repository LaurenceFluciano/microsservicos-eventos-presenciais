
using Api.Application.Events;
using Api.Application.Services;
using MediatR;

namespace Api.Application.Listeners;

public sealed class PagamentoEventListener :
    INotificationHandler<PagamentoCriadoEvent>,
    INotificationHandler<CobrancaGeradaEvent>,
    INotificationHandler<PagamentoAprovadoEvent>,
    INotificationHandler<PagamentoRecusadoEvent>,
    INotificationHandler<PagamentoConfirmadoEvent>
{
    private readonly PagamentoService _pagamentoService;
    private readonly ProcessarPedido _processarPedido;

    public PagamentoEventListener(
        PagamentoService pagamentoService,
        ProcessarPedido processarPedido)
    {
        _pagamentoService = pagamentoService;
        _processarPedido = processarPedido;
    }

    public Task Handle(
        PagamentoCriadoEvent notification,
        CancellationToken cancellationToken)
    {
        return _pagamentoService.GerarCobranca(
            notification.PagamentoId,
            cancellationToken);
    }

    public Task Handle(
        CobrancaGeradaEvent notification,
        CancellationToken cancellationToken)
    {
        return _pagamentoService.RegistrarCobrancaGerada(
            notification,
            cancellationToken);
    }

    public Task Handle(
        PagamentoAprovadoEvent notification,
        CancellationToken cancellationToken)
    {
        return _pagamentoService.RegistrarAprovacao(
            notification,
            cancellationToken);
    }

    public Task Handle(
        PagamentoRecusadoEvent notification,
        CancellationToken cancellationToken)
    {
        return _pagamentoService.RegistrarRecusa(
            notification,
            cancellationToken);
    }

    public Task Handle(
        PagamentoConfirmadoEvent notification,
        CancellationToken cancellationToken)
    {
        return _processarPedido.Processar(
            new PedidoCriadoEvent(notification.PedidoId),
            cancellationToken);
    }
}
