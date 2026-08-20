using SwiftCare.Models;
using System.ComponentModel.DataAnnotations;

namespace SwiftCare.ViewModels
{
    public class RequestViewModel
    {
        public string? ProfilePicture { get; set; }

        [Required(ErrorMessage = "Resident ID is required.")]
        public string RequestedForId { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public Room? RoomNo { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Reason for transport is required.")]
        public string RequestReason { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime RequestedForDate { get; set; }

        [DataType(DataType.Time)]
        public DateTime RequestedForTime { get; set; }

        [Required(ErrorMessage = "Pickup address is required.")]
        public string PickUpAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination address is required.")]
        public string DestinationAddress { get; set; } = string.Empty;

        public double PickUpLatitude { get; set; }

        public double PickUpLongitude { get; set; }

        public double DestinationLatitude { get; set; }

        public double DestinationLongitude { get; set; }
    }
}