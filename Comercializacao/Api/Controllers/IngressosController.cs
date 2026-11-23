
using Api.Application.Services;
using Api.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/ingressos")]
public class IngressosController : ControllerBase
{

    private IngressoService _ingressoService;

    public IngressosController(IngressoService ingressoService)
    {
        _ingressoService = ingressoService;
    }


    [HttpGet("{id:guid}")]
    [UserJwt]
    public async Task<IActionResult> ObterPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var ingresso = await _ingressoService.ConsultarPorIdAsync(
            id,
            cancellationToken);

        return Ok(ingresso);
    }



    [HttpGet("codigo/{codigoIngresso}")]
    [InternalApi]
    public async Task<IActionResult> ObterPorCodigo(
        [FromRoute] string codigoIngresso,
        CancellationToken cancellationToken)
    {
        var ingresso = await _ingressoService.ConsultarPorCodigoAsync(
            codigoIngresso,
            cancellationToken);

        return Ok(ingresso);
    }


    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> ObterQrCode(
        Guid idIngresso,
        CancellationToken cancellationToken)
    {
        return Ok();
    }
}