using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class OurEquipment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EquipmentId { get; set; }

        public string EquipmentName { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public string BarcodeValue { get; set; }

        public string BarcodeImage { get; set; }

        [Required]
        public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

        // ============================================================
        // ASSIGNED TO
        // ============================================================

        public string? AssignedToId { get; set; }

        [ForeignKey(nameof(AssignedToId))]
        public Resident? AssignedTo { get; set; }

        public string? AssignmentNotes { get; set; }

        // ============================================================
        // ASSIGNED BY
        // ============================================================

        public string? AssignedById { get; set; }

        [ForeignKey(nameof(AssignedById))]
        public Nurse? AssignedBy { get; set; }

        public DateTime? AssignedAt { get; set; }
    }

    public enum EquipmentStatus
    {
        [Display(Name = "Available")]
        Available = 0,

        [Display(Name = "In Use")]
        InUse = 1,

        [Display(Name = "Under Maintenance")]
        UnderMaintenance = 2,

        [Display(Name = "Retired")]
        Retired = 3
    }
}