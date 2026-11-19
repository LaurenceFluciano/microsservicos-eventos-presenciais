using System.ComponentModel.DataAnnotations;

namespace Usuario.DTO
{
    public class CriarContaInputDTO
    {
        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string Senha { get; set; } = string.Empty;
    }
}
