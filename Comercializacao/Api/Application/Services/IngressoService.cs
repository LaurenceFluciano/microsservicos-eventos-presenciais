
using Api.Application.DTO;
using Api.Application.Events;
using Api.Error;
using Api.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class IngressoService
{
    private readonly DataContext _dataContext;

    private readonly IPublisher _publisher; 

    public IngressoService(DataContext dataContext, IPublisher publisher)
    {
        _dataContext = dataContext;
        _publisher = publisher;
    }

    public async Task EmitirIngressos(
        CriarIngressoRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var pedido = await _dataContext.Pedidos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == request.IdPedido,
                cancellationToken
            )
            ?? throw new NotFoundException(
                "Pedido não encontrado."
            );

        await EmitirIngressos(pedido, cancellationToken);
    }

    public async Task EmitirIngressos(
        Pedido pedido,
        CancellationToken cancellationToken = default)
    {
     
        if (pedido.Status != StatusPedido.EM_PROCESSAMENTO)
            return;

        var ingressos = await _dataContext.Ingressos
            .Where(i => i.PedidoId == pedido.Id)
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);

        if (ingressos.Count > pedido.Quantidade)
            return;

        var quantidadeRestante =
            pedido.Quantidade - ingressos.Count;

        for (int i = 0; i < quantidadeRestante; i++)
        {
            var ingresso = new Ingresso
            {
                PedidoId = pedido.Id,
                CodigoIngresso = Guid.NewGuid().ToString("N"),
                EmitidoEm = DateTime.UtcNow
            };

            _dataContext.Ingressos.Add(ingresso);
            ingressos.Add(ingresso);
        }

        if (quantidadeRestante > 0)
            await _dataContext.SaveChangesAsync(
                cancellationToken
            );

        var idEvento = await _dataContext.OfertasIngresso
            .Where(o => o.Id == pedido.OfertaId)
            .Select(o => o.IdEvento)
            .FirstAsync(cancellationToken);
        
        await _publisher.Publish(
            new IngressosGeradosEvent(
                EventoId: idEvento,
                PedidoId: pedido.Id,
                IngressoIds: ingressos
                    .Select(i => i.Id)
                    .ToArray()
            ),
            cancellationToken
        );
        
    }
}
