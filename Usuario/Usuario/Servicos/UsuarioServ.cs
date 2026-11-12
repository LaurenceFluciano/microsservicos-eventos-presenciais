using AppTemplate.DTO;
using AppTemplate.Infra;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Usuario.DTO;

namespace Usuario.Servicos
{
    public class UsuarioServ
    {
        private DataContext _dataContext;

        public UsuarioServ()
        {
            _dataContext = GeradorDeServicos.CarregarContexto();
        }

        public async Task<UsuarioOutputDTO> Criar(UsuarioInputDTO dto)
        {
            var novoUsuario = new UsuarioModel
            {
                Id = Guid.CreateVersion7(),
                Nome = dto.Nome,
                Email = dto.Email,
                Telefone = dto.Telefone

            };
            _dataContext.Usuarios.Add(novoUsuario);
            await _dataContext.SaveChangesAsync();

            return new UsuarioOutputDTO
            {
                Id = novoUsuario.Id,
                Nome = novoUsuario.Nome,
                Email = novoUsuario.Email,
                Telefone = novoUsuario.Telefone
            };
        }
    }
    }
}
