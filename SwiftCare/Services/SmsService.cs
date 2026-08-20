using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

public class SmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _accountSid = configuration["Twilio:AccountSid"]
            ?? throw new InvalidOperationException(
                "Twilio AccountSid is not configured.");

        _authToken = configuration["Twilio:AuthToken"]
            ?? throw new InvalidOperationException(
                "Twilio AuthToken is not configured.");

        _fromNumber = configuration["Twilio:FromNumber"]
            ?? throw new InvalidOperationException(
                "Twilio FromNumber is not configured.");

        _logger = logger;

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task SendSmsAsync(string to, string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException(
                    "Recipient phone number is required.",
                    nameof(to));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "SMS message is required.",
                    nameof(message));
            }

            _logger.LogInformation(
                "Attempting to send SMS to {PhoneNumber}",
                to);

            var result = await MessageResource.CreateAsync(
                to: new PhoneNumber(to),
                from: new PhoneNumber(_fromNumber),
                body: message
            );

            _logger.LogInformation(
                "SMS sent successfully. SID: {Sid}, Status: {Status}",
                result.Sid,
                result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SMS to {PhoneNumber}",
                to);

            throw;
        }
    }
}