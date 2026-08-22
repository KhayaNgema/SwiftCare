using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class DailyCare
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CareId {get; set; }

        [ForeignKey(nameof(NurseId))]
        public string NurseId { get; set; }
        public virtual Nurse Nurse { get; set; }

        public DateTime RecordedAt { get; set; }

        public string CareNotes { get; set; }

        [ForeignKey(nameof(ResidentId))]
        public string ResidentId { get; set; }
        public virtual Resident Resident { get; set; }
    }
}
