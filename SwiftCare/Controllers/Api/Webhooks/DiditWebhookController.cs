using System.Text;
using System.Text.Json;
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
        private readonly IDiditWebhookSignatureValidator _signatureValidator;
        private readonly IDiditWebhookService _webhookService;
        private readonly ILogger<DiditWebhookController> _logger;

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

        // ============================================================
        // GET - DIDIT REACHABILITY TEST
        // ============================================================

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult Get()
        {
            _logger.LogInformation(
                "==================================================");

            _logger.LogInformation(
                "DIDIT WEBHOOK GET ENDPOINT REACHED.");

            _logger.LogInformation(
                "URL: {Url}",
                $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

            _logger.LogInformation(
                "==================================================");

            return Ok(new
            {
                success = true,
                message = "SwiftCare Didit webhook endpoint is reachable.",
                endpoint = "/api/webhooks/didit",
                method = "POST"
            });
        }

        // ============================================================
        // POST - ACTUAL DIDIT WEBHOOK
        // ============================================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Receive()
        {
            _logger.LogCritical(
                "############################################################");

            _logger.LogCritical(
                "🔥 DIDIT POST WEBHOOK RECEIVED 🔥");

            _logger.LogCritical(
                "Request URL: {Url}",
                $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

            _logger.LogCritical(
                "Remote IP: {RemoteIp}",
                HttpContext.Connection.RemoteIpAddress);

            _logger.LogCritical(
                "Content-Type: {ContentType}",
                Request.ContentType);

            _logger.LogCritical(
                "Content-Length: {ContentLength}",
                Request.ContentLength);

            _logger.LogCritical(
                "############################################################");

            // ========================================================
            // ENABLE REQUEST BODY BUFFERING
            // ========================================================

            Request.EnableBuffering();

            string rawBody;

            try
            {
                using var reader = new StreamReader(
                    Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                rawBody = await reader.ReadToEndAsync();

                Request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DIDIT WEBHOOK: Failed to read request body.");

                return BadRequest(new
                {
                    success = false,
                    message = "Could not read webhook body."
                });
            }

            _logger.LogCritical(
                "DIDIT POST BODY RECEIVED. Length={Length}",
                rawBody?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                _logger.LogError(
                    "DIDIT WEBHOOK BODY IS EMPTY.");

                return BadRequest(new
                {
                    success = false,
                    message = "Webhook body is empty."
                });
            }

            // ========================================================
            // DESERIALIZE PAYLOAD
            // ========================================================

            DiditWebhookEnvelope? envelope;

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
                    "DIDIT WEBHOOK JSON DESERIALIZATION FAILED.");

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid JSON payload."
                });
            }

            if (envelope == null)
            {
                _logger.LogError(
                    "DIDIT WEBHOOK DESERIALIZED TO NULL.");

                return BadRequest(new
                {
                    success = false,
                    message = "Webhook payload is empty."
                });
            }

            _logger.LogCritical(
                "DIDIT PAYLOAD PARSED. " +
                "EventId={EventId}, " +
                "WebhookType={WebhookType}, " +
                "SessionId={SessionId}, " +
                "VendorData={VendorData}, " +
                "Status={Status}",
                envelope.EventId,
                envelope.WebhookType,
                envelope.SessionId,
                envelope.VendorData,
                envelope.Status);

            // ========================================================
            // TIMESTAMP
            // ========================================================

            var timestampHeader =
                Request.Headers["X-Timestamp"]
                    .FirstOrDefault();

            if (!long.TryParse(
                    timestampHeader,
                    out var timestamp))
            {
                _logger.LogWarning(
                    "DIDIT WEBHOOK REJECTED: " +
                    "X-Timestamp is missing or invalid. " +
                    "Value={Timestamp}",
                    timestampHeader);

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid webhook timestamp."
                });
            }

            if (!_signatureValidator.IsTimestampValid(timestamp))
            {
                _logger.LogWarning(
                    "DIDIT WEBHOOK REJECTED: " +
                    "Timestamp outside allowed window. " +
                    "Timestamp={Timestamp}",
                    timestamp);

                return Unauthorized(new
                {
                    success = false,
                    message = "Webhook timestamp expired."
                });
            }

            _logger.LogInformation(
                "DIDIT TIMESTAMP VALIDATED.");

            // ========================================================
            // SIGNATURE HEADERS
            // ========================================================

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
                "DIDIT SIGNATURE HEADERS. " +
                "HasV2={HasV2}, " +
                "HasSignature={HasSignature}, " +
                "HasSimpleSignature={HasSimpleSignature}",
                !string.IsNullOrWhiteSpace(signatureV2),
                !string.IsNullOrWhiteSpace(signature),
                !string.IsNullOrWhiteSpace(signatureSimple));

            // ========================================================
            // SIGNATURE VALIDATION
            // ========================================================

            var signatureValid = false;

            // --------------------------------------------------------
            // V2 SIGNATURE
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(signatureV2))
            {
                _logger.LogInformation(
                    "Trying Didit V2 signature.");

                signatureValid =
                    _signatureValidator.VerifyV2(
                        rawBody,
                        signatureV2);

                _logger.LogInformation(
                    "Didit V2 signature result: {Valid}",
                    signatureValid);
            }

            // --------------------------------------------------------
            // RAW BODY SIGNATURE
            // --------------------------------------------------------

            if (!signatureValid &&
                !string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogInformation(
                    "Trying Didit raw-body signature.");

                signatureValid =
                    _signatureValidator.VerifyRawBody(
                        rawBody,
                        signature);

                _logger.LogInformation(
                    "Didit raw-body signature result: {Valid}",
                    signatureValid);
            }

            // --------------------------------------------------------
            // SIMPLE SIGNATURE
            // --------------------------------------------------------

            if (!signatureValid &&
                !string.IsNullOrWhiteSpace(signatureSimple))
            {
                _logger.LogInformation(
                    "Trying Didit simple signature.");

                signatureValid =
                    _signatureValidator.VerifySimple(
                        timestamp,
                        envelope.SessionId,
                        envelope.Status,
                        envelope.WebhookType,
                        signatureSimple);

                _logger.LogInformation(
                    "Didit simple signature result: {Valid}",
                    signatureValid);
            }

            if (!signatureValid)
            {
                _logger.LogError(
                    "DIDIT WEBHOOK SIGNATURE VALIDATION FAILED. " +
                    "EventId={EventId}, SessionId={SessionId}",
                    envelope.EventId,
                    envelope.SessionId);

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid webhook signature."
                });
            }

            _logger.LogCritical(
                "############################################################");

            _logger.LogCritical(
                "🔥 DIDIT SIGNATURE VALIDATED 🔥");

            _logger.LogCritical(
                "############################################################");

            // ========================================================
            // EVENT ID
            // ========================================================

            if (string.IsNullOrWhiteSpace(envelope.EventId))
            {
                _logger.LogError(
                    "DIDIT WEBHOOK REJECTED: EventId missing.");

                return BadRequest(new
                {
                    success = false,
                    message = "Webhook EventId is missing."
                });
            }

            // ========================================================
            // CHECK DUPLICATE WEBHOOK
            // ========================================================

            var existingWebhook =
                await _context.DiditWebhookLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.EventId == envelope.EventId);

            if (existingWebhook != null)
            {
                _logger.LogWarning(
                    "DIDIT WEBHOOK ALREADY EXISTS. " +
                    "EventId={EventId}, " +
                    "WebhookLogId={WebhookLogId}, " +
                    "Processed={Processed}, " +
                    "ProcessingFailed={ProcessingFailed}",
                    envelope.EventId,
                    existingWebhook.Id,
                    existingWebhook.Processed,
                    existingWebhook.ProcessingFailed);

                return Ok(new
                {
                    success = true,
                    duplicate = true,
                    received = true,
                    processed = existingWebhook.Processed,
                    webhookLogId = existingWebhook.Id
                });
            }

            // ========================================================
            // CREATE WEBHOOK LOG
            // ========================================================

            var webhookLog =
                new DiditWebhookLog
                {
                    EventId = envelope.EventId,

                    WebhookType = envelope.WebhookType,

                    SessionId = envelope.SessionId,

                    VendorData = envelope.VendorData,

                    Status = envelope.Status,

                    Environment = envelope.Environment,

                    Timestamp = timestamp,

                    ReceivedDate = DateTime.Now,

                    SignatureVerified = true,

                    Processed = false,

                    ProcessingFailed = false,

                    RawBody = rawBody
                };

            _context.DiditWebhookLogs.Add(webhookLog);

            await _context.SaveChangesAsync();

            _logger.LogCritical(
                "############################################################");

            _logger.LogCritical(
                "🔥 DIDIT WEBHOOK LOG CREATED 🔥");

            _logger.LogCritical(
                "WebhookLogId={WebhookLogId}",
                webhookLog.Id);

            _logger.LogCritical(
                "EventId={EventId}",
                webhookLog.EventId);

            _logger.LogCritical(
                "Status={Status}",
                webhookLog.Status);

            _logger.LogCritical(
                "############################################################");

            // ========================================================
            // CALL SERVICE
            // ========================================================

            try
            {
                _logger.LogCritical(
                    "############################################################");

                _logger.LogCritical(
                    "🔥🔥🔥 CALLING DIDIT WEBHOOK SERVICE 🔥🔥🔥");

                _logger.LogCritical(
                    "Calling _webhookService.ProcessWebhookAsync");

                _logger.LogCritical(
                    "WebhookLogId={WebhookLogId}",
                    webhookLog.Id);

                _logger.LogCritical(
                    "SessionId={SessionId}",
                    webhookLog.SessionId);

                _logger.LogCritical(
                    "Status={Status}",
                    webhookLog.Status);

                _logger.LogCritical(
                    "############################################################");

                await _webhookService.ProcessWebhookAsync(
                    webhookLog.Id,
                    CancellationToken.None);

                _logger.LogCritical(
                    "############################################################");

                _logger.LogCritical(
                    "🔥🔥🔥 DIDIT WEBHOOK SERVICE FINISHED 🔥🔥🔥");

                _logger.LogCritical(
                    "WebhookLogId={WebhookLogId}",
                    webhookLog.Id);

                _logger.LogCritical(
                    "############################################################");

                return Ok(new
                {
                    success = true,
                    received = true,
                    processed = true,
                    webhookLogId = webhookLog.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    "############################################################");

                _logger.LogCritical(
                    "🔥🔥🔥 DIDIT WEBHOOK SERVICE FAILED 🔥🔥🔥");

                _logger.LogCritical(
                    "WebhookLogId={WebhookLogId}",
                    webhookLog.Id);

                _logger.LogCritical(
                    "EventId={EventId}",
                    webhookLog.EventId);

                _logger.LogCritical(
                    "SessionId={SessionId}",
                    webhookLog.SessionId);

                _logger.LogCritical(
                    "Status={Status}",
                    webhookLog.Status);

                _logger.LogCritical(
                    "############################################################");

                // Mark webhook as failed.
                try
                {
                    webhookLog.Processed = false;
                    webhookLog.ProcessingFailed = true;

                    await _context.SaveChangesAsync(
                        CancellationToken.None);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(
                        dbEx,
                        "Failed to mark Didit webhook as ProcessingFailed. " +
                        "WebhookLogId={WebhookLogId}",
                        webhookLog.Id);
                }

                // Return 500 while debugging.
                // This makes the failure visible.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        received = true,
                        processed = false,
                        webhookLogId = webhookLog.Id,
                        message = "Webhook service processing failed."
                    });
            }
        }
    }
}