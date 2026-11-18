using Api.Application.Services;
using Api.Application.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Error;



namespace Api.Controllers;

[ApiController]
[Route("api/eventos")]
[Authorize]
public class EventosController : ControllerBase
{

    public EventoService EventoService;
    private readonly IConfiguration _configuracao;

    public EventosController(EventoService eventoService, IConfiguration configuracao)
    {
        EventoService = eventoService;
        _configuracao = configuracao;
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
            // Configuravel por Eventos__PublicBaseUrl. Sem configuracao,
            // utiliza o esquema, host, porta e PathBase da requisicao.
            var baseUrl = _configuracao["Eventos:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            var resultado = await EventoService.Publicar(id, publicBaseUrl: baseUrl);
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

    private string ObterBaseUrlPublica()
    {
        var configurada = _configuracao["Eventos:PublicBaseUrl"];
        return !string.IsNullOrWhiteSpace(configurada)
            ? configurada
            : $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    }

    [HttpGet("meus")]
    public async Task<IActionResult> MeusEventos(
        [FromQuery] string ordem = "asc",
        [FromQuery] bool? publicado = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventos = await EventoService.ListarMeus(
                ordem,
                publicado,
                cancellationToken,
                ObterBaseUrlPublica());

            return Ok(eventos);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("meus/{id:guid}")]
    public async Task<IActionResult> ConsultarMeu(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evento = await EventoService.ConsultarMeu(
                id,
                cancellationToken,
                ObterBaseUrlPublica());

            return Ok(evento);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }



}