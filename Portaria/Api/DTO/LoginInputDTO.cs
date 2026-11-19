using System.ComponentModel.DataAnnotations;

namespace Usuario.DTO
{
    public class LoginInputDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Senha { get; set; } = string.Empty;
    }
}
