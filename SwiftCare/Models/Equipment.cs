using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class Equipment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EquipmentId { get; set; }

        public string EquipmentName { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }
    }
}
