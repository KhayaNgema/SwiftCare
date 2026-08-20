using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class CartItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartItemId { get; set; }

        public int EquipmentId { get; set; }
        public virtual Equipment Equipment { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal { get; set; }

        public bool Deleted { get; set; }

        public int CartId { get; set; }
        public virtual Cart Cart { get; set; }
    }
}
