// HourCategorizerUtil.cs
// Purpose: Provides utility methods for categorizing hours.
// This can be placed in your Helpers folder or a new Utilities folder.

using System;
using LabourBudgetCalculator.Models; // Assuming DailyHourBreakdown is in Models

namespace LabourBudgetCalculator.Helpers // Or a more general namespace like LabourBudgetCalculator.Utils
{
    public static class HourCategorizerUtil
    {
        public static DailyHourBreakdown CategorizeDailyHours(
            decimal totalLabourHours,
            decimal totalTravelHours,
            DayOfWeek dayOfWeek,
            bool isEmergency,
            bool isHoldoverDay)
        {
            var breakdown = new DailyHourBreakdown();

            if (totalLabourHours < 0) totalLabourHours = 0;
            if (totalTravelHours < 0) totalTravelHours = 0;

            if (isEmergency)
            {
                breakdown.PremiumLabourHours = totalLabourHours;
                breakdown.PremiumTravelHours = totalTravelHours;
            }
            else if (isHoldoverDay)
            {
                // Holdover day logic: All hours are typically regular.
                // Assuming input totalLabourHours for holdover (e.g., 8) are all regular.
                breakdown.RegularLabourHours = totalLabourHours;
                breakdown.RegularTravelHours = totalTravelHours; // And any travel is also regular
            }
            else
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        breakdown.PremiumLabourHours = totalLabourHours;
                        breakdown.PremiumTravelHours = totalTravelHours;
                        break;

                    case DayOfWeek.Saturday:
                        breakdown.OvertimeLabourHours = totalLabourHours;
                        breakdown.OvertimeTravelHours = totalTravelHours;
                        break;

                    // Weekdays: Monday to Friday
                    default:
                        decimal combinedHours = totalLabourHours + totalTravelHours;
                        decimal regularHoursLimit = 8.0m;

                        if (combinedHours <= regularHoursLimit)
                        {
                            breakdown.RegularLabourHours = totalLabourHours;
                            breakdown.RegularTravelHours = totalTravelHours;
                        }
                        else // More than 8 combined hours
                        {
                            // Prioritize labour for regular hours
                            breakdown.RegularLabourHours = Math.Min(totalLabourHours, regularHoursLimit);
                            decimal remainingRegularAllowance = regularHoursLimit - breakdown.RegularLabourHours;
                            breakdown.RegularTravelHours = Math.Min(totalTravelHours, remainingRegularAllowance);

                            // Remaining hours are overtime
                            breakdown.OvertimeLabourHours = totalLabourHours - breakdown.RegularLabourHours;
                            breakdown.OvertimeTravelHours = totalTravelHours - breakdown.RegularTravelHours;
                        }
                        break;
                }
            }
            return breakdown;
        }
    }
}