using Api.Application.DTO;
using Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Authorize]
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
    public async Task<IActionResult> Criar(
        [FromBody] CriarOfertaIngressoRequest request)
    {
        var response = await _ofertaIngressoService.Criar(request);

        return Ok(response);
    }
}