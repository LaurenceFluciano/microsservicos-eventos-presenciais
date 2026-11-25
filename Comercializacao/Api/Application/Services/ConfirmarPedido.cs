using Api.Application.Events;
using Api.Error;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class ConfirmarPedido
{
    private readonly DataContext _dataContext;

    public ConfirmarPedido(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task Confirmar(
        CredencialGeradaEvent notification,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _dataContext.Pedidos
            .FirstOrDefaultAsync(
                p => p.Id == notification.PedidoId,
                cancellationToken
            );

        if (pedido is null)
            return;

        if (pedido.EstaConfirmado())
            return;

        if (!pedido.EstaEmProcessamento())
            return;

        pedido.Confirmar();

        await _dataContext.SaveChangesAsync(cancellationToken);
    }
}