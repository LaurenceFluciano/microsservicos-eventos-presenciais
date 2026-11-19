using System.Text.Json;

namespace Usuario.DTO
{
    public sealed class AtualizarMeInputDTO
    {
        public string? Nome { get; set; }

        public JsonElement Telefone { get; set; }
    }
}
