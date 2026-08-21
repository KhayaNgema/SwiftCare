
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SwiftCare.Models
{
    public class PackageOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PackageOrderId { get; set; }


        // ============================================================
        // ORDER RELATIONSHIP
        // ============================================================

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; }


        // ============================================================
        // ORDER ITEM RELATIONSHIP
        // ============================================================

        [Required]
        public int OrderItemId { get; set; }

        [ForeignKey(nameof(OrderItemId))]
        public virtual OrderItem OrderItem { get; set; }


        // ============================================================
        // PACKAGE INFORMATION
        // ============================================================

        [Required]
        [Display(Name = "Package Number")]
        public string PackageNumber { get; set; }


        // ============================================================
        // QR CODE
        // ============================================================

        [Required]
        [Display(Name = "QR Code Value")]
        public string QrCodeValue { get; set; }


        // ============================================================
        // PACKAGE STATUS
        // ============================================================

        public bool IsReceived { get; set; }

        public DateTime? ReceivedDate { get; set; }


        // ============================================================
        // CREATED DATE
        // ============================================================

        public DateTime CreatedDate { get; set; }


        public PackageOrder()
        {
            CreatedDate = DateTime.Now;
            IsReceived = false;
        }
    }
}
