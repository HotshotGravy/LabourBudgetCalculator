using System;

namespace LabourBudgetCalculator
{
    [Serializable]
    public class ResourceDayData
    {
        // Labour Hours
        public decimal PlannedRegularLabourHours { get; set; }
        public decimal PlannedOvertimeLabourHours { get; set; }
        public decimal PlannedPremiumLabourHours { get; set; }
        public decimal ActualRegularLabourHours { get; set; }
        public decimal ActualOvertimeLabourHours { get; set; }
        public decimal ActualPremiumLabourHours { get; set; }

        // Travel Hours
        public decimal PlannedRegularTravelHours { get; set; }
        public decimal PlannedOvertimeTravelHours { get; set; }
        public decimal PlannedPremiumTravelHours { get; set; }
        public decimal ActualRegularTravelHours { get; set; }
        public decimal ActualOvertimeTravelHours { get; set; }
        public decimal ActualPremiumTravelHours { get; set; }

        // Expenses
        public decimal PlannedMileageCost { get; set; }
        public decimal PlannedPerDiemCost { get; set; }
        public decimal PlannedFlightCost { get; set; }
        public decimal PlannedRentalCarCost { get; set; }
        public decimal PlannedHotelCost { get; set; }
        public decimal ActualMileageCost { get; set; }
        public decimal ActualPerDiemCost { get; set; }
        public decimal ActualFlightCost { get; set; }
        public decimal ActualRentalCarCost { get; set; }
        public decimal ActualHotelCost { get; set; }
    }
}