
using Api.Application.DTO.Pedidos;

using Api.Application.DTO;
using Api.Application.DTO.Client;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Error;
using Api.Model;
using Microsoft.EntityFrameworkCore;
using Api.Application.Events;
using MediatR;
using Api.Application.DTO.Pagamentos;

namespace Api.Application.Services;

public class PedidoService
{
    private readonly DataContext _dataContext;
    private PedidoMapper _pedidoMapper;
   
    #region Clientes
    private IEventoClient _eventoClient;

    #endregion

    #region Serviços
    private OfertaIngressoService _ofertaIngressoService;

    #endregion

    #region Publicadores

    private readonly IPublisher _publisher;

    #endregion

    public PedidoService(
        DataContext dataContext, 
        PedidoMapper pedidoMapper, 
        OfertaIngressoService ofertaIngressoService,
        IEventoClient eventoClient,
        IPublisher publisher
    )
    {
        _dataContext = dataContext;
        _pedidoMapper = pedidoMapper;
        _ofertaIngressoService = ofertaIngressoService;
        _eventoClient = eventoClient;
        _publisher = publisher;

    }

    public async Task<CriarPedidoResponse> CriarPedido(
        CriarPedidoRequest request,
        CancellationToken cancellationToken = default)
    {

        var oferta = await _ofertaIngressoService
            .BuscarEntidadePorId(request.OfertaId);

        var evento = await _eventoClient
            .ObterEventoAsync(oferta.IdEvento);

        if (!evento.EstaPublicado)
            throw new DomainException("Não foi possível realizar o pedido nessa oferta.");

        if (oferta.Status != StatusOfertaIngresso.ATIVA)
            throw new DomainException(
                "A oferta de ingresso não está ativa."
            );
        
        var comprometida = await _ofertaIngressoService.ObterQuantidadeComprometida(
            oferta.Id,
            cancellationToken
        );

        var disponivel = oferta.QuantidadeTotal - comprometida;

        if (disponivel == 0)
            throw new DomainException("Não há mais ingressos disponíveis.");

        if (request.Quantidade > disponivel)
            throw new DomainException(
                $"Quantidade indisponível. Restam {disponivel} ingressos."
            );

        var pedido = _pedidoMapper.ToEntity(request, oferta);

        _dataContext.Pedidos.Add(pedido);

        await _dataContext.SaveChangesAsync(
            cancellationToken
        );

        await _publisher.Publish(
            new PedidoCriadoEvent(pedido.Id)
        );

        return _pedidoMapper.ToCriarPedidoResponse(pedido);
    }


    
    public async Task<DetalhePedidoResponse?> ObterPorId(
        Guid pedidoId,
        CancellationToken cancellationToken = default)
    {
        var pedido = await _dataContext.Pedidos
            .AsNoTracking()
            .Where(p => p.Id == pedidoId)
            .Select(p => new
            {
                p.Id,
                p.OfertaId,
                p.Quantidade,
                p.Status,
                p.ExpiraEm,

                EventoId = _dataContext.OfertasIngresso
                    .Where(o => o.Id == p.OfertaId)
                    .Select(o => (Guid?)o.IdEvento)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pedido is null)
            return null;

        if (pedido.EventoId is null)
            throw new InvalidOperationException(
                "Oferta do pedido não encontrada."
            );

        var pagamentos = await _dataContext.Pagamentos
            .AsNoTracking()
            .Where(p =>
                p.PedidoId == pedidoId &&
                p.Status != StatusPagamento.CANCELADO &&
                p.Status != StatusPagamento.ESTORNADO)
            .Select(p => new
            {
                p.Id,
                p.UrlCheckout,
                p.CriadoEm
            })
            .ToListAsync(cancellationToken);

        var pagamento = pagamentos
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new PagamentoResponse(
                p.Id,
                p.UrlCheckout
            ))
            .FirstOrDefault();

        var ingressos = await _dataContext.Ingressos
            .AsNoTracking()
            .Where(i => i.PedidoId == pedidoId)
            .OrderBy(i => i.EmitidoEm)
            .Select(i => new IngressoResponse(
                i.Id,
                i.CodigoIngresso,
                i.EmitidoEm
            ))
            .ToListAsync(cancellationToken);

        return new DetalhePedidoResponse(
            EventoId: pedido.EventoId.Value,
            PedidoId: pedido.Id,
            OfertaId: pedido.OfertaId,
            Quantidade: pedido.Quantidade,
            Status: pedido.Status,
            ExpiraEm: pedido.ExpiraEm,
            Pagamento: pagamento,
            Ingressos: ingressos
        );
    }



}
