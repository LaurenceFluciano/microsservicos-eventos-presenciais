
using Api.Application.DTO.Pagamentos;
using Api.Application.DTO.Client;
using Api.Application.Events;
using Api.Application.Ports;
using Api.Model;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Api.Application.Services;

public class PagamentoService
{
    private readonly DataContext _dataContext;
    private readonly IGatewayPagamento _gatewayPagamento;

    private readonly IPublisher _publisher;


    public PagamentoService(
        DataContext dataContext,
        IGatewayPagamento gatewayPagamento,
        IPublisher publisher
        )
    {
        _dataContext = dataContext;
        _gatewayPagamento = gatewayPagamento;
        _publisher = publisher;

    }

    public async Task Criar(
        CriarPagamentoRequest request,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _dataContext.Pedidos
            .FirstOrDefaultAsync(
                p => p.Id == request.PedidoId,
                cancellationToken);

        if (pedido is null ||
                !pedido.EstaPendente() ||
                pedido.EstaExpirado(DateTimeOffset.UtcNow) ||
                pedido.IsGratis
                )
            return;
            

        var pagamentoExistente = await _dataContext.Pagamentos
            .FirstOrDefaultAsync(
                p => p.PedidoId == pedido.Id,
                cancellationToken);

        if (pagamentoExistente is not null)
            return;

        var pagamento = new Pagamento(
            pedido.Id,
            pedido.ValorTotal()
        );

        _dataContext.Pagamentos.Add(pagamento);

        await _dataContext.SaveChangesAsync(
            cancellationToken);

        await _publisher.Publish(
            new PagamentoCriadoEvent(pagamento.Id),
            cancellationToken
        );
    }

    public async Task GerarCobranca(
        Guid pagamentoId,
        CancellationToken cancellationToken = default)
    {
        var pagamento = await _dataContext.Pagamentos
            .Include(p => p.Pedido)
            .FirstOrDefaultAsync(
                p => p.Id == pagamentoId,
                cancellationToken
            );

        if (pagamento is null ||
            pagamento.Status != StatusPagamento.PENDENTE ||
            !pagamento.Pedido.EstaPendente() ||
            pagamento.Pedido.EstaExpirado(DateTimeOffset.UtcNow))
            return;
        


        
        

        var checkout = await _gatewayPagamento
            .CriarSessaoPagamentoAsync(
                new CriarSessaoPagamentoRequest(
                    PedidoId: pagamento.PedidoId,
                    PagamentoId: pagamento.Id,
                    Valor: pagamento.Valor
                ),
                cancellationToken
            );

        pagamento.IdExterno = checkout.IdSessaoStripe;
        pagamento.UrlCheckout = checkout.UrlCheckout;

        await _dataContext.SaveChangesAsync(
            cancellationToken);

        await _publisher.Publish(
            new CobrancaGeradaEvent(
                pagamento.Id,
                checkout.IdSessaoStripe
            ),
            cancellationToken
        );
    }

    public async Task RegistrarCobrancaGerada(
        CobrancaGeradaEvent notification,
        CancellationToken cancellationToken = default)
    {
        var pagamento = await _dataContext.Pagamentos
            .FirstOrDefaultAsync(
                p => p.Id == notification.PagamentoId,
                cancellationToken
            );

        if (pagamento is null)
            return;

        // A cobrança deve corresponder à sessão atual.
        if (pagamento.IdExterno != notification.IdSessaoStripe)
            return;

        if (!pagamento.RegistrarCobranca(
                notification.IdSessaoStripe))
            return;

        await _dataContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task RegistrarAprovacao(
        PagamentoAprovadoEvent notification,
        CancellationToken cancellationToken = default)
    {
        var pagamento = await _dataContext.Pagamentos
            .FirstOrDefaultAsync(
                p => p.Id == notification.PagamentoId,
                cancellationToken
            );

        if (pagamento is null ||
            pagamento.IdExterno != notification.IdSessaoStripe)
            return;

        // Evita repetir uma confirmação já registrada.
        if (!pagamento.Confirmar())
            return;

        var tentativa = new PagamentoTentativa(
            pagamento.Id,
            StatusPagamento.CONFIRMADO
        );

        _dataContext.PagamentoTentativas.Add(tentativa);

        // Atualiza Pagamento e insere a tentativa
        // na mesma transação do SaveChanges.
        await _dataContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new PagamentoConfirmadoEvent(pagamento.PedidoId),
            cancellationToken
        );
    }
   public async Task RegistrarRecusa(
        PagamentoRecusadoEvent notification,
        CancellationToken cancellationToken = default)
    {
        var pagamento = await _dataContext.Pagamentos
            .FirstOrDefaultAsync(
                p => p.Id == notification.PagamentoId,
                cancellationToken
            );

        if (pagamento is null ||
            pagamento.IdExterno != notification.IdSessaoStripe)
            return;

        if (!pagamento.Recusar())
            return;

        var tentativa = new PagamentoTentativa(
            pagamento.Id,
            StatusPagamento.RECUSADO
        );

        _dataContext.PagamentoTentativas.Add(tentativa);

        await _dataContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> JaFoiPago(
        Guid idPedido,
        CancellationToken cancellationToken = default)
    {
        return _dataContext.Pagamentos.AnyAsync(
            p => p.PedidoId == idPedido &&
                 p.Status == StatusPagamento.CONFIRMADO,
            cancellationToken);
    }


    public async Task<string?> Checkout(
        Guid pedidoId,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTimeOffset.UtcNow;

        return await _dataContext.Pagamentos
            .AsNoTracking()
            .Where(p =>
                p.PedidoId == pedidoId &&
                p.Pedido.Status == StatusPedido.PENDENTE &&
                p.Pedido.ExpiraEm > agora &&
                p.Status == StatusPagamento.COBRANCA_GERADA
            )
            .Select(p => p.UrlCheckout)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
}
