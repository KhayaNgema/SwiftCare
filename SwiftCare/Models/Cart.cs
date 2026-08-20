using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }

        // ============================================================
        // ADMIN / USER WHO OWNS THE CART
        // ============================================================

        [Required]
        public string UserId { get; set; }

        public virtual Administrator Administrator { get; set; }

        // ============================================================
        // CREATED DATE
        // ============================================================

        public DateTime CreatedAt { get; set; }

        // ============================================================
        // CART ITEMS
        // ============================================================

        public virtual List<CartItem> Items { get; set; }

        public Cart()
        {
            CreatedAt = DateTime.Now;
            Items = new List<CartItem>();
        }

        // ============================================================
        // ADD EQUIPMENT
        // ============================================================

        public void AddItem(Equipment equipment, int quantity)
        {
            if (equipment == null || quantity <= 0)
            {
                return;
            }

            var existingItem = Items.FirstOrDefault(
                item => item.EquipmentId == equipment.EquipmentId
            );

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    EquipmentId = equipment.EquipmentId,
                    Equipment = equipment,
                    Quantity = quantity
                });
            }
        }

        // ============================================================
        // REMOVE EQUIPMENT
        // ============================================================

        public void RemoveItem(int equipmentId)
        {
            var itemToRemove = Items.FirstOrDefault(
                item => item.EquipmentId == equipmentId
            );

            if (itemToRemove != null)
            {
                Items.Remove(itemToRemove);
            }
        }

        // ============================================================
        // UPDATE QUANTITY
        // ============================================================

        public void UpdateQuantity(int equipmentId, int quantity)
        {
            var itemToUpdate = Items.FirstOrDefault(
                item => item.EquipmentId == equipmentId
            );

            if (itemToUpdate == null)
            {
                return;
            }

            if (quantity <= 0)
            {
                Items.Remove(itemToUpdate);
            }
            else
            {
                itemToUpdate.Quantity = quantity;
            }
        }

        // ============================================================
        // CLEAR CART
        // ============================================================

        public void Clear()
        {
            Items.Clear();
        }

        // ============================================================
        // TOTAL QUANTITY
        // ============================================================

        public int GetTotalItems()
        {
            return Items.Sum(item => item.Quantity);
        }

        // ============================================================
        // EMPTY CHECK
        // ============================================================

        public bool IsEmpty()
        {
            return Items == null || !Items.Any();
        }
    }
}