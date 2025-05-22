using System;
using System.Collections.Generic; // Not strictly needed for this class definition but often included
using System.Xml.Serialization; // Required for [Serializable]

namespace TimeExpenseCalculator.Models // As per your provided DataManager.cs
{
    [Serializable] // For XML serialization
    public class RateSheet
    {
        public string Name { get; set; }
        public decimal RegularLabourRate { get; set; }
        public decimal OvertimeLabourRate { get; set; }
        public decimal PremiumLabourRate { get; set; }
        public decimal RegularTravelRate { get; set; }
        public decimal OvertimeTravelRate { get; set; }
        public decimal PremiumTravelRate { get; set; }

        // Expense rates associated with this sheet
        public decimal HotelCost { get; set; }
        public decimal PerDiemRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal RentalCarRate { get; set; }
        public decimal FlightCost { get; set; } // Typical flight cost associated with this rate profile

        public RateSheet()
        {
            // Parameterless constructor required for XML deserialization
            Name = "Unnamed Rate Sheet"; // Default name
        }

        public RateSheet(string name)
        {
            Name = name;
            // Initialize with default values or leave as 0 to be set explicitly
            RegularLabourRate = 0;
            OvertimeLabourRate = 0;
            PremiumLabourRate = 0;
            RegularTravelRate = 0;
            OvertimeTravelRate = 0;
            PremiumTravelRate = 0;
            HotelCost = 0;
            PerDiemRate = 0;
            MileageRate = 0;
            RentalCarRate = 0;
            FlightCost = 0;
        }
    }
}
