using System;
using System.Collections.Generic;

namespace TimeExpenseCalculator.Models
{
    [Serializable]
    public class RateSheet
    {
        public string Name { get; set; }
        public decimal RegularLabourRate { get; set; }
        public decimal OvertimeLabourRate { get; set; }
        public decimal PremiumLabourRate { get; set; }
        public decimal RegularTravelRate { get; set; }
        public decimal OvertimeTravelRate { get; set; }
        public decimal PremiumTravelRate { get; set; }
        public decimal HotelCost { get; set; }
        public decimal PerDiemRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal RentalCarRate { get; set; }

        public decimal FlightCost { get; set; }

        public RateSheet()
        {
            // Default constructor
        }

        public RateSheet(string name)
        {
            Name = name;
        }
    }
}