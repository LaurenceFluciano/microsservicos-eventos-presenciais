
using Api.Application.DTO;
using Api.Application.Events;
using Api.Application.Ports;
using Api.Error;
using Api.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class IngressoService
{
    
    private readonly DataContext _dataContext;
    private readonly IPublisher _publisher;
    private readonly IEventoClient _eventoClient;
    private readonly IngressoDigitalService _ingressoDigitalService;

    public IngressoService(
        DataContext dataContext,
        IPublisher publisher,
        IEventoClient eventoClient,
        IngressoDigitalService ingressoDigitalService)
    {
        _dataContext = dataContext;
        _publisher = publisher;
        _eventoClient = eventoClient;
        _ingressoDigitalService = ingressoDigitalService;
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
                CodigoIngresso = CodigoIngressoGenerator.Gerar(6),
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



    public async Task<DetalheIngressoResponse> ConsultarPorIdAsync(
        Guid idIngresso,
        CancellationToken cancellationToken = default)
    {
        var ingresso = await _dataContext.Ingressos
            .AsNoTracking()
            .Include(i => i.Pedido)
            .FirstOrDefaultAsync(
                i => i.Id == idIngresso,
                cancellationToken
            )
            ?? throw new NotFoundException(
                "Ingresso não encontrado."
            );

        var idEvento = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .Where(o => o.Id == ingresso.Pedido.OfertaId)
            .Select(o => o.IdEvento)
            .FirstOrDefaultAsync(cancellationToken);

        if (idEvento == Guid.Empty)
        {
            throw new NotFoundException(
                "Evento associado ao ingresso não encontrado."
            );
        }

        var evento = await _eventoClient.ObterEventoAsync(
            idEvento,
            cancellationToken
        );

        var ingressoDigital = _ingressoDigitalService.Gerar(
            ingresso
        );

        return new DetalheIngressoResponse(
            Id: ingresso.Id,
            PedidoId: ingresso.PedidoId,
            EventoId: evento.Id,
            NomeEvento: evento.Nome,
            Inicio: evento.Inicio,
            Fim: evento.Fim,
            CodigoIngresso: ingresso.CodigoIngresso,
            EmitidoEm: ingresso.EmitidoEm,
            UrlValidacao: ingressoDigital.UrlConfirmacao,
            QrCodeDataUri: ingressoDigital.QrCodeDataUri
        );
    }


    public async Task<IngressoValidacaoResponse> ConsultarPorCodigoAsync(
        string codigoIngresso,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoIngresso))
        {
            throw new DomainException(
                "O código do ingresso é obrigatório.");
        }

        var ingresso = await _dataContext.Ingressos
            .AsNoTracking()
            .Include(i => i.Pedido)
            .FirstOrDefaultAsync(
                i => i.CodigoIngresso == codigoIngresso,
                cancellationToken
            )
            ?? throw new NotFoundException(
                "Ingresso não encontrado.");

        var idEvento = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .Where(o => o.Id == ingresso.Pedido.OfertaId)
            .Select(o => o.IdEvento)
            .FirstOrDefaultAsync(cancellationToken);

        if (idEvento == Guid.Empty)
        {
            throw new NotFoundException(
                "Evento associado ao ingresso não encontrado.");
        }

        var evento = await _eventoClient.ObterEventoAsync(
            idEvento,
            cancellationToken
        );


        return new IngressoValidacaoResponse(
            Id: ingresso.Id,
            PedidoId: ingresso.PedidoId,
            EventoId: evento.Id,
            OrganizadorId: evento.OrganizadorId,
            NomeEvento: evento.Nome,
            Inicio: evento.Inicio,
            Fim: evento.Fim,
            CodigoIngresso: ingresso.CodigoIngresso,
            EmitidoEm: ingresso.EmitidoEm
        );
    }


}
