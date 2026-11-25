using Api.Application.DTO;
using Api.Application.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Api.Attributes;

namespace Api.Controllers;

[ApiController]
[Route("/api/ofertas")]
public class OfertaIngressoController : ControllerBase
{
    private readonly OfertaIngressoService _ofertaIngressoService;

    public OfertaIngressoController(
        OfertaIngressoService ofertaIngressoService)
    {
        _ofertaIngressoService = ofertaIngressoService;
    }

    [HttpPost]
    [UserJwt]
    public async Task<IActionResult> Criar(
        [FromBody] CriarOfertaIngressoRequest request)
    {
        var response = await _ofertaIngressoService.Criar(request);

        return Ok(response);
    }


    [HttpPatch("{idOferta:guid}")]
    [UserJwt]
    public async Task<IActionResult> Atualizar(
        [FromRoute] Guid idOferta,
        [FromBody] AtualizarOfertaIngressoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _ofertaIngressoService.Atualizar(
            idOferta,
            request,
            cancellationToken
        );

        return Ok(response);
    }

    [HttpGet("~/api/eventos/meus/{idEvento:guid}/ofertas")]
    [UserJwt]
    public async Task<IActionResult> ConsultarOfertasDoMeuEvento(
        [FromRoute] Guid idEvento,
        CancellationToken cancellationToken = default)
    {
      
        var response = await _ofertaIngressoService
            .ConsultarOfertasDoMeuEventoAsync(
                idEvento,
                cancellationToken
            );

        return Ok(response);
       
    }


    [HttpGet("~/api/eventos/publicados/{idEvento:guid}/ofertas")]
    [AllowAnonymous]
    public async Task<IActionResult> ConsultarOfertasPorEventoPublicado(
        [FromRoute] Guid idEvento,
        CancellationToken cancellationToken = default)
    {
      
        var response = await _ofertaIngressoService
            .ConsultarOfertasPorEventoPublicadoAsync(
                idEvento,
                cancellationToken
            );

        return Ok(response);
       
    }


    [HttpGet("eventos/{idEvento:guid}/possui-oferta-valida")]
    [InternalApi]
    public async Task<ActionResult<bool>> PossuiOfertaValida(
        [FromRoute] Guid idEvento,
        CancellationToken cancellationToken)
    {
        var possuiOferta = await _ofertaIngressoService
            .PossuiOfertaValidaAsync(
                idEvento,
                cancellationToken
            );

        return Ok(possuiOferta);
    }

}