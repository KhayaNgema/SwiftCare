namespace SwiftCare.Models.VolunteerOnboarding
{
    public enum VolunteerOnboardingStatus
    {
        OfferPending = 1,
        OfferAccepted = 2,
        OfferDeclined = 3,
        PoliciesPending = 4,
        ContractPending = 5,
        DocumentsPending = 6,
        AdminVerification = 7,
        RejectedForCorrection = 8,
        ReadyForActivation = 9,
        Completed = 10,
        Cancelled = 11
    }
}