using Api.Application.DTO;
using Api.Application.Events;
using Api.Application.DTO.Pagamentos;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class ProcessarPedido
{
    private readonly DataContext _dataContext;

    #region Serviços
    private IngressoService _ingressoService;
    private PagamentoService _pagamentoService;

    #endregion


    public ProcessarPedido(
        DataContext dataContext, 
        IngressoService ingressoService,
        PagamentoService pagamentoService
    )
    {
        _dataContext = dataContext;
        _ingressoService = ingressoService;
        _pagamentoService = pagamentoService;

    }

    public async Task Processar(
        PedidoCriadoEvent notification,
        CancellationToken cancellationToken = default
    )
    {
        var pedido = await 
            _dataContext.Pedidos.Where(p => p.Id == notification.PedidoId).FirstOrDefaultAsync();

        if (pedido is null)
            return;

        await Processar(pedido, cancellationToken);
    }

    public async Task Processar(
        PagamentoConfirmadoEvent notification,
        CancellationToken cancellationToken = default
    )
    {
        var pedido = await 
            _dataContext.Pedidos.Where(p => p.Id == notification.PedidoId).FirstOrDefaultAsync();

        if (pedido is null)
            return;

        await Processar(pedido, cancellationToken);
    }

    private async Task Processar(
        Pedido pedido, 
        CancellationToken cancellationToken = default
    )
    {
        if (pedido.EstaConfirmado()) return;

        if (pedido.EstaPendente())
        {
            if (!pedido.IsGratis)
            {
                var pagamentoConfirmado = await _pagamentoService
                    .JaFoiPago(pedido.Id, cancellationToken);

                if (!pagamentoConfirmado)
                {
                    await GarantirPagamento(pedido, cancellationToken);
                    return;
                }
            }

            pedido.MarcarEmProcessamento();

            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        if (!pedido.EstaEmProcessamento())
            return;

        await _ingressoService.EmitirIngressos(
            new CriarIngressoRequest(
                pedido.Id
            )
        );

    }


    private async Task GarantirPagamento(
        Pedido pedido,
        CancellationToken cancellationToken)
    {
        var existePagamentoPendente = await _dataContext.Pagamentos
            .AnyAsync(
                p => p.PedidoId == pedido.Id &&
                    p.Status == StatusPagamento.PENDENTE,
                cancellationToken
            );

        if (existePagamentoPendente)
            return;

        await _pagamentoService.Criar(
            new CriarPagamentoRequest(pedido.Id),
            cancellationToken
        );
    }

}