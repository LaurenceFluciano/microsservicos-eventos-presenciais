using Microsoft.AspNetCore.Mvc;
using Usuario.DTO;
using Usuario.Servicos;

namespace Usuario.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly CadastroContaService _cadastroContaService;

        public AuthController(CadastroContaService cadastroContaService)
        {
            _cadastroContaService = cadastroContaService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Registrar(
            [FromBody] CriarContaInputDTO dto,
            CancellationToken cancellationToken)
        {
            try
            {
                await _cadastroContaService.SolicitarCadastroAsync(dto, cancellationToken);
                return Accepted(new
                {
                    mensagem = "Solicitação recebida. Confirme o e-mail para ativar a conta."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensagem = ex.Message });
            }
            catch (EmailSenderNotConfiguredException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { mensagem = "O envio de e-mail não está disponível." });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerificarEmail(
            [FromBody] VerificarEmailInputDTO dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var resultado = await _cadastroContaService.VerificarEmailAsync(
                    dto.Token,
                    cancellationToken);

                return Ok(new
                {
                    usuarioId = resultado.UsuarioId,
                    contaId = resultado.ContaId,
                    mensagem = "E-mail confirmado e conta criada."
                });
            }
            catch (InvalidVerificationTokenException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensagem = ex.Message });
            }
        }
    }
}
