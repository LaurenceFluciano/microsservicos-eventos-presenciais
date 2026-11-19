namespace Usuario.Servicos
{
    public sealed class EmailSenderNotConfigured : IEmailSender
    {
        public bool IsConfigured => false;

        public Task SendVerificationAsync(
            string email,
            string token,
            CancellationToken cancellationToken = default)
        {
            throw new EmailSenderNotConfiguredException();
        }
    }
}
