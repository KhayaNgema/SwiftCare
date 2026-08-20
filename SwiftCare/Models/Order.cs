using SwiftCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class Order
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Display(Name = "Order number")]
        [ScaffoldColumn(false)]
        public string OrderNumber { get; set; }

        [Display(Name = "Order date")]
        public DateTime OrderDate { get; set; }


        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }

        [Display(Name = "Last updated at")]
        public DateTime LastUpdated { get; set; }


        public string LastFourDigitsOfOrderNumber
        {
            get
            {
                return OrderNumber.Substring(OrderNumber.Length - 4);
            }
        }

        public string UserId { get; set; }
        public virtual Administrator User { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }

        public Order()
        {
            OrderDate = DateTime.Now;
        }
    }

}
