namespace Usuario.DTO
{
    public sealed class LoginOutputDTO
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public string TokenType { get; set; } = "Bearer";
    }
}