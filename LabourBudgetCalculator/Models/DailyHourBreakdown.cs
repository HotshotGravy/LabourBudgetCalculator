// DailyHourBreakdown.cs
// Purpose: Data structure to hold the categorized daily hours.
// This can be placed in your Models folder or a new Utilities folder.

namespace LabourBudgetCalculator.Models // Or your appropriate namespace
{
    public class DailyHourBreakdown
    {
        public decimal RegularLabourHours { get; set; }
        public decimal OvertimeLabourHours { get; set; }
        public decimal PremiumLabourHours { get; set; }
        public decimal RegularTravelHours { get; set; }
        public decimal OvertimeTravelHours { get; set; }
        public decimal PremiumTravelHours { get; set; }

        public DailyHourBreakdown()
        {
            RegularLabourHours = 0m;
            OvertimeLabourHours = 0m;
            PremiumLabourHours = 0m;
            RegularTravelHours = 0m;
            OvertimeTravelHours = 0m;
            PremiumTravelHours = 0m;
        }

        // New method to get total planned hours
        public decimal GetTotalPlannedHours()
        {
            return RegularLabourHours + OvertimeLabourHours + PremiumLabourHours +
                   RegularTravelHours + OvertimeTravelHours + PremiumTravelHours;
        }
    }
}