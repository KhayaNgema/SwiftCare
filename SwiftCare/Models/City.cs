using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.Models
{
    public class City
    {
        [Key]
        public int CityId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(2)]
        public string CountryCode { get; set; } = "ZA";

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? GeoNameId { get; set; }

        public ICollection<VolunteerOpportunity> VolunteerOpportunities { get; set; }
            = new List<VolunteerOpportunity>();
    }
}