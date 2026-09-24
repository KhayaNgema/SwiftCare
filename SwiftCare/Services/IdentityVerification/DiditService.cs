using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftCare.Configuration;
using SwiftCare.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.IdentityVerification
{
    public class DiditService : IDiditService
    {
        private readonly HttpClient _httpClient;
        private readonly DiditOptions _options;
        private readonly ILogger<DiditService> _logger;
        private readonly EmailService _emailService;

        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        public DiditService(
            HttpClient httpClient,
            IOptions<DiditOptions> options,
            ILogger<DiditService> logger,
            EmailService emailService)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _emailService = emailService;

            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                _httpClient.BaseAddress =
                    new Uri(
                        _options.BaseUrl.TrimEnd('/') + "/");
            }

            _httpClient.Timeout =
                TimeSpan.FromSeconds(60);
        }

        // ============================================================
        // CREATE DIDIT VERIFICATION SESSION
        // ============================================================

        public async Task<DiditVerificationSessionResult>
            CreateVerificationSessionAsync(
                DiditVerificationSessionRequest request,
                CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_options.Enabled)
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit identity verification is disabled."
                };
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit API key is not configured."
                };
            }

            if (string.IsNullOrWhiteSpace(_options.WorkflowId))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "Didit WorkflowId is not configured."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "The SwiftCare verification reference is missing."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        "The applicant email address is missing."
                };
            }

            var baseUrl =
                string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://verification.didit.me/"
                    : _options.BaseUrl.TrimEnd('/') + "/";

            var endpoint =
                $"{baseUrl}v3/session/";

            // ========================================================
            // DIDIT SESSION PAYLOAD
            //
            // IMPORTANT:
            //
            // The WorkflowId configured here must be the onboarding
            // Didit workflow containing:
            //
            // 1. ID Verification
            // 2. Passive Liveness
            // 3. Face Match
            // 4. Face Search 1:N
            //
            // Face Search 1:N is what allows the verified face from
            // this onboarding flow to participate in the biometric
            // population used by later face-login searches.
            // ========================================================

            var payload =
                new DiditCreateSessionRequest
                {
                    WorkflowId =
                        _options.WorkflowId,

                    // ------------------------------------------------
                    // IMPORTANT SWIFTCARE CORRELATION VALUE
                    // ------------------------------------------------
                    //
                    // Example:
                    //
                    // SWIFTCARE-ID-123
                    //
                    // Face Search results must ultimately correlate
                    // back to this value.
                    //
                    VendorData =
                        request.ReferenceId,

                    // ------------------------------------------------
                    // WEBHOOK CALLBACK
                    // ------------------------------------------------

                    Callback =
                        _options.CallbackUrl,

                    CallbackMethod =
                        "both",

                    // ------------------------------------------------
                    // METADATA
                    // ------------------------------------------------

                    Metadata =
                        new DiditSessionMetadata
                        {
                            Source =
                                "SwiftCare",

                            ReferenceId =
                                request.ReferenceId,

                            Purpose =
                                "identity_verification",

                            BiometricEnrollment =
                                "face_search_1_to_n"
                        },

                    Language =
                        "en",

                    // ------------------------------------------------
                    // SWIFTCARE SENDS ITS OWN EMAIL
                    //
                    // Do NOT enable Didit's notification emails.
                    // ------------------------------------------------

                    ContactDetails =
                        new DiditContactDetails
                        {
                            Email =
                                request.Email,

                            SendNotificationEmails =
                                false,

                            EmailLanguage =
                                "en"
                        },

                    // ------------------------------------------------
                    // EXPECTED APPLICANT DETAILS
                    // ------------------------------------------------

                    ExpectedDetails =
                        new DiditExpectedDetails
                        {
                            FirstName =
                                request.FirstName,

                            LastName =
                                request.LastName,

                            DateOfBirth =
                                request.DateOfBirth?
                                    .ToString("yyyy-MM-dd"),

                            IdCountry =
                                request.CountryCode,

                            ExpectedDocumentTypes =
                                new[]
                                {
                                    "ID",
                                    "P"
                                }
                        }
                };

            // ========================================================
            // SERIALIZE REQUEST
            //
            // JsonPropertyName attributes on the DTOs below explicitly
            // control the exact JSON names sent to Didit.
            // ========================================================

            var json =
                JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition =
                            JsonIgnoreCondition.WhenWritingNull
                    });

            _logger.LogInformation(
                "Creating Didit verification session. " +
                "Endpoint: {Endpoint}, " +
                "ReferenceId: {ReferenceId}, " +
                "WorkflowId: {WorkflowId}",
                endpoint,
                request.ReferenceId,
                _options.WorkflowId);

            _logger.LogDebug(
                "Didit session request payload: {Payload}",
                json);

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);

            httpRequest.Headers.Add(
                "x-api-key",
                _options.ApiKey);

            httpRequest.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            HttpResponseMessage response;

            // ========================================================
            // CALL DIDIT
            // ========================================================

            try
            {
                response =
                    await _httpClient.SendAsync(
                        httpRequest,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Didit verification session creation was cancelled.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception while calling Didit Create Session API.");

                return new DiditVerificationSessionResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Could not connect to Didit: {ex.Message}"
                };
            }

            using (response)
            {
                var responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                // ====================================================
                // DEBUG RESPONSE LOG
                // ====================================================

                _logger.LogInformation(
                    "Didit Create Session response. " +
                    "HTTP {StatusCode} ({ReasonPhrase}). " +
                    "Body: {ResponseBody}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    responseBody);

                // ====================================================
                // DIDIT ERROR
                // ====================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Didit Create Session API failed. " +
                        "HTTP {StatusCode}. " +
                        "Response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            $"Didit returned HTTP " +
                            $"{(int)response.StatusCode}: " +
                            responseBody
                    };
                }

                // ====================================================
                // DESERIALIZE DIDIT RESPONSE
                // ====================================================

                DiditSessionResponse? diditResponse;

                try
                {
                    diditResponse =
                        JsonSerializer.Deserialize<DiditSessionResponse>(
                            responseBody,
                            JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not deserialize Didit response. " +
                        "Raw response: {ResponseBody}",
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit returned an invalid JSON response. " +
                            "Check the application logs for the raw response."
                    };
                }

                if (diditResponse == null)
                {
                    _logger.LogError(
                        "Didit returned an empty response body.");

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit returned an empty response."
                    };
                }

                // ====================================================
                // LOG PARSED RESPONSE
                // ====================================================

                _logger.LogInformation(
                    "Parsed Didit response. " +
                    "SessionId={SessionId}, " +
                    "SessionNumber={SessionNumber}, " +
                    "Url={Url}, " +
                    "Status={Status}, " +
                    "WorkflowId={WorkflowId}",
                    diditResponse.SessionId,
                    diditResponse.SessionNumber,
                    diditResponse.Url,
                    diditResponse.Status,
                    diditResponse.WorkflowId);

                // ====================================================
                // SESSION ID REQUIRED
                // ====================================================

                if (string.IsNullOrWhiteSpace(
                        diditResponse.SessionId))
                {
                    _logger.LogError(
                        "Didit returned HTTP {StatusCode}, " +
                        "but session_id was missing. " +
                        "Raw response: {ResponseBody}",
                        (int)response.StatusCode,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit did not return a session ID. " +
                            $"Raw response: {responseBody}"
                    };
                }

                // ====================================================
                // HOSTED VERIFICATION URL REQUIRED
                // ====================================================

                if (string.IsNullOrWhiteSpace(
                        diditResponse.Url))
                {
                    _logger.LogError(
                        "Didit returned session {SessionId}, " +
                        "but verification URL was missing. " +
                        "Raw response: {ResponseBody}",
                        diditResponse.SessionId,
                        responseBody);

                    return new DiditVerificationSessionResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Didit created a session but did not " +
                            "return a verification URL."
                    };
                }

                // ====================================================
                // DIDIT SESSION SUCCESSFULLY CREATED
                // ====================================================

                var result =
                    new DiditVerificationSessionResult
                    {
                        Success = true,

                        SessionId =
                            diditResponse.SessionId,

                        // ------------------------------------------------
                        // For now the Create Session response gives us
                        // the session identifier, so we use it as the
                        // provider verification ID.
                        // ------------------------------------------------

                        VerificationId =
                            diditResponse.SessionId,

                        VerificationUrl =
                            diditResponse.Url,

                        Status =
                            diditResponse.Status
                    };

                _logger.LogInformation(
                    "Didit verification session created successfully. " +
                    "SessionId={SessionId}, " +
                    "VerificationUrl={VerificationUrl}, " +
                    "ReferenceId={ReferenceId}, " +
                    "BiometricEnrollment={BiometricEnrollment}",
                    diditResponse.SessionId,
                    diditResponse.Url,
                    request.ReferenceId,
                    "face_search_1_to_n");

                // ====================================================
                // SEND SWIFTCARE EMAIL
                //
                // IMPORTANT:
                //
                // THIS IS YOUR ORIGINAL EMAIL LOGIC.
                //
                // We are NOT changing it to:
                //
                // SendIdentityVerificationEmailAsync(...)
                //
                // Your existing SendEmailAsync(...) remains intact.
                // ====================================================

                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            request.Email))
                    {
                        _logger.LogInformation(
                            "ABOUT TO SEND SWIFTCARE IDENTITY " +
                            "VERIFICATION EMAIL. " +
                            "Email={Email}, " +
                            "SessionId={SessionId}, " +
                            "VerificationUrl={VerificationUrl}",
                            request.Email,
                            diditResponse.SessionId,
                            diditResponse.Url);

                        var applicantName =
                            $"{request.FirstName} {request.LastName}"
                                .Trim();

                        var emailBody =
                            BuildIdentityVerificationEmailBody(
                                applicantName,
                                diditResponse.Url,
                                request.ReferenceId);

                        // =================================================
                        // KEEPING YOUR ORIGINAL EMAIL LOGIC
                        // =================================================

                        await _emailService.SendEmailAsync(
                            request.Email,
                            "SwiftCare Identity Verification Required",
                            emailBody,
                            "SwiftCare");

                        _logger.LogInformation(
                            "SWIFTCARE IDENTITY VERIFICATION EMAIL " +
                            "SENT SUCCESSFULLY. " +
                            "Email={Email}, " +
                            "SessionId={SessionId}",
                            request.Email,
                            diditResponse.SessionId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Didit session {SessionId} was created, " +
                            "but applicant email was empty. " +
                            "No SwiftCare email was sent.",
                            diditResponse.SessionId);
                    }
                }
                catch (Exception ex)
                {
                    // =================================================
                    // IMPORTANT:
                    //
                    // Didit session creation succeeded.
                    //
                    // Email failure must NOT make the Didit session
                    // fail.
                    // =================================================

                    _logger.LogError(
                        ex,
                        "Didit session {SessionId} was created successfully, " +
                        "but SwiftCare could not send the verification email.",
                        diditResponse.SessionId);

                    result.ErrorMessage =
                        "Didit verification session was created, " +
                        "but SwiftCare could not send the email invitation.";
                }

                return result;
            }
        }

        // ============================================================
        // RESEND EMAIL FOR EXISTING DIDIT SESSION
        // ============================================================

        public async Task<bool> ResendVerificationEmailAsync(
            string email,
            string applicantName,
            string verificationUrl,
            string verificationReference,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because Didit is disabled.");

                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because applicant email is empty.");

                return false;
            }

            if (string.IsNullOrWhiteSpace(verificationUrl))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because verification URL is empty. " +
                    "Reference: {ReferenceId}",
                    verificationReference);

                return false;
            }

            if (string.IsNullOrWhiteSpace(verificationReference))
            {
                _logger.LogWarning(
                    "Cannot resend Didit verification email " +
                    "because verification reference is empty.");

                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var emailSubject =
                    "SwiftCare Identity Verification Required";

                var emailBody =
                    BuildIdentityVerificationEmailBody(
                        applicantName,
                        verificationUrl,
                        verificationReference);

                _logger.LogInformation(
                    "RESENDING SWIFTCARE IDENTITY VERIFICATION EMAIL. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                // =====================================================
                // KEEPING YOUR ORIGINAL RESEND EMAIL LOGIC
                // =====================================================

                await _emailService.SendEmailAsync(
                    email,
                    emailSubject,
                    emailBody,
                    "SwiftCare");

                _logger.LogInformation(
                    "SWIFTCARE IDENTITY VERIFICATION EMAIL " +
                    "RESENT SUCCESSFULLY. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FAILED TO RESEND SWIFTCARE IDENTITY " +
                    "VERIFICATION EMAIL. " +
                    "Recipient: {Email}, " +
                    "Reference: {ReferenceId}",
                    email,
                    verificationReference);

                return false;
            }
        }

        // ============================================================
        // GET VERIFICATION RESULT
        // ============================================================

        public async Task<DiditVerificationResult?>
            GetVerificationResultAsync(
                string providerVerificationId,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    providerVerificationId))
            {
                return null;
            }

            _logger.LogInformation(
                "Didit verification result requested for " +
                "provider verification ID {ProviderVerificationId}.",
                providerVerificationId);

            /*
             * Didit verification status will normally be processed
             * through the webhook.
             *
             * The webhook service updates:
             *
             * - Status
             * - DocumentVerified
             * - LivenessVerified
             * - FaceMatchVerified
             * - FaceIdentityEnrolled
             * - ProviderPersonId
             * - FailureReason
             */

            await Task.CompletedTask;

            return null;
        }

        // ============================================================
        // SWIFTCARE IDENTITY VERIFICATION EMAIL
        // ============================================================

        private static string BuildIdentityVerificationEmailBody(
            string applicantName,
            string verificationUrl,
            string verificationReference)
        {
            var safeApplicantName =
                System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(applicantName)
                        ? "Applicant"
                        : applicantName);

            var safeVerificationUrl =
                System.Net.WebUtility.HtmlEncode(
                    verificationUrl);

            var safeReference =
                System.Net.WebUtility.HtmlEncode(
                    verificationReference);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">

    <meta name=""viewport""
          content=""width=device-width, initial-scale=1.0"">

    <title>SwiftCare Identity Verification</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background-color:#F0FDF4;
    font-family:Arial,Helvetica,sans-serif;
"">

<table width=""100%""
       cellpadding=""0""
       cellspacing=""0""
       border=""0""
       style=""background-color:#F0FDF4;padding:30px 15px;"">

    <tr>
        <td align=""center"">

            <table width=""600""
                   cellpadding=""0""
                   cellspacing=""0""
                   border=""0""
                   style=""
                       max-width:600px;
                       width:100%;
                       background:#ffffff;
                       border-radius:18px;
                       overflow:hidden;
                       border:1px solid #D5E7D6;
                       box-shadow:0 8px 30px rgba(27,94,32,0.12);
                   "">

                <!-- HEADER -->

                <tr>
                    <td style=""
                        background:#1B5E20;
                        padding:28px 30px;
                        text-align:center;
                    "">

                        <div style=""
                            color:#ffffff;
                            font-size:28px;
                            font-weight:bold;
                            letter-spacing:0.5px;
                        "">
                            SwiftCare
                        </div>

                        <div style=""
                            color:#DCEDC8;
                            font-size:14px;
                            margin-top:6px;
                        "">
                            Volunteer Management System
                        </div>

                    </td>
                </tr>

                <!-- CONTENT -->

                <tr>
                    <td style=""
                        padding:35px 35px 20px 35px;
                        color:#333333;
                    "">

                        <h2 style=""
                            margin:0 0 18px 0;
                            color:#1B5E20;
                            font-size:24px;
                        "">
                            Identity Verification Required
                        </h2>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            Hello <strong>{safeApplicantName}</strong>,
                        </p>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            As part of your SwiftCare volunteer onboarding,
                            you are required to complete an identity
                            verification process.
                        </p>

                        <p style=""
                            margin:0 0 20px 0;
                            font-size:15px;
                            line-height:1.7;
                        "">
                            The verification process will securely guide
                            you through the required identity document
                            and facial verification steps.
                        </p>

                        <!-- REFERENCE -->

                        <table width=""100%""
                               cellpadding=""0""
                               cellspacing=""0""
                               border=""0""
                               style=""
                                   background:#F0FDF4;
                                   border:1px solid #D5E7D6;
                                   border-radius:12px;
                                   margin-bottom:25px;
                               "">

                            <tr>
                                <td style=""
                                    padding:16px 18px;
                                    font-size:14px;
                                "">

                                    <strong style=""
                                        color:#1B5E20;
                                    "">
                                        Verification Reference
                                    </strong>

                                    <br />

                                    <span style=""
                                        color:#555555;
                                        font-family:monospace;
                                    "">
                                        {safeReference}
                                    </span>

                                </td>
                            </tr>

                        </table>

                        <!-- BUTTON -->

                        <div style=""
                            text-align:center;
                            margin:30px 0;
                        "">

                            <a href=""{safeVerificationUrl}""
                               target=""_blank""
                               style=""
                                   display:inline-block;
                                   background:#1B5E20;
                                   color:#ffffff;
                                   text-decoration:none;
                                   padding:15px 30px;
                                   border-radius:10px;
                                   font-size:16px;
                                   font-weight:bold;
                               "">

                                Start Identity Verification

                            </a>

                        </div>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:14px;
                            line-height:1.7;
                            color:#555555;
                        "">
                            Please complete the verification using the
                            button above. You may be asked to provide your
                            identity document and complete a facial
                            verification step.
                        </p>

                        <p style=""
                            margin:0 0 15px 0;
                            font-size:14px;
                            line-height:1.7;
                            color:#555555;
                        "">
                            If the button does not work, copy and paste
                            the following link into your browser:
                        </p>

                        <div style=""
                            background:#f7f7f7;
                            border-radius:8px;
                            padding:12px;
                            word-break:break-all;
                            font-size:12px;
                            color:#555555;
                        "">
                            {safeVerificationUrl}
                        </div>

                    </td>
                </tr>

                <!-- SECURITY NOTICE -->

                <tr>
                    <td style=""
                        padding:10px 35px 30px 35px;
                    "">

                        <table width=""100%""
                               cellpadding=""0""
                               cellspacing=""0""
                               border=""0""
                               style=""
                                   background:#FFFDE7;
                                   border:1px solid #E6E0A3;
                                   border-radius:10px;
                               "">

                            <tr>
                                <td style=""
                                    padding:15px 18px;
                                    font-size:13px;
                                    line-height:1.6;
                                    color:#555555;
                                "">

                                    <strong>
                                        Security Notice
                                    </strong>

                                    <br />

                                    SwiftCare will never ask you to send
                                    your identity document or verification
                                    information by replying to this email.

                                </td>
                            </tr>

                        </table>

                    </td>
                </tr>

                <!-- FOOTER -->

                <tr>
                    <td style=""
                        background:#124116;
                        padding:22px 30px;
                        text-align:center;
                        color:#DCEDC8;
                        font-size:12px;
                        line-height:1.6;
                    "">

                        <strong style=""color:#ffffff;"">
                            SwiftCare
                        </strong>

                        <br />

                        Volunteer Identity Verification

                        <br />

                        This is an automated message.
                        Please do not reply to this email.

                    </td>
                </tr>

            </table>

        </td>
    </tr>

</table>

</body>
</html>";
        }

        // ============================================================
        // DIDIT CREATE SESSION REQUEST
        // ============================================================

        private sealed class DiditCreateSessionRequest
        {
            [JsonPropertyName("workflow_id")]
            public string WorkflowId { get; set; } = string.Empty;

            [JsonPropertyName("vendor_data")]
            public string VendorData { get; set; } = string.Empty;

            [JsonPropertyName("callback")]
            public string? Callback { get; set; }

            [JsonPropertyName("callback_method")]
            public string CallbackMethod { get; set; } = "both";

            [JsonPropertyName("metadata")]
            public DiditSessionMetadata Metadata { get; set; } = new();

            [JsonPropertyName("language")]
            public string Language { get; set; } = "en";

            [JsonPropertyName("contact_details")]
            public DiditContactDetails ContactDetails { get; set; } = new();

            [JsonPropertyName("expected_details")]
            public DiditExpectedDetails ExpectedDetails { get; set; } = new();
        }

        // ============================================================
        // DIDIT METADATA
        // ============================================================

        private sealed class DiditSessionMetadata
        {
            [JsonPropertyName("source")]
            public string Source { get; set; } = "SwiftCare";

            [JsonPropertyName("reference_id")]
            public string ReferenceId { get; set; } = string.Empty;

            [JsonPropertyName("purpose")]
            public string Purpose { get; set; } =
                "identity_verification";

            [JsonPropertyName("biometric_enrollment")]
            public string BiometricEnrollment { get; set; } =
                "face_search_1_to_n";
        }

        // ============================================================
        // DIDIT CONTACT DETAILS
        // ============================================================

        private sealed class DiditContactDetails
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("send_notification_emails")]
            public bool SendNotificationEmails { get; set; }

            [JsonPropertyName("email_lang")]
            public string EmailLanguage { get; set; } = "en";
        }

        // ============================================================
        // DIDIT EXPECTED DETAILS
        // ============================================================

        private sealed class DiditExpectedDetails
        {
            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; }

            [JsonPropertyName("date_of_birth")]
            public string? DateOfBirth { get; set; }

            [JsonPropertyName("id_country")]
            public string? IdCountry { get; set; }

            [JsonPropertyName("expected_document_types")]
            public string[] ExpectedDocumentTypes { get; set; } =
                Array.Empty<string>();
        }

        // ============================================================
        // DIDIT CREATE SESSION RESPONSE
        // ============================================================

        private sealed class DiditSessionResponse
        {
            [JsonPropertyName("session_id")]
            public string? SessionId { get; set; }

            [JsonPropertyName("session_number")]
            public int? SessionNumber { get; set; }

            [JsonPropertyName("session_token")]
            public string? SessionToken { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("vendor_data")]
            public string? VendorData { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("workflow_id")]
            public string? WorkflowId { get; set; }

            [JsonPropertyName("workflow_version")]
            public int? WorkflowVersion { get; set; }

            [JsonPropertyName("callback")]
            public string? Callback { get; set; }
        }
    }
}