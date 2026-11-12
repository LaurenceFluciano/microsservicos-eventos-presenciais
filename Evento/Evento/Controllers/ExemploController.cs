using Evento.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Evento.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExemploController : ControllerBase
    {
        private ServExemplo _servExemplo;

        public ExemploController()
        {
            _servExemplo = new ServExemplo();
        }

        [Route("/api/[Controller]/{id}")]
        [HttpGet]
        public IActionResult Exemplo(int id)
        {
            try
            {
                var exemploDto = _servExemplo.Exemplo(id);

                return Ok(exemploDto);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
