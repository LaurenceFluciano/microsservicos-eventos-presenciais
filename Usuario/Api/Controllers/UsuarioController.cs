
using System.ComponentModel.DataAnnotations;
using Api.Attributes;
using Api.Autenticacao;
using Microsoft.AspNetCore.Mvc;
using Usuario.DTO;
using Usuario.Servicos;

namespace Usuario.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsuarioController : ControllerBase
{
    private readonly UsuarioServ _usuarioServ;
    private readonly IUsuarioAtual _usuarioAtual;

    public UsuarioController(
        UsuarioServ usuarioServ,
        IUsuarioAtual usuarioAtual)
    {
        _usuarioServ = usuarioServ;
        _usuarioAtual = usuarioAtual;
    }


    [UserJwt]
    [HttpGet("me")]
    public async Task<IActionResult> ObterMeusDados()
    {
        var usuarioId = _usuarioAtual.Id;

        if (usuarioId is null)
            return Unauthorized();

        var usuario = await _usuarioServ.ObterPorId(
            usuarioId.Value);

        return usuario is null
            ? NotFound()
            : Ok(usuario);
    }

    [UserJwt]
    [HttpPatch("me")]
    public async Task<IActionResult> AtualizarMeusDados(
        [FromBody] AtualizarMeInputDTO dto,
        CancellationToken cancellationToken)
    {
        var usuarioId = _usuarioAtual.Id;

        if (usuarioId is null)
            return Unauthorized();

        try
        {
            var usuario = await _usuarioServ.AtualizarMeAsync(
                usuarioId.Value,
                dto,
                cancellationToken);

            return usuario is null
                ? NotFound()
                : Ok(usuario);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [InternalApi]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> AtualizarUsuario(
        Guid id,
        [FromBody] UsuarioInputDTO dto)
    {
        try
        {
            var usuarioAtualizado =
                await _usuarioServ.AtualizarUsuario(id, dto);

            return usuarioAtualizado is null
                ? NotFound()
                : Ok(usuarioAtualizado);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    [InternalApi]
    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] UsuarioInputDTO dto)
    {
        try
        {
            var usuario = await _usuarioServ.Criar(dto);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = usuario.Id },
                usuario);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    [InternalApi]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var usuarioDto = await _usuarioServ.ObterPorId(id);

        return usuarioDto is null
            ? NotFound()
            : Ok(usuarioDto);
    }


    [HttpGet]
    [InternalApi]
    public async Task<IActionResult> ObterUsuario(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Nessa versão filtro por e-mail é obrigatório.");

        var usuario = await _usuarioServ.ObterPorEmail(
            email,
            cancellationToken
        );

        if (usuario is null)
            return NotFound();

        return Ok(usuario);
    }
}
