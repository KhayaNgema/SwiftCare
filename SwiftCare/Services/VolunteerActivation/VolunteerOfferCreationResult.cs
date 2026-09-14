using SwiftCare.Models.VolunteerOnboarding;

public class VolunteerOfferCreationResult
{
    public VolunteerOffer Offer { get; set; } = null!;

    public string RawAcceptanceToken { get; set; } = string.Empty;
}