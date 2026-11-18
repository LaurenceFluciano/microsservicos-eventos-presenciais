using System.Security.Claims;
using Api.Application.Services;
using Api.Application.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Error;
using Microsoft.AspNetCore.Http.HttpResults;



namespace Api.Controllers;

[ApiController]
[Route("api/eventos")]
[Authorize]
public class EventosController : ControllerBase
{

    public EventoService EventoService;

    public EventosController(EventoService eventoService)
    {
        EventoService = eventoService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarEventoRequest request)
    {
        try
        {
            var result = await EventoService.Cadastrar(request);

            return Ok(result);
        }
        catch (DomainException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("meus")]
    public IActionResult MeusEventos()
    {
        var usuarioId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        return Ok(new
        {
            UsuarioId = usuarioId,
            Mensagem = "Usuário autenticado!"
        });
    }



}