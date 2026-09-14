using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models.VolunteerOnboarding
{
    public class VolunteerPolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VolunteerPolicyId { get; set; }

        // ============================================================
        // POLICY
        // ============================================================

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(30)]
        public string Version { get; set; } = "1.0";

        // ============================================================
        // DOCUMENT
        // ============================================================

        [StringLength(500)]
        public string? DocumentPath { get; set; }

        [StringLength(100)]
        public string? DocumentFileName { get; set; }

        // ============================================================
        // SETTINGS
        // ============================================================

        [Required]
        public bool IsRequired { get; set; } = true;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? PublishedDate { get; set; }

        // ============================================================
        // AUDIT
        // ============================================================

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(450)]
        public string? ModifiedBy { get; set; }

        // ============================================================
        // NAVIGATION
        // ============================================================

        public virtual ICollection<VolunteerPolicyAcceptance> Acceptances { get; set; }
            = new List<VolunteerPolicyAcceptance>();
    }
}