using System;

namespace LabourBudgetCalculator.Models
{
    [Serializable]
    public class ResourceDayData
    {
        public DateTime Date { get; set; }

        // Planned values
        public string PlannedStartTime { get; set; }
        public string PlannedEndTime { get; set; }
        public decimal PlannedRegularLabourHours { get; set; }
        public decimal PlannedOvertimeLabourHours { get; set; }
        public decimal PlannedPremiumLabourHours { get; set; }
        public decimal PlannedRegularTravelHours { get; set; }
        public decimal PlannedOvertimeTravelHours { get; set; }
        public decimal PlannedPremiumTravelHours { get; set; }
        public decimal PlannedHotelCost { get; set; }
        public decimal PlannedPerDiemCost { get; set; }
        public decimal PlannedMileageCost { get; set; }
        public decimal PlannedFlightCost { get; set; }
        public decimal PlannedRentalCarCost { get; set; }

        // Actual values
        public string ActualStartTime { get; set; }
        public string ActualEndTime { get; set; }
        public decimal ActualRegularLabourHours { get; set; }
        public decimal ActualOvertimeLabourHours { get; set; }
        public decimal ActualPremiumLabourHours { get; set; }
        public decimal ActualRegularTravelHours { get; set; }
        public decimal ActualOvertimeTravelHours { get; set; }
        public decimal ActualPremiumTravelHours { get; set; }
        public decimal ActualHotelCost { get; set; }
        public decimal ActualPerDiemCost { get; set; }
        public decimal ActualMileageCost { get; set; }
        public decimal ActualFlightCost { get; set; }
        public decimal ActualRentalCarCost { get; set; }

        // Constructor with default values
        public ResourceDayData()
        {
            // Initialize with safe default values
            Date = DateTime.Today;
            PlannedStartTime = "07:00";
            ActualStartTime = "07:00";
            PlannedEndTime = "15:30";
            ActualEndTime = "15:30";
        }

        // Helper methods to calculate totals
        public decimal GetPlannedLabourHoursTotal()
        {
            return PlannedRegularLabourHours + PlannedOvertimeLabourHours + PlannedPremiumLabourHours;
        }

        public decimal GetActualLabourHoursTotal()
        {
            return ActualRegularLabourHours + ActualOvertimeLabourHours + ActualPremiumLabourHours;
        }

        public decimal GetPlannedTravelHoursTotal()
        {
            return PlannedRegularTravelHours + PlannedOvertimeTravelHours + PlannedPremiumTravelHours;
        }

        public decimal GetActualTravelHoursTotal()
        {
            return ActualRegularTravelHours + ActualOvertimeTravelHours + ActualPremiumTravelHours;
        }

        public decimal GetPlannedExpensesTotal()
        {
            return PlannedHotelCost + PlannedPerDiemCost + PlannedMileageCost +
                   PlannedFlightCost + PlannedRentalCarCost;
        }

        public decimal GetActualExpensesTotal()
        {
            return ActualHotelCost + ActualPerDiemCost + ActualMileageCost +
                   ActualFlightCost + ActualRentalCarCost;
        }
    }
}