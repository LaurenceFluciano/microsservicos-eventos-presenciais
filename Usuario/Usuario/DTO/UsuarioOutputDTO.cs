namespace Usuario.DTO
{
    public class UsuarioOutputDTO
    {
        public Guid Id { get; set; } 
        public string nome { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? telefone { get; set; } 

    }
}
