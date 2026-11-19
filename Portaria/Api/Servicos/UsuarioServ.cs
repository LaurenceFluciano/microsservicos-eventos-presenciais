using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AppTemplate;
using Microsoft.EntityFrameworkCore;
using Usuario.DTO;
using Usuario.Models;

namespace Usuario.Servicos
{
    public sealed class UsuarioServ
    {
        private readonly DataContext _dataContext;

        public UsuarioServ(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<UsuarioOutputDTO?> AtualizarUsuario(Guid id, UsuarioInputDTO dto)
        {
            var usuarioExistente = await _dataContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuarioExistente is null)
            {
                return null;
            }

            dto.Email = dto.Email.Trim().ToLowerInvariant();
            var emailJaExiste = await _dataContext.Usuarios
                .AnyAsync(u => u.Id != id && u.Email == dto.Email);

            if (emailJaExiste)
            {
                throw new InvalidOperationException("O e-mail já está em uso.");
            }

            usuarioExistente.Nome = dto.Nome;
            usuarioExistente.Email = dto.Email;
            usuarioExistente.Telefone = dto.Telefone;
            await _dataContext.SaveChangesAsync();

            return Mapear(usuarioExistente);
        }

        public async Task<UsuarioOutputDTO?> ObterPorId(Guid id)
        {
            var usuario = await _dataContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            return usuario is null ? null : Mapear(usuario);
        }

        public async Task<UsuarioOutputDTO?> AtualizarMeAsync(
            Guid usuarioId,
            AtualizarMeInputDTO dto,
            CancellationToken cancellationToken = default)
        {
            var novoNome = dto.Nome;
            if (novoNome is not null)
            {
                novoNome = novoNome.Trim();
                if (novoNome.Length == 0 || novoNome.Length > 150)
                {
                    throw new ValidationException(
                        "O nome deve conter entre 1 e 150 caracteres.");
                }
            }

            string? novoTelefone = null;
            var atualizarTelefone = dto.Telefone.ValueKind != JsonValueKind.Undefined;
            if (atualizarTelefone)
            {
                if (dto.Telefone.ValueKind == JsonValueKind.Null)
                {
                    novoTelefone = null;
                }
                else if (dto.Telefone.ValueKind == JsonValueKind.String)
                {
                    novoTelefone = dto.Telefone.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(novoTelefone) || novoTelefone.Length > 20)
                    {
                        throw new ValidationException(
                            "O telefone deve conter entre 1 e 20 caracteres ou ser null para removê-lo.");
                    }
                }
                else
                {
                    throw new ValidationException(
                        "O telefone deve ser texto, null ou omitido.");
                }
            }

            var usuario = await _dataContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);

            if (usuario is null)
            {
                return null;
            }

            if (novoNome is not null)
            {
                usuario.Nome = novoNome;
            }

            if (atualizarTelefone)
            {
                usuario.Telefone = novoTelefone;
            }

            await _dataContext.SaveChangesAsync(cancellationToken);
            return Mapear(usuario);
        }

        public async Task<UsuarioOutputDTO> Criar(UsuarioInputDTO dto)
        {
            dto.Email = dto.Email.Trim().ToLowerInvariant();
            var emailJaExiste = await _dataContext.Usuarios
                .AnyAsync(u => u.Email == dto.Email);

            if (emailJaExiste)
            {
                throw new InvalidOperationException("O e-mail já está em uso.");
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
            return Mapear(novoUsuario);
        }

        private static UsuarioOutputDTO Mapear(UsuarioModel usuario)
        {
            return new UsuarioOutputDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                CriadoEm = usuario.CriadoEm
            };
        }
    }
}
