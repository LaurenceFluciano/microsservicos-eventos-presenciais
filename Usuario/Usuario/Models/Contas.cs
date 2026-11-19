using System.ComponentModel.DataAnnotations.Schema;

namespace Usuario.Models
{
    [Table("contas")]
    public class ContaModel
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public Guid UsuarioId { get; set; }

        public string SenhaHash { get; set; } = string.Empty;

        public bool EmailVerificado { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public UsuarioModel Usuario { get; set; } = null!;
    }
}
