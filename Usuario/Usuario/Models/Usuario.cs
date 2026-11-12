using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Usuario.Models
{
    [Table("Usuarios")]
    public class UsuarioModel 
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }
    }
}
