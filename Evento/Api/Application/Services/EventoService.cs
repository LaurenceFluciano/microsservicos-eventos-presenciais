
using Api.Application.DTO;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services;

public class EventoService
{
    private readonly DataContext _dataContext;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IComercializacaoClient? _comercializacaoClient;

    public EventoService(
        DataContext dataContext,
        IUsuarioAtual usuarioAtual,
        IComercializacaoClient? comercializacaoClient = null)
    {
        _dataContext = dataContext;
        _usuarioAtual = usuarioAtual;
        _comercializacaoClient = comercializacaoClient;
    }

    public async Task<DetalheEventoResponse> Cadastrar(
        CriarEventoRequest request)
    {
        var organizadorId = _usuarioAtual.GetId();

        var evento = EventoMapper.ToEntity(
            request,
            organizadorId
        );

        var ultimoNumero = await _dataContext
            .Set<Evento>()
            .AsNoTracking()
            .Where(e => e.Slug == evento.Slug)
            .Select(e => (int?)e.SlugNumero)
            .MaxAsync();

        evento.DefinirNumeroSlug(
            (ultimoNumero ?? 0) + 1
        );

        _dataContext.Set<Evento>().Add(evento);

        await _dataContext.SaveChangesAsync();

        return EventoMapper.ToDetalheResponse(evento);
    }

    public async Task<DetalheEventoResponse> Atualizar(
        Guid id,
        AtualizarEventoRequest request)
    {
        var organizadorId = _usuarioAtual.GetId();
        var evento = await _dataContext.Set<Evento>()
            .SingleOrDefaultAsync(e => e.Id == id);

        if (evento is null)
            throw new KeyNotFoundException("Evento não encontrado.");

        if (evento.OrganizadorId != organizadorId)
            throw new UnauthorizedAccessException(
                "Apenas o organizador pode alterar este evento."
            );

        var slugAnterior = evento.Slug;

        // Valida o período resultante e preserva os campos ausentes.
        evento.AtualizarInformacoes(
            request.Nome,
            request.Descricao,
            request.Local,
            request.Inicio,
            request.Fim
        );

        // Ao trocar o nome, gera um slug único sem contar o próprio evento.
        // Se o slug-base permanecer igual, mantém o número atual e a URL.
        if (evento.Slug != slugAnterior)
        {
            var ultimoNumero = await _dataContext.Set<Evento>()
                .AsNoTracking()
                .Where(e => e.Id != id && e.Slug == evento.Slug)
                .Select(e => (int?)e.SlugNumero)
                .MaxAsync();

            evento.DefinirNumeroSlug((ultimoNumero ?? 0) + 1);
        }

        await _dataContext.SaveChangesAsync();
        return EventoMapper.ToDetalheResponse(evento);
    }

    public async Task<EventoResponse> Publicar(
        Guid id,
        CancellationToken cancellationToken = default,
        string? publicBaseUrl = null)
    {
        var organizadorId = _usuarioAtual.GetId();
        var evento = await _dataContext.Set<Evento>()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evento is null)
            throw new KeyNotFoundException("Evento nao encontrado.");

        if (evento.OrganizadorId != organizadorId)
            throw new UnauthorizedAccessException(
                "Apenas o organizador pode publicar este evento.");

        // Publicacao repetida e idempotente: nao consulta Comercializacao novamente.
        if (evento.PublicadoEm.HasValue)
            return EventoMapper.ToEventoResponse(evento, publicBaseUrl);

        if (_comercializacaoClient is null)
            throw new InvalidOperationException(
                "O cliente de Comercializacao nao foi configurado.");

        bool possuiOfertaValida;
        try
        {
            possuiOfertaValida = await _comercializacaoClient
                .PossuiOfertaValidaAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new HttpRequestException(
                "Nao foi possivel consultar as ofertas de ingresso. Tente novamente.", ex);
        }

        if (!possuiOfertaValida)
            throw new Api.Error.DomainException(
                "O evento precisa ter pelo menos uma oferta de ingresso valida para ser publicado.");

        evento.Publicar();
        await _dataContext.SaveChangesAsync(cancellationToken);
        return EventoMapper.ToEventoResponse(evento, publicBaseUrl);
    }
}
