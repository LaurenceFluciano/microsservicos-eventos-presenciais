using System.ComponentModel.DataAnnotations;

namespace Usuario.DTO
{
    public class VerificarEmailInputDTO
    {
        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;
    }
}
