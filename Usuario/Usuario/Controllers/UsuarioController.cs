using Microsoft.AspNetCore.Mvc;
using Usuario.DTO;
using Usuario.Servicos;
using Microsoft.AspNetCore.Authorization;
namespace Usuario.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioServ _UsuarioServ;

        public UsuarioController()
        {
            _UsuarioServ = new UsuarioServ();
        }

        [Authorize(Policy = "ServiceApiKey")]
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(Guid id, UsuarioInputDTO dto)
        {
            try
            {
                var usuarioAtualizado = await _UsuarioServ.AtualizarUsuario(id, dto);
                if (usuarioAtualizado == null)
                {
                    return NotFound();
                }
                return Ok(usuarioAtualizado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [Authorize(Policy = "ServiceApiKey")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] UsuarioInputDTO dto)
        {
            try
            {
                var usuario = await _UsuarioServ.Criar(dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, usuario);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);

            }
        }


        [Authorize(Policy = "ServiceApiKey")]
        [HttpGet("{id}")]
            public async Task<IActionResult> ObterPorId(Guid id)
            {
                var usuarioDto = await _UsuarioServ.ObterPorId(id);
             
            if (usuarioDto == null)
                {

                    return NotFound();
                }
            return Ok(usuarioDto);
            }
            
    }
}
