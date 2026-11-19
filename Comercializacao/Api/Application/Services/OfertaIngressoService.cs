using Api.Application.DTO;
using Api.Application.DTO.Client;
using Api.Application.Mappers;
using Api.Application.Ports;
using Api.Error;
using Api.Infrastructure;
using Api.Model;

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

        if (evento.IdOrganizador != _usuarioAtual.GetId())
            throw new DomainException(
                "Você não tem permissão para adicionar ofertas de ingressos nesse evento."
            );

        var oferta = _ofertaIngressoMapper.toEntity(request, evento.IdOrganizador);

        _dataContext.OfertasIngresso.Add(oferta);

        await _dataContext.SaveChangesAsync(cancellationToken);

        return _ofertaIngressoMapper.toResponse(oferta);
    }
}

