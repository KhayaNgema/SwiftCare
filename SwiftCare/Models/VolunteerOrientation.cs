using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class VolunteerOrientation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerOrientationId { get; set; }

        public DateTime? OrientationDate { get; set; }

        public bool Completed { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string? ConductedById { get; set; }

        public string VolunteerId { get; set; }

        [ForeignKey(nameof(VolunteerId))]
        public Volunteer? Volunteer { get; set; }
    }
}