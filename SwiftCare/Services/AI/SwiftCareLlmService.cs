using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SwiftCare.Configuration;
using System;
using System.ClientModel;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.AI
{
    public class SwiftCareLlmService : ISwiftCareLlmService
    {
        private readonly SwiftCareLlmOptions _options;
        private readonly ILogger<SwiftCareLlmService> _logger;
        private readonly ChatClient _chatClient;

        public SwiftCareLlmService(
            IOptions<SwiftCareLlmOptions> options,
            ILogger<SwiftCareLlmService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                throw new InvalidOperationException(
                    "AzureFoundry:Endpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.DeploymentName))
            {
                throw new InvalidOperationException(
                    "AzureFoundry:DeploymentName is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "AzureFoundry:ApiKey is not configured.");
            }

            var endpoint =
                NormalizeEndpoint(
                    _options.Endpoint);

            _chatClient =
                new ChatClient(
                    model: _options.DeploymentName,
                    credential: new ApiKeyCredential(
                        _options.ApiKey),
                    options: new OpenAIClientOptions
                    {
                        Endpoint = new Uri(endpoint)
                    });

            _logger.LogInformation(
                "[SWIFTCARE LLM] Initialized. Deployment={Deployment}, Endpoint={Endpoint}",
                _options.DeploymentName,
                endpoint);
        }

        // ============================================================
        // GENERATE
        // ============================================================

        public async Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                throw new ArgumentException(
                    "System prompt cannot be empty.",
                    nameof(systemPrompt));
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                throw new ArgumentException(
                    "User prompt cannot be empty.",
                    nameof(userPrompt));
            }

            _logger.LogInformation(
                "[SWIFTCARE LLM] Sending request to {Deployment}",
                _options.DeploymentName);

            var messages =
                new ChatMessage[]
                {
                    new SystemChatMessage(
                        systemPrompt),

                    new UserChatMessage(
                        userPrompt)
                };

            var chatOptions =
                new ChatCompletionOptions
                {
                    Temperature =
                        _options.Temperature,

                    MaxOutputTokenCount =
                        _options.MaxTokens
                };

            try
            {
                var response =
                    await _chatClient.CompleteChatAsync(
                        messages,
                        chatOptions,
                        cancellationToken);

                var text =
                    response.Value.Content.Count > 0
                        ? response.Value.Content[0].Text
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException(
                        "Azure Foundry returned an empty response.");
                }

                _logger.LogInformation(
                    "[SWIFTCARE LLM] Response received. Length={Length}",
                    text.Length);

                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE LLM] GenerateAsync failed.");

                throw;
            }
        }

        // ============================================================
        // SIMPLE GENERATE
        // ============================================================

        public async Task<string> GenerateAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            return await GenerateAsync(
                """
                You are an AI assistant for the SwiftCare
                nursing-home volunteer management system.

                Provide accurate, concise and useful responses.

                Do not invent information.
                """,
                prompt,
                cancellationToken);
        }

        // ============================================================
        // GENERATE STRUCTURED
        //
        // IMPORTANT:
        // Phi-4-mini-instruct does NOT support Azure Structured
        // Outputs. Therefore we intentionally DO NOT send
        // ChatResponseFormat.CreateJsonSchemaFormat().
        //
        // Instead:
        // 1. Tell the model exactly what JSON to return.
        // 2. Ask for JSON only.
        // 3. Parse/validate the JSON in the caller.
        // ============================================================

        public async Task<string> GenerateStructuredAsync(
            string systemPrompt,
            string userPrompt,
            string jsonSchema,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                throw new ArgumentException(
                    "System prompt cannot be empty.",
                    nameof(systemPrompt));
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                throw new ArgumentException(
                    "User prompt cannot be empty.",
                    nameof(userPrompt));
            }

            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                throw new ArgumentException(
                    "JSON schema cannot be empty.",
                    nameof(jsonSchema));
            }

            _logger.LogInformation(
                "[SWIFTCARE LLM] Sending JSON evaluation request to {Deployment}",
                _options.DeploymentName);

            var enhancedSystemPrompt =
                systemPrompt.Trim()
                + """



                IMPORTANT OUTPUT RULES:

                Return ONLY one valid JSON object.

                Do not use markdown.

                Do not use ```json.

                Do not write explanations before the JSON.

                Do not write explanations after the JSON.

                The JSON must follow the requested structure.

                Every property must be present.

                Use strings for text values.

                Use numbers for numeric values.

                Use true or false for boolean values.

                Do not add properties that were not requested.

                The JSON must be complete and valid.
                """;

            var schemaInstruction =
                """



                REQUIRED JSON STRUCTURE:

                The following JSON schema describes the exact
                structure that your response must follow.

                JSON SCHEMA:

                """
                + jsonSchema
                + """



                Return only the JSON object.
                """;

            var messages =
                new ChatMessage[]
                {
                    new SystemChatMessage(
                        enhancedSystemPrompt),

                    new UserChatMessage(
                        userPrompt + schemaInstruction)
                };

            var chatOptions =
                new ChatCompletionOptions
                {
                    Temperature =
                        0.1f,

                    MaxOutputTokenCount =
                        _options.MaxTokens
                };

            try
            {
                var response =
                    await _chatClient.CompleteChatAsync(
                        messages,
                        chatOptions,
                        cancellationToken);

                if (response.Value.Content.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Azure Foundry returned no content.");
                }

                var text =
                    response.Value.Content[0].Text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException(
                        "Azure Foundry returned an empty JSON response.");
                }

                text =
                    CleanJsonResponse(
                        text);

                _logger.LogInformation(
                    "[SWIFTCARE LLM] JSON response received. Length={Length}",
                    text.Length);

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE LLM] GenerateStructuredAsync failed.");

                throw;
            }
        }

        // ============================================================
        // TEST CONNECTION
        // ============================================================

        public async Task<bool> TestConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response =
                    await GenerateAsync(
                        """
                        You are a connectivity test for SwiftCare.

                        Reply with exactly:
                        CONNECTED

                        Do not add anything else.
                        """,
                        "Test the SwiftCare AI connection.",
                        cancellationToken);

                return response
                    .Trim()
                    .Equals(
                        "CONNECTED",
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SWIFTCARE LLM] Connection test failed.");

                return false;
            }
        }

        // ============================================================
        // CLEAN JSON
        // ============================================================

        private string CleanJsonResponse(
            string response)
        {
            response =
                response.Trim();

            // --------------------------------------------------------
            // Remove markdown JSON fences if Phi returns them anyway.
            // --------------------------------------------------------

            if (response.StartsWith(
                    "```json",
                    StringComparison.OrdinalIgnoreCase))
            {
                response =
                    response.Substring(
                        7);

                var closingFence =
                    response.LastIndexOf(
                        "```",
                        StringComparison.Ordinal);

                if (closingFence >= 0)
                {
                    response =
                        response.Substring(
                            0,
                            closingFence);
                }
            }
            else if (response.StartsWith("```"))
            {
                response =
                    response.Substring(3);

                var closingFence =
                    response.LastIndexOf(
                        "```",
                        StringComparison.Ordinal);

                if (closingFence >= 0)
                {
                    response =
                        response.Substring(
                            0,
                            closingFence);
                }
            }

            response =
                response.Trim();

            // --------------------------------------------------------
            // If there is accidental text before/after the JSON,
            // isolate the outermost JSON object.
            // --------------------------------------------------------

            var firstBrace =
                response.IndexOf('{');

            var lastBrace =
                response.LastIndexOf('}');

            if (firstBrace >= 0 &&
                lastBrace > firstBrace)
            {
                response =
                    response.Substring(
                        firstBrace,
                        lastBrace - firstBrace + 1);
            }

            return response.Trim();
        }

        // ============================================================
        // ENDPOINT NORMALIZATION
        // ============================================================

        private string NormalizeEndpoint(
            string endpoint)
        {
            endpoint =
                endpoint.Trim()
                    .TrimEnd('/');

            if (!endpoint.EndsWith(
                    "/openai/v1",
                    StringComparison.OrdinalIgnoreCase))
            {
                endpoint +=
                    "/openai/v1";
            }

            return endpoint + "/";
        }
    }
}