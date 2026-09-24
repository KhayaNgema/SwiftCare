namespace SwiftCare.Configuration
{
    public class DiditOptions
    {
        public const string SectionName = "Didit";

        public string BaseUrl { get; set; } =
            "https://verification.didit.me/";

        public string ApiKey { get; set; } =
            string.Empty;

        public string WebhookSecret { get; set; } =
            string.Empty;

        // ------------------------------------------------------------
        // NORMAL IDENTITY VERIFICATION WORKFLOW
        // ------------------------------------------------------------
        //
        // This workflow should contain:
        //
        // ID Verification
        // Passive Liveness
        // Face Match
        // Face Search 1:N
        //
        // The Face Search 1:N module is what allows the verified
        // onboarding face to become part of Didit's searchable
        // biometric population.
        //
        // ------------------------------------------------------------

        public string WorkflowId { get; set; } =
            string.Empty;

        // ------------------------------------------------------------
        // BIOMETRIC LOGIN WORKFLOW
        // ------------------------------------------------------------
        //
        // This workflow should contain:
        //
        // Passive Liveness
        // Face Search 1:N
        //
        // It searches the same Didit biometric population created
        // during onboarding.
        //
        // ------------------------------------------------------------

        public string BiometricWorkflowId { get; set; } =
            string.Empty;

        public string CallbackUrl { get; set; } =
            string.Empty;

        public bool Enabled { get; set; } = true;

        // ------------------------------------------------------------
        // FACE SEARCH THRESHOLD
        // ------------------------------------------------------------

        public int FaceSearchSimilarityThreshold { get; set; } = 70;
    }
}