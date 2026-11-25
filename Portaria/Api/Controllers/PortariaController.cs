
using Api.Application.DTO;
using Api.Application.Services;
using Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/portaria")]
public sealed class PortariaController : ControllerBase
{
    private readonly PortariaService _servicePortaria;

    public PortariaController(PortariaService portariaService)
    {
        _servicePortaria = portariaService;
    }

    [HttpPost("credencial-acesso")]
    [InternalApi]
    public async Task<IActionResult> CriarCredencialAcesso(
        [FromBody] CriarCredencialAcessoRequest request,
        CancellationToken cancellationToken)
    {
        await _servicePortaria.CriarCredencialAcesso(
            request,
            cancellationToken
        );

        return NoContent();
    }

    [HttpPost("/portaria/checkin/{codigo}")]
    [UserJwt]
    public async Task<IActionResult> CheckIn(
        [FromRoute] string codigo,
        CancellationToken cancellationToken)
    {
        await _servicePortaria.CheckIn(
            codigo,
            cancellationToken
        );

        return Ok(new { Message = "Entrada validada com sucesso!"});
    }
}
