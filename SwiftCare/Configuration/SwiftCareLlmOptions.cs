namespace SwiftCare.Configuration
{
    public class SwiftCareLlmOptions
    {
        public const string SectionName = "AzureFoundry";

        /// <summary>
        /// Foundry resource endpoint.
        /// Example:
        /// https://swiftcare-llm-model-resource.services.ai.azure.com/openai/v1/
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// The deployment name inside Microsoft Foundry.
        /// </summary>
        public string DeploymentName { get; set; } = "Phi-4-mini-instruct";

        /// <summary>
        /// API key for the Foundry resource.
        /// Prefer User Secrets / environment variables in development
        /// rather than committing the key to appsettings.json.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Temperature controls response variability.
        /// Lower values are better for ATS/evaluation workloads.
        /// </summary>
        public float Temperature { get; set; } = 0.2f;

        /// <summary>
        /// Maximum number of output tokens requested.
        /// </summary>
        public int MaxTokens { get; set; } = 2500;

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;
    }
}