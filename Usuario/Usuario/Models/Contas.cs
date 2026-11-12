using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Usuario.Models
{
    [Table("contas")]
    public class ContasModel
    {

        public Guid Id { get; set; } = Guid.CreateVersion7();

        public string Senha_hash { get; set; } = string.Empty;

        public bool Email_verificado { get; set; } = false;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public UsuarioModel? Usuario { get; set; }

    }
}
