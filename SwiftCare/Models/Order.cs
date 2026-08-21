
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }


        // ============================================================
        // ORDER INFORMATION
        // ============================================================

        [Display(Name = "Order number")]
        [ScaffoldColumn(false)]
        public string OrderNumber { get; set; }

        [Display(Name = "Order date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }

        [Display(Name = "Last updated at")]
        public DateTime LastUpdated { get; set; }


        // ============================================================
        // DISPLAY
        // ============================================================

        public string LastFourDigitsOfOrderNumber
        {
            get
            {
                if (string.IsNullOrEmpty(OrderNumber) ||
                    OrderNumber.Length < 4)
                {
                    return OrderNumber;
                }

                return OrderNumber.Substring(
                    OrderNumber.Length - 4
                );
            }
        }


        // ============================================================
        // USER
        // ============================================================

        public string UserId { get; set; }

        public virtual Administrator User { get; set; }


        // ============================================================
        // ORDER ITEMS
        // ============================================================

        public virtual ICollection<OrderItem> OrderItems { get; set; }


        // ============================================================
        // PACKAGES
        // ============================================================

        public virtual ICollection<PackageOrder> PackageOrders { get; set; }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public Order()
        {
            OrderDate = DateTime.Now;

            LastUpdated = DateTime.Now;

            OrderItems = new List<OrderItem>();

            PackageOrders = new List<PackageOrder>();
        }
    }
}

