namespace SwiftCare.ViewModels
{
    public class GeoNamesCityViewModel
    {
        public int GeoNameId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}