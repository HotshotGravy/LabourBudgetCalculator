using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LabourBudgetCalculator
{
    [Serializable]
    public class CommissioningProject
    {
        public string ProjectID { get; set; }
        public string ProjectName { get; set; }
        public decimal InitialEstimate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ProjectNumber { get; set; }
        public string ClientName { get; set; }
        public string SiteLocation { get; set; }
        public DateTime ProjectDate { get; set; }

        // Additional properties needed for CommissioningResultsWindow
        public decimal PlannedTotal { get; set; }
        public decimal CurrentTotal { get; set; }
        public decimal ForecastTotal { get; set; }
        public bool IsDirty { get; set; } = true;

        public List<CommissioningResource> Resources { get; set; }

        public CommissioningProject()
        {
            Resources = new List<CommissioningResource>();
        }

        public void CalculateTotals()
        {
            decimal plannedTotal = 0;
            decimal currentTotal = 0;
            decimal forecastTotal = 0;

            foreach (var resource in Resources)
            {
                decimal resourcePlanned = 0;
                decimal resourceCurrent = 0;
                decimal resourceForecast = 0;

                foreach (var entry in resource.DailyData)
                {
                    var dayData = entry.Value;

                    // Calculate labor costs
                    decimal plannedLabor =
                        (dayData.PlannedRegularLabourHours * resource.RegularLabourRate) +
                        (dayData.PlannedOvertimeLabourHours * resource.OvertimeLabourRate) +
                        (dayData.PlannedPremiumLabourHours * resource.PremiumLabourRate);

                    decimal actualLabor =
                        (dayData.ActualRegularLabourHours * resource.RegularLabourRate) +
                        (dayData.ActualOvertimeLabourHours * resource.OvertimeLabourRate) +
                        (dayData.ActualPremiumLabourHours * resource.PremiumLabourRate);

                    // Calculate travel costs
                    decimal plannedTravel =
                        (dayData.PlannedRegularTravelHours * resource.RegularTravelRate) +
                        (dayData.PlannedOvertimeTravelHours * resource.OvertimeTravelRate) +
                        (dayData.PlannedPremiumTravelHours * resource.PremiumTravelRate);

                    decimal actualTravel =
                        (dayData.ActualRegularTravelHours * resource.RegularTravelRate) +
                        (dayData.ActualOvertimeTravelHours * resource.OvertimeTravelRate) +
                        (dayData.ActualPremiumTravelHours * resource.PremiumTravelRate);

                    // Calculate expense costs
                    decimal plannedExpenses =
                        dayData.PlannedMileageCost +
                        dayData.PlannedPerDiemCost +
                        dayData.PlannedFlightCost +
                        dayData.PlannedRentalCarCost +
                        dayData.PlannedHotelCost;

                    decimal actualExpenses =
                        dayData.ActualMileageCost +
                        dayData.ActualPerDiemCost +
                        dayData.ActualFlightCost +
                        dayData.ActualRentalCarCost +
                        dayData.ActualHotelCost;

                    // Add to resource totals
                    resourcePlanned += plannedLabor + plannedTravel + plannedExpenses;
                    resourceCurrent += actualLabor + actualTravel + actualExpenses;

                    // For forecast, use actual if available, otherwise use planned
                    decimal forecastLabor = (actualLabor > 0) ? actualLabor : plannedLabor;
                    decimal forecastTravel = (actualTravel > 0) ? actualTravel : plannedTravel;
                    decimal forecastExpenses = (actualExpenses > 0) ? actualExpenses : plannedExpenses;

                    resourceForecast += forecastLabor + forecastTravel + forecastExpenses;
                }

                // Add to project totals
                plannedTotal += resourcePlanned;
                currentTotal += resourceCurrent;
                forecastTotal += resourceForecast;
            }

            // Update project properties
            PlannedTotal = plannedTotal;
            CurrentTotal = currentTotal;
            ForecastTotal = forecastTotal;

            // Reset dirty flag
            IsDirty = false;
        }
    }
}