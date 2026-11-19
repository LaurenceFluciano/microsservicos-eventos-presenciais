using Microsoft.AspNetCore.Authorization;
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
        private readonly AuthService _authService;

        public AuthController(
            CadastroContaService cadastroContaService,
            AuthService authService)
        {
            _cadastroContaService = cadastroContaService;
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginInputDTO dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.LoginAsync(dto, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { mensagem = "Credenciais inválidas." });
            }
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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
