using Usuario.DTO;
using Usuario.Infra;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Usuario.Servicos
{
    public class UsuarioServ
    {
        private DataContext _dataContext;

        public UsuarioServ()
        {
            _dataContext = GeradorDeServicos.CarregarContexto();
        }
        public async Task<UsuarioOutputDTO> AtualizarUsuario(Guid id, UsuarioInputDTO dto)
        {
            var usuarioExistente = await _dataContext.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuarioExistente == null)
            {
                return null;
            }
            usuarioExistente.Nome = dto.Nome;
            usuarioExistente.Email = dto.Email;
            usuarioExistente.Telefone = dto.Telefone;
            await _dataContext.SaveChangesAsync();
            return new UsuarioOutputDTO
            {
                Id = usuarioExistente.Id,
                Nome = usuarioExistente.Nome,
                Email = usuarioExistente.Email,
                Telefone = usuarioExistente.Telefone
            };

        }
        public async Task<UsuarioOutputDTO> ObterPorId(Guid id)
        {
            var usuario = await _dataContext.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return null;
            }
            return new UsuarioOutputDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone
            };

        }           


        public async Task<UsuarioOutputDTO> Criar(UsuarioInputDTO dto)
        {

            dto.Email = dto.Email.Trim().ToLowerInvariant();

            var emailJaExiste = await _dataContext.Usuarios
                .AnyAsync(e => e.Email == dto.Email);

            if (emailJaExiste)
            {
                throw new Exception("O e-mail já está em uso.");
            }

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
