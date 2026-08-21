using SwiftCare.Models;

namespace SwiftCare.ViewModels
{
    public class ResidentDetailsViewModel
    {
        // ============================================================
        // RESIDENT
        // ============================================================

        public Resident Resident { get; set; } = null!;


        // ============================================================
        // EQUIPMENT ASSIGNED TO THIS RESIDENT
        // ============================================================

        public List<OurEquipment> AssignedEquipment { get; set; }
            = new List<OurEquipment>();
    }
}