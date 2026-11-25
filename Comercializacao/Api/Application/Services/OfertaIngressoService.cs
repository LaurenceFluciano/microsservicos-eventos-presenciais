using Api.Application.DTO;
using Api.Application.DTO.Client;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Error;
using Api;
using Api.Infrastructure;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class OfertaIngressoService
{
    private DataContext _dataContext;
    private OfertaIngressoMapper _ofertaIngressoMapper;
    private IEventoClient _eventoClient;
    private IUsuarioAtual _usuarioAtual;

    public OfertaIngressoService(
        DataContext dataContext,
        OfertaIngressoMapper ofertaIngressoMapper,
        IEventoClient eventoClient,
        IUsuarioAtual usuarioAtual)
    {
        _dataContext = dataContext;
        _ofertaIngressoMapper = ofertaIngressoMapper;
        _eventoClient = eventoClient;
        _usuarioAtual = usuarioAtual;
    }

    public async Task<OfertaIngressoResponse> Criar(
        CriarOfertaIngressoRequest request,
        CancellationToken cancellationToken = default)
    {
        var evento = await _eventoClient.ObterEventoAsync(
            request.IdEvento,
            cancellationToken
        );

        if (evento.OrganizadorId != _usuarioAtual.GetId())
            throw new DomainException(
                "Você não tem permissão para adicionar ofertas de ingressos nesse evento."
            );

        var oferta = _ofertaIngressoMapper.ToEntity(request, evento.OrganizadorId);

        _dataContext.OfertasIngresso.Add(oferta);

        await _dataContext.SaveChangesAsync(cancellationToken);

        return _ofertaIngressoMapper.ToResponse(oferta);
    }

    public async Task<OfertaIngressoResponse> Atualizar(
        Guid idOferta,
        AtualizarOfertaIngressoRequest request,
        CancellationToken cancellationToken = default)
    {
        var oferta = await _dataContext.OfertasIngresso
            .FirstOrDefaultAsync(
                o => o.Id == idOferta,
                cancellationToken
            ) ?? throw new NotFoundException(
                "Oferta não encontrada."
            );

        if (oferta.IdOrganizador != _usuarioAtual.GetId())
            throw new DomainException(
                "Você não tem permissão para atualizar essa oferta."
            );

        if (oferta.Status == StatusOfertaIngresso.ENCERRADA)
            throw new DomainException(
                "Não é permitido atualizar uma oferta encerrada."
            );

        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new DomainException(
                "O nome da oferta é obrigatório."
            );

        if (request.PrecoUnitario < 0)
            throw new DomainException(
                "O preço unitário não pode ser negativo."
            );

        if (request.QuantidadeTotal <= 0)
            throw new DomainException(
                "A quantidade total deve ser maior que zero."
            );

        var quantidadeComprometida =
            await ObterQuantidadeComprometida(
                idOferta,
                cancellationToken
            );

        if (request.QuantidadeTotal < quantidadeComprometida)
            throw new DomainException(
                $"A quantidade total não pode ser inferior aos " +
                $"{quantidadeComprometida} ingressos comprometidos."
            );

        _ofertaIngressoMapper.ToUpdate(request, oferta);

        await _dataContext.SaveChangesAsync(cancellationToken);

        return _ofertaIngressoMapper.ToResponse(oferta, quantidadeComprometida);
    }

    public async Task<OfertaIngresso> BuscarEntidadePorId(Guid IdOferta)
    {

        var oferta = await
            _dataContext.OfertasIngresso
                .Where(oi => oi.Id == IdOferta)
                .FirstOrDefaultAsync() 
            ?? 
                throw new NotFoundException("Oferta não encontrada.");

        return oferta;
    }


    public async Task<int> ObterQuantidadeComprometida(
        Guid idOferta,
        CancellationToken cancellationToken)
    {
        return await _dataContext.Pedidos
            .Where(p => p.OfertaId == idOferta)
            .Where(Pedido.ComprometidosEm(DateTime.UtcNow))
            .SumAsync(
                p => (int?)p.Quantidade,
                cancellationToken
            ) ?? 0;
    }



    public async Task<ConsultarOfertasEventoResponse> ConsultarOfertasPorEventoPublicadoAsync(
        Guid idEvento,
        CancellationToken cancellationToken = default)
    {
        var evento = await _eventoClient.ObterEventoAsync(
            idEvento,
            cancellationToken
        );

        if (!evento.EstaPublicado)
        {
            throw new NotFoundException(
                "Evento publicado não encontrado.");
        }

        var ofertas = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .Where(o => o.IdEvento == idEvento)
            .Where(o => o.Status == StatusOfertaIngresso.ATIVA)
            .OrderBy(o => o.Preco)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);


        var idsOfertas = ofertas
            .Select(o => o.Id)
            .ToArray();

        var agora = DateTime.UtcNow;

        var quantidadesComprometidas = await _dataContext.Pedidos
            .AsNoTracking()
            .Where(p => idsOfertas.Contains(p.OfertaId))
            .Where(Pedido.ComprometidosEm(agora))
            .GroupBy(p => p.OfertaId)
            .Select(g => new
            {
                OfertaId = g.Key,
                Quantidade = g.Sum(p => p.Quantidade)
            })
            .ToDictionaryAsync(
                x => x.OfertaId,
                x => x.Quantidade,
                cancellationToken
            );


        var ofertasResponse = ofertas
            .Select(oferta =>
            {
                quantidadesComprometidas.TryGetValue(
                    oferta.Id,
                    out var quantidadeComprometida
                );

                return _ofertaIngressoMapper.ToResponse(
                    oferta,
                    quantidadeComprometida
                );
            })
            .ToList();

        return new ConsultarOfertasEventoResponse(
            evento.Id,
            evento.Nome,
            HorarioBrasilia.ParaLocal(evento.Inicio),
            HorarioBrasilia.ParaLocal(evento.Fim),
            ofertasResponse
        );
    }

      public async Task<ConsultarOfertasEventoResponse> ConsultarOfertasDoMeuEventoAsync(
        Guid idEvento,
        CancellationToken cancellationToken = default)
    {
        var evento = await _eventoClient.ObterEventoAsync(
            idEvento,
            cancellationToken
        );


        var idUsuarioAtual = _usuarioAtual.GetId();

        if (evento.OrganizadorId != idUsuarioAtual)
        {
            throw new NotFoundException("Evento não encontrado.");
        }

        var ofertas = await _dataContext.OfertasIngresso
            .AsNoTracking()
            .Where(o => o.IdEvento == idEvento)
            .Where(o => o.Status == StatusOfertaIngresso.ATIVA)
            .OrderBy(o => o.Preco)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);



        var idsOfertas = ofertas
            .Select(o => o.Id)
            .ToArray();

        var agora = DateTime.UtcNow;

        var quantidadesComprometidas = await _dataContext.Pedidos
            .AsNoTracking()
            .Where(p => idsOfertas.Contains(p.OfertaId))
            .Where(Pedido.ComprometidosEm(agora))
            .GroupBy(p => p.OfertaId)
            .Select(g => new
            {
                OfertaId = g.Key,
                Quantidade = g.Sum(p => p.Quantidade)
            })
            .ToDictionaryAsync(
                x => x.OfertaId,
                x => x.Quantidade,
                cancellationToken
            );


        var ofertasResponse = ofertas
            .Select(oferta =>
            {
                quantidadesComprometidas.TryGetValue(
                    oferta.Id,
                    out var quantidadeComprometida
                );

                return _ofertaIngressoMapper.ToResponse(
                    oferta,
                    quantidadeComprometida
                );
            })
            .ToList();

        return new ConsultarOfertasEventoResponse(
            evento.Id,
            evento.Nome,
            HorarioBrasilia.ParaLocal(evento.Inicio),
            HorarioBrasilia.ParaLocal(evento.Fim),
            ofertasResponse
        );
    }

    public async Task<bool> PossuiOfertaValidaAsync(
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.OfertasIngresso
            .AsNoTracking()
            .AnyAsync(
                oferta =>
                    oferta.IdEvento == eventoId &&
                    oferta.Status == StatusOfertaIngresso.ATIVA &&
                    oferta.QuantidadeTotal > 0,
                cancellationToken
            );
    }

}

