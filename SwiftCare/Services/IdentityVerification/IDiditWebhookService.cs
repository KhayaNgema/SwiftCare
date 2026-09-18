namespace SwiftCare.Services.IdentityVerification
{
    public interface IDiditWebhookService
    {
        Task ProcessWebhookAsync(
            int webhookLogId,
            CancellationToken cancellationToken = default);
    }
}