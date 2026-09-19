using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwiftCare.Services.IdentityVerification
{
    public class DiditWebhookService : IDiditWebhookService
    {
        private readonly SwiftCareDbContext _context;
        private readonly ILogger<DiditWebhookService> _logger;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public DiditWebhookService(
            SwiftCareDbContext context,
            ILogger<DiditWebhookService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessWebhookAsync(
            int webhookLogId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "========== DIDIT WEBHOOK PROCESSING STARTED ==========");

            _logger.LogInformation(
                "Processing Didit webhook log {WebhookLogId}.",
                webhookLogId);

            var webhook =
                await _context.DiditWebhookLogs
                    .FirstOrDefaultAsync(
                        x => x.Id == webhookLogId,
                        cancellationToken);

            if (webhook == null)
            {
                _logger.LogWarning(
                    "Didit webhook log {WebhookLogId} was not found.",
                    webhookLogId);

                return;
            }

            if (webhook.Processed)
            {
                _logger.LogInformation(
                    "Didit webhook log {WebhookLogId} was already processed.",
                    webhookLogId);

                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(
                        webhook.RawBody))
                {
                    throw new InvalidOperationException(
                        "Didit webhook RawBody is empty.");
                }

                _logger.LogInformation(
                    "Raw Didit webhook body: {RawBody}",
                    webhook.RawBody);

                var envelope =
                    JsonSerializer.Deserialize<DiditWebhookEnvelope>(
                        webhook.RawBody,
                        JsonOptions);

                if (envelope == null)
                {
                    throw new InvalidOperationException(
                        "Unable to deserialize Didit webhook.");
                }

                _logger.LogInformation(
                    "Didit webhook parsed successfully. " +
                    "EventId={EventId}, " +
                    "Type={WebhookType}, " +
                    "SessionId={SessionId}, " +
                    "VendorData={VendorData}, " +
                    "Status={Status}.",
                    envelope.EventId,
                    envelope.WebhookType,
                    envelope.SessionId,
                    envelope.VendorData,
                    envelope.Status);

                switch (envelope.WebhookType)
                {
                    case "status.updated":

                    case "data.updated":

                        await ProcessSessionEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    case "user.status.updated":

                        await ProcessUserStatusEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    case "user.data.updated":

                        await ProcessUserDataEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    case "business.status.updated":

                    case "business.data.updated":

                        await ProcessBusinessEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    case "activity.created":

                        await ProcessActivityEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    case "transaction.created":

                    case "transaction.status.updated":

                        await ProcessTransactionEventAsync(
                            envelope,
                            cancellationToken);

                        break;

                    default:

                        _logger.LogWarning(
                            "Unsupported Didit webhook type {WebhookType}. " +
                            "EventId={EventId}.",
                            envelope.WebhookType,
                            envelope.EventId);

                        break;
                }

                webhook.Processed = true;
                webhook.ProcessingFailed = false;
                webhook.ProcessedDate =
                    DateTime.Now;
                webhook.ProcessingError = null;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Didit webhook log {WebhookLogId} processed successfully.",
                    webhookLogId);

                _logger.LogInformation(
                    "========== DIDIT WEBHOOK PROCESSING FINISHED ==========");
            }
            catch (Exception ex)
            {
                webhook.ProcessingFailed = true;
                webhook.Processed = false;
                webhook.ProcessingError =
                    ex.Message.Length > 4000
                        ? ex.Message[..4000]
                        : ex.Message;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "FAILED processing Didit webhook {WebhookLogId}.",
                    webhookLogId);

                throw;
            }
        }

        private async Task ProcessSessionEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing Didit session event. " +
                "EventId={EventId}, " +
                "Status={Status}, " +
                "VendorData={VendorData}.",
                envelope.EventId,
                envelope.Status,
                envelope.VendorData);

            if (string.IsNullOrWhiteSpace(
                    envelope.VendorData))
            {
                _logger.LogWarning(
                    "Didit session webhook {EventId} has no vendor_data.",
                    envelope.EventId);

                return;
            }

            var verificationId =
                ExtractVerificationId(
                    envelope.VendorData);

            if (!verificationId.HasValue)
            {
                _logger.LogWarning(
                    "Unable to resolve SwiftCare verification from vendor_data {VendorData}.",
                    envelope.VendorData);

                return;
            }

            _logger.LogInformation(
                "Resolved Didit vendor_data {VendorData} to SwiftCare verification ID {VerificationId}.",
                envelope.VendorData,
                verificationId.Value);

            var verification =
                await _context
                    .VolunteerIdentityVerifications
                    .FirstOrDefaultAsync(
                        x => x.Id == verificationId.Value,
                        cancellationToken);

            if (verification == null)
            {
                _logger.LogWarning(
                    "VolunteerIdentityVerification {VerificationId} was not found.",
                    verificationId.Value);

                return;
            }

            _logger.LogInformation(
                "Found VolunteerIdentityVerification {VerificationId}. " +
                "Current status={CurrentStatus}, " +
                "ProviderSessionId={ProviderSessionId}.",
                verification.Id,
                verification.Status,
                verification.ProviderSessionId);

            if (
                verification.Status ==
                VolunteerIdentityVerificationStatus.Verified &&
                string.Equals(
                    envelope.Status,
                    "In Progress",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Ignoring late In Progress event for already verified verification {VerificationId}.",
                    verification.Id);

                return;
            }

            verification.ProviderSessionId =
                string.IsNullOrWhiteSpace(
                    verification.ProviderSessionId)
                    ? envelope.SessionId
                    : verification.ProviderSessionId;

            switch (envelope.Status)
            {
                case "Approved":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Verified;

                    verification.DocumentVerified =
                        true;

                    verification.VerifiedDate =
                        DateTime.Now;

                    verification.FailureReason = null;

                    ApplyDecisionData(
                        verification,
                        envelope.Decision);

                    verification.ModifiedDate =
                        DateTime.Now;

                    _logger.LogInformation(
                        "Didit verification {VerificationId} marked VERIFIED.",
                        verification.Id);

                    break;

                case "Declined":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Rejected;

                    verification.DocumentVerified =
                        false;

                    verification.FaceMatchVerified =
                        false;

                    verification.LivenessVerified =
                        false;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.FailureReason =
                        ExtractDecisionWarning(
                            envelope.Decision);

                    verification.ModifiedDate =
                        DateTime.Now;

                    _logger.LogWarning(
                        "Didit verification {VerificationId} marked REJECTED. Reason={Reason}.",
                        verification.Id,
                        verification.FailureReason);

                    break;

                case "In Review":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.RequiresReview;

                    verification.ModifiedDate =
                        DateTime.Now;

                    ApplyDecisionData(
                        verification,
                        envelope.Decision);

                    _logger.LogInformation(
                        "Didit verification {VerificationId} marked REQUIRES REVIEW.",
                        verification.Id);

                    break;

                case "In Progress":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.InProgress;

                    verification.ModifiedDate =
                        DateTime.Now;

                    _logger.LogInformation(
                        "Didit verification {VerificationId} marked IN PROGRESS.",
                        verification.Id);

                    break;

                case "Resubmitted":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.AwaitingUser;

                    verification.DocumentVerified =
                        false;

                    verification.LivenessVerified =
                        false;

                    verification.FaceMatchVerified =
                        false;

                    verification.FaceIdentityEnrolled =
                        false;

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;

                case "Abandoned":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Cancelled;

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;

                case "Expired":

                case "KYC Expired":

                    verification.Status =
                        VolunteerIdentityVerificationStatus.Expired;

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;

                default:

                    _logger.LogWarning(
                        "Unhandled Didit session status '{Status}' for verification {VerificationId}.",
                        envelope.Status,
                        verification.Id);

                    verification.ModifiedDate =
                        DateTime.Now;

                    break;
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "VolunteerIdentityVerification {VerificationId} saved. " +
                "New status={Status}, " +
                "DocumentVerified={DocumentVerified}, " +
                "LivenessVerified={LivenessVerified}, " +
                "FaceMatchVerified={FaceMatchVerified}.",
                verification.Id,
                verification.Status,
                verification.DocumentVerified,
                verification.LivenessVerified,
                verification.FaceMatchVerified);
        }

        private static void ApplyDecisionData(
            VolunteerIdentityVerification verification,
            JsonElement? decision)
        {
            if (!decision.HasValue ||
                decision.Value.ValueKind !=
                JsonValueKind.Object)
            {
                return;
            }

            var decisionValue =
                decision.Value;

            if (
                decisionValue.TryGetProperty(
                    "id_verifications",
                    out var idVerifications) &&
                idVerifications.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var idVerification
                    in idVerifications.EnumerateArray())
                {
                    if (
                        idVerification.TryGetProperty(
                            "status",
                            out var idStatus) &&
                        string.Equals(
                            idStatus.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.DocumentVerified =
                            true;
                    }
                }
            }

            if (
                decisionValue.TryGetProperty(
                    "liveness_checks",
                    out var livenessChecks) &&
                livenessChecks.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var liveness
                    in livenessChecks.EnumerateArray())
                {
                    if (
                        liveness.TryGetProperty(
                            "status",
                            out var statusProperty) &&
                        string.Equals(
                            statusProperty.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.LivenessVerified =
                            true;
                    }
                }
            }

            if (
                decisionValue.TryGetProperty(
                    "face_matches",
                    out var faceMatches) &&
                faceMatches.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (
                    var faceMatch
                    in faceMatches.EnumerateArray())
                {
                    if (
                        faceMatch.TryGetProperty(
                            "status",
                            out var statusProperty) &&
                        string.Equals(
                            statusProperty.GetString(),
                            "Approved",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        verification.FaceMatchVerified =
                            true;
                    }
                }
            }

            if (
                verification.DocumentVerified &&
                verification.LivenessVerified &&
                verification.FaceMatchVerified &&
                !string.IsNullOrWhiteSpace(
                    verification.ProviderPersonId))
            {
                verification.FaceIdentityEnrolled =
                    true;
            }
        }

        private static string? ExtractDecisionWarning(
            JsonElement? decision)
        {
            if (!decision.HasValue ||
                decision.Value.ValueKind !=
                JsonValueKind.Object)
            {
                return null;
            }

            if (
                !decision.Value.TryGetProperty(
                    "warnings",
                    out var warnings))
            {
                return null;
            }

            if (
                warnings.ValueKind !=
                JsonValueKind.Array)
            {
                return null;
            }

            var values =
                warnings.EnumerateArray()
                    .Select(x =>
                        x.ValueKind ==
                        JsonValueKind.String
                            ? x.GetString()
                            : x.ToString())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

            return values.Count == 0
                ? null
                : string.Join(
                    "; ",
                    values);
        }

        private Task ProcessUserStatusEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit user status event {EventId}: {Status}.",
                envelope.EventId,
                envelope.Status);

            return Task.CompletedTask;
        }

        private Task ProcessUserDataEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit user data event {EventId}.",
                envelope.EventId);

            return Task.CompletedTask;
        }

        private Task ProcessBusinessEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit business event {EventId}: {WebhookType}.",
                envelope.EventId,
                envelope.WebhookType);

            return Task.CompletedTask;
        }

        private Task ProcessActivityEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit activity event {EventId}.",
                envelope.EventId);

            return Task.CompletedTask;
        }

        private Task ProcessTransactionEventAsync(
            DiditWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Didit transaction event {EventId}: {Status}.",
                envelope.EventId,
                envelope.Status);

            return Task.CompletedTask;
        }

        private static int? ExtractVerificationId(
            string vendorData)
        {
            const string prefix =
                "SWIFTCARE-ID-";

            if (!vendorData.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var value =
                vendorData[prefix.Length..];

            return int.TryParse(
                value,
                out var id)
                ? id
                : null;
        }
    }

    public class DiditWebhookEnvelope
    {
        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }

        [JsonPropertyName("webhook_type")]
        public string? WebhookType { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("application_id")]
        public string? ApplicationId { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("business_session_id")]
        public string? BusinessSessionId { get; set; }

        [JsonPropertyName("session_kind")]
        public string? SessionKind { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("workflow_id")]
        public string? WorkflowId { get; set; }

        [JsonPropertyName("workflow_version")]
        public int? WorkflowVersion { get; set; }

        [JsonPropertyName("vendor_data")]
        public string? VendorData { get; set; }

        [JsonPropertyName("vendor_user_id")]
        public string? VendorUserId { get; set; }

        [JsonPropertyName("vendor_business_id")]
        public string? VendorBusinessId { get; set; }

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; set; }

        [JsonPropertyName("decision")]
        public JsonElement? Decision { get; set; }

        [JsonPropertyName("resubmit_info")]
        public JsonElement? ResubmitInfo { get; set; }

        [JsonPropertyName("changes")]
        public JsonElement? Changes { get; set; }

        [JsonPropertyName("changed_fields")]
        public List<string>? ChangedFields { get; set; }
    }
}