namespace Usuario.Servicos
{
    public interface IEmailSender
    {
        bool IsConfigured { get; }

        Task SendVerificationAsync(
            string email,
            string token,
            CancellationToken cancellationToken = default);
    }

    public sealed class EmailSenderNotConfiguredException : Exception
    {
        public EmailSenderNotConfiguredException()
            : base("O envio de e-mail não está configurado neste ambiente.")
        {
        }
    }
}
