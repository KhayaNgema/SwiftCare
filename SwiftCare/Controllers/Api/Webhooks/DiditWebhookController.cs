using System.Text;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCare.Data;
using SwiftCare.Models.VolunteerOnboarding;
using SwiftCare.Services.IdentityVerification;

namespace SwiftCare.Controllers.Api.Webhooks
{
    [ApiController]
    [Route("api/webhooks/didit")]
    public class DiditWebhookController : ControllerBase
    {
        private readonly SwiftCareDbContext _context;

        private readonly IDiditWebhookSignatureValidator
            _signatureValidator;

        private readonly IDiditWebhookService
            _webhookService;

        private readonly ILogger<DiditWebhookController>
            _logger;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public DiditWebhookController(
            SwiftCareDbContext context,
            IDiditWebhookSignatureValidator signatureValidator,
            IDiditWebhookService webhookService,
            ILogger<DiditWebhookController> logger)
        {
            _context = context;
            _signatureValidator = signatureValidator;
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult Get()
        {
            _logger.LogInformation(
                "Didit webhook GET endpoint reached.");

            return Ok(new
            {
                success = true,
                message = "SwiftCare Didit webhook endpoint is reachable.",
                endpoint = "/api/webhooks/didit",
                method = "POST"
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Receive()
        {
            _logger.LogInformation(
                "================================================");

            _logger.LogInformation(
                "DIDIT WEBHOOK REQUEST RECEIVED.");

            _logger.LogInformation(
                "Request URL: {Url}",
                $"{Request.Scheme}://{Request.Host}{Request.Path}");

            _logger.LogInformation(
                "Remote IP: {RemoteIp}",
                HttpContext.Connection.RemoteIpAddress);

            _logger.LogInformation(
                "================================================");

            Request.EnableBuffering();

            string rawBody;

            using (
                var reader =
                    new StreamReader(
                        Request.Body,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true))
            {
                rawBody =
                    await reader.ReadToEndAsync();
            }

            Request.Body.Position = 0;

            _logger.LogInformation(
                "Didit webhook raw body received. Length={Length}.",
                rawBody?.Length ?? 0);

            _logger.LogInformation(
                "Didit webhook raw body: {RawBody}",
                rawBody);

            // ---------------------------------------------------------
            // HEADERS
            // ---------------------------------------------------------

            var timestampHeader =
                Request.Headers["X-Timestamp"]
                    .FirstOrDefault();

            var signatureV2 =
                Request.Headers["X-Signature-V2"]
                    .FirstOrDefault();

            var signature =
                Request.Headers["X-Signature"]
                    .FirstOrDefault();

            var signatureSimple =
                Request.Headers["X-Signature-Simple"]
                    .FirstOrDefault();

            _logger.LogInformation(
                "Didit webhook headers. " +
                "X-Timestamp={Timestamp}, " +
                "HasV2={HasV2}, " +
                "HasSignature={HasSignature}, " +
                "HasSimpleSignature={HasSimpleSignature}.",
                timestampHeader,
                !string.IsNullOrWhiteSpace(signatureV2),
                !string.IsNullOrWhiteSpace(signature),
                !string.IsNullOrWhiteSpace(signatureSimple));

            // ---------------------------------------------------------
            // PARSE PAYLOAD FIRST FOR DIAGNOSTICS
            // ---------------------------------------------------------

            DiditWebhookEnvelope? envelope = null;

            try
            {
                envelope =
                    JsonSerializer.Deserialize<DiditWebhookEnvelope>(
                        rawBody,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not deserialize Didit webhook payload.");

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid JSON payload."
                });
            }

            if (envelope == null)
            {
                _logger.LogWarning(
                    "Didit webhook payload deserialized to null.");

                return BadRequest(new
                {
                    success = false,
                    message = "Empty webhook payload."
                });
            }

            _logger.LogInformation(
                "Didit webhook envelope. " +
                "EventId={EventId}, " +
                "WebhookType={WebhookType}, " +
                "SessionId={SessionId}, " +
                "VendorData={VendorData}, " +
                "Status={Status}.",
                envelope.EventId,
                envelope.WebhookType,
                envelope.SessionId,
                envelope.VendorData,
                envelope.Status);

            // ---------------------------------------------------------
            // TIMESTAMP
            // ---------------------------------------------------------

            if (!long.TryParse(
                    timestampHeader,
                    out var timestamp))
            {
                _logger.LogWarning(
                    "Didit webhook rejected: X-Timestamp is missing or invalid.");

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid webhook timestamp."
                });
            }

            if (!_signatureValidator
                    .IsTimestampValid(timestamp))
            {
                _logger.LogWarning(
                    "Didit webhook rejected: timestamp outside allowed window. " +
                    "Timestamp={Timestamp}.",
                    timestamp);

                return Unauthorized(new
                {
                    success = false,
                    message = "Webhook timestamp expired."
                });
            }

            // ---------------------------------------------------------
            // SIGNATURE
            // ---------------------------------------------------------

            var signatureValid = false;

            if (!string.IsNullOrWhiteSpace(signatureV2))
            {
                _logger.LogInformation(
                    "Attempting Didit V2 signature validation.");

                signatureValid =
                    _signatureValidator.VerifyV2(
                        rawBody,
                        signatureV2);

                _logger.LogInformation(
                    "Didit V2 signature validation result: {Valid}.",
                    signatureValid);
            }

            if (!signatureValid &&
                !string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogInformation(
                    "Attempting Didit raw-body signature validation.");

                signatureValid =
                    _signatureValidator.VerifyRawBody(
                        rawBody,
                        signature);

                _logger.LogInformation(
                    "Didit raw-body signature validation result: {Valid}.",
                    signatureValid);
            }

            if (!signatureValid &&
                !string.IsNullOrWhiteSpace(
                    signatureSimple))
            {
                _logger.LogInformation(
                    "Attempting Didit simple signature validation.");

                signatureValid =
                    _signatureValidator.VerifySimple(
                        timestamp,
                        envelope.SessionId,
                        envelope.Status,
                        envelope.WebhookType,
                        signatureSimple);

                _logger.LogInformation(
                    "Didit simple signature validation result: {Valid}.",
                    signatureValid);
            }

            if (!signatureValid)
            {
                _logger.LogWarning(
                    "Didit webhook signature validation FAILED. " +
                    "EventId={EventId}, SessionId={SessionId}.",
                    envelope.EventId,
                    envelope.SessionId);

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid webhook signature."
                });
            }

            _logger.LogInformation(
                "Didit webhook signature VALIDATED successfully.");

            // ---------------------------------------------------------
            // EVENT ID
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    envelope.EventId))
            {
                _logger.LogWarning(
                    "Didit webhook rejected because EventId is missing.");

                return BadRequest(new
                {
                    success = false,
                    message = "Webhook EventId is missing."
                });
            }

            // ---------------------------------------------------------
            // IDEMPOTENCY
            // ---------------------------------------------------------

            var existing =
                await _context.DiditWebhookLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.EventId ==
                             envelope.EventId);

            if (existing != null)
            {
                _logger.LogInformation(
                    "Didit webhook EventId {EventId} already exists as log {WebhookLogId}. Returning OK.",
                    envelope.EventId,
                    existing.Id);

                return Ok(new
                {
                    success = true,
                    duplicate = true,
                    webhookLogId = existing.Id
                });
            }

            // ---------------------------------------------------------
            // CREATE WEBHOOK LOG
            // ---------------------------------------------------------

            var webhookLog =
                new DiditWebhookLog
                {
                    EventId =
                        envelope.EventId,

                    WebhookType =
                        envelope.WebhookType,

                    SessionId =
                        envelope.SessionId,

                    VendorData =
                        envelope.VendorData,

                    Status =
                        envelope.Status,

                    Environment =
                        envelope.Environment,

                    Timestamp =
                        timestamp,

                    ReceivedDate =
                        DateTime.Now,

                    SignatureVerified =
                        true,

                    Processed =
                        false,

                    ProcessingFailed =
                        false,

                    RawBody =
                        rawBody
                };

            _context.DiditWebhookLogs.Add(
                webhookLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Didit webhook log CREATED. " +
                "WebhookLogId={WebhookLogId}, " +
                "EventId={EventId}.",
                webhookLog.Id,
                webhookLog.EventId);

            // ---------------------------------------------------------
            // PROCESS WEBHOOK
            // ---------------------------------------------------------

            try
            {
                /*
                 * For now we process synchronously.
                 *
                 * This is intentional during development so we can
                 * immediately see failures in the request/log output.
                 */

                await _webhookService.ProcessWebhookAsync(
                    webhookLog.Id,
                    HttpContext.RequestAborted);

                _logger.LogInformation(
                    "Didit webhook {WebhookLogId} processed successfully.",
                    webhookLog.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Didit webhook {WebhookLogId} processing failed.",
                    webhookLog.Id);

                /*
                 * We already have the webhook stored, so return 200.
                 *
                 * Didit should not endlessly retry a webhook simply
                 * because our internal processing failed.
                 */

                return Ok(new
                {
                    success = true,
                    received = true,
                    processed = false,
                    webhookLogId = webhookLog.Id
                });
            }

            // ---------------------------------------------------------
            // RETURN
            // ---------------------------------------------------------

            return Ok(new
            {
                success = true,
                received = true,
                processed = true,
                webhookLogId = webhookLog.Id
            });
        }
    }
}