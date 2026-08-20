using SwiftCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemId { get; set; }

        // ============================================================
        // ORDER
        // ============================================================

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; }

        // ============================================================
        // EQUIPMENT
        // ============================================================

        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public virtual Equipment Equipment { get; set; }

        // ============================================================
        // QUANTITY
        // ============================================================

        [Required]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        public bool IsPackaged { get; set; }
    }
}