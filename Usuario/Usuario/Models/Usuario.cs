using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Usuario.Models
{
    [Table("usuarios")]
    public class UsuarioModel 
    {

        public Guid Id { get; set; } = Guid.CreateVersion7()

       
        public string Nome { get; set; } = string.Empty;


        public string Email { get; set; } = string.Empty;


        public string? Telefone { get; set; }
        
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public ContaModel? Conta { get; set; }
    }
}
