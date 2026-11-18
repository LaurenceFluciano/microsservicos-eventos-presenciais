using AppTemplate.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Usuario.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioServ _servUsuario;

        public UsuarioController()
        {
            _servUsuario = new UsuarioServ();
        }

      
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(Guid id, UsuarioInputDTO dto)
        {
            try
            {
                var usuarioAtualizado = await _servUsuario.AtualizarUsuario(id, dto);
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
        [Route("/api/[usuario]")]
        [HttpPost]
        public async Task<IActionResult> Criar(UsuarioInputDTO dto)
        {
            try
            {
                var usuario = await _usuarioServ.Criar(dto);
                return CreatedAtAction(nameof(Criar), new { id = usuario.Id }, usuario);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);

            }
        }

      
	
        [HttpGet("{id}")]
            public async Task<IActionResult<UsuarioOutputDTO>> ObterPorId(Guid id)
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
