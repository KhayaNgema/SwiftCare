using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class DailyMaintenance
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaintenanceId { get; set; }

        [ForeignKey(nameof(MaintenancePersonId))]
        public string MaintenancePersonId { get; set; }
        public virtual MaintenancePerson MaintenancePerson { get; set; }

        public MaintenanceRequestStatus MaintenanceStatus { get; set; }

        [ForeignKey(nameof(RequestId))]
        public int RequestId { get; set; }
        public virtual MaintenanceRequest MaintenanceRequest { get; set; }

        public DateTime RecordedAt { get; set; }
    }
}
