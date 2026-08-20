using System.ComponentModel.DataAnnotations;

namespace SwiftCare.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Packaged")]
        Packaged,

        [Display(Name = "On the way")]
        OnTheWay,

        [Display(Name = "Delivered")]
        Collected,

        [Display(Name = "Cancelled")]
        Cancelled
    }
}
