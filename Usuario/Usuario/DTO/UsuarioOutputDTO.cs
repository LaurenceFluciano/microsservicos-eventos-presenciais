namespace Usuario.DTO
{
    public class UsuarioOutputDTO
    {
        public Guid Id { get; set; } 
       
        public string Nome { get; set; } = string.Empty;
       
        public string Email { get; set; } = string.Empty;

        public string? Telefone { get; set; }

        public DateTime CriadoEm { get; set; }

    }
}
