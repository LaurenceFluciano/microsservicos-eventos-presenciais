using System.ComponentModel.DataAnnotations.Schema;

namespace Usuario.Models
{
    [Table("solicitacoes_cadastro")]
    public class SolicitacaoCadastro
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Telefone { get; set; }

        public string SenhaHash { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime ExpiraEm { get; set; }

        public DateTime? UtilizadoEm { get; set; }
    }
}
