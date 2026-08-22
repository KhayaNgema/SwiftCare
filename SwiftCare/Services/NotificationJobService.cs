using Microsoft.Extensions.Logging;

namespace SwiftCare.Services
{
    public class NotificationJobService
    {
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly ILogger<NotificationJobService> _logger;

        public NotificationJobService(
            EmailService emailService,
            SmsService smsService,
            ILogger<NotificationJobService> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        // ============================================================
        // SEND EMAIL USING HANGFIRE
        // ============================================================

        public async Task SendEmailAsync(
            string email,
            string subject,
            string body,
            string senderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning(
                        "Email notification was not sent because the recipient email is empty."
                    );

                    return;
                }

                _logger.LogInformation(
                    "Hangfire is sending email to {Email}",
                    email
                );

                await _emailService.SendEmailAsync(
                    to: email,
                    subject: subject,
                    body: body,
                    senderName: senderName
                );

                _logger.LogInformation(
                    "Email successfully sent to {Email}",
                    email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email}",
                    email
                );

                throw;
            }
        }


        // ============================================================
        // SEND SMS USING HANGFIRE
        // ============================================================

        public async Task SendSmsAsync(
            string phoneNumber,
            string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogWarning(
                        "SMS notification was not sent because the phone number is empty."
                    );

                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning(
                        "SMS notification was not sent because the message is empty."
                    );

                    return;
                }

                _logger.LogInformation(
                    "Hangfire is sending SMS to {PhoneNumber}",
                    phoneNumber
                );

                await _smsService.SendSmsAsync(
                    phoneNumber,
                    message
                );

                _logger.LogInformation(
                    "SMS successfully sent to {PhoneNumber}",
                    phoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send SMS to {PhoneNumber}",
                    phoneNumber
                );

                throw;
            }
        }
    }
}