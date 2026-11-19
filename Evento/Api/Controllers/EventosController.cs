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

    [HttpPatch("meus/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarEventoRequest request)
    {
        try
        {
            var resultado = await EventoService.Atualizar(id, request);
            return Ok(resultado);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (DomainException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("meus/{id:guid}/publicar")]
    public async Task<IActionResult> Publicar(Guid id)
    {
        try
        {
            var resultado = await EventoService.Publicar(id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(503, exception.Message);
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