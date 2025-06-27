using System;
using LabourBudgetCalculator.Models;

namespace LabourBudgetCalculator.Helpers
{
    internal static class ResultsGridCalculationHelper
    {
        internal static decimal GetPlannedValue(ResourceDayData dD, string dT)
        {
            if (dD == null) return 0;
            return dT switch
            {
                "ServiceReg" => dD.PlannedRegularLabourHours,
                "ServiceOT" => dD.PlannedOvertimeLabourHours,
                "ServicePrem" => dD.PlannedPremiumLabourHours,
                "TravelReg" => dD.PlannedRegularTravelHours,
                "TravelOT" => dD.PlannedOvertimeTravelHours,
                "TravelPrem" => dD.PlannedPremiumTravelHours,
                "Mileage" => dD.PlannedMileageCost,
                "PerDiem" => dD.PlannedPerDiemCost,
                "Flight" => dD.PlannedFlightCost,
                "CarRental" => dD.PlannedRentalCarCost,
                "Hotel" => dD.PlannedHotelCost,
                _ => 0
            };
        }

        internal static decimal GetActualValue(ResourceDayData dD, string dT)
        {
            if (dD == null) return 0;
            return dT switch
            {
                "ServiceReg" => dD.ActualRegularLabourHours,
                "ServiceOT" => dD.ActualOvertimeLabourHours,
                "ServicePrem" => dD.ActualPremiumLabourHours,
                "TravelReg" => dD.ActualRegularTravelHours,
                "TravelOT" => dD.ActualOvertimeTravelHours,
                "TravelPrem" => dD.ActualPremiumTravelHours,
                "Mileage" => dD.ActualMileageCost,
                "PerDiem" => dD.ActualPerDiemCost,
                "Flight" => dD.ActualFlightCost,
                "CarRental" => dD.ActualRentalCarCost,
                "Hotel" => dD.ActualHotelCost,
                _ => 0
            };
        }

        internal static decimal CalculateSubtotal(ResourceDayData dD, CommissioningResource resource, bool isCharges, bool isPlanned)
        {
            if (dD == null || resource == null) return 0;
            decimal total = 0;
            if (isCharges)
            {
                // Labour + Travel charges
                total += (isPlanned ? dD.PlannedRegularLabourHours : dD.ActualRegularLabourHours) * GetRegularLabourRate(resource);
                total += (isPlanned ? dD.PlannedOvertimeLabourHours : dD.ActualOvertimeLabourHours) * GetOvertimeLabourRate(resource);
                total += (isPlanned ? dD.PlannedPremiumLabourHours : dD.ActualPremiumLabourHours) * GetPremiumLabourRate(resource);
                total += (isPlanned ? dD.PlannedRegularTravelHours : dD.ActualRegularTravelHours) * GetRegularTravelRate(resource);
                total += (isPlanned ? dD.PlannedOvertimeTravelHours : dD.ActualOvertimeTravelHours) * GetOvertimeTravelRate(resource);
                total += (isPlanned ? dD.PlannedPremiumTravelHours : dD.ActualPremiumTravelHours) * GetPremiumTravelRate(resource);
            }
            else
            {
                // Expenses
                total += isPlanned ? dD.PlannedMileageCost : dD.ActualMileageCost;
                total += isPlanned ? dD.PlannedPerDiemCost : dD.ActualPerDiemCost;
                total += isPlanned ? dD.PlannedFlightCost : dD.ActualFlightCost;
                total += isPlanned ? dD.PlannedRentalCarCost : dD.ActualRentalCarCost;
                total += isPlanned ? dD.PlannedHotelCost : dD.ActualHotelCost;
            }
            return total;
        }

        internal static decimal CalculateDayTotal(ResourceDayData dD, CommissioningResource resource, bool isPlanned)
        {
            if (dD == null || resource == null) return 0;
            decimal total = 0;
            // All charges
            total += (isPlanned ? dD.PlannedRegularLabourHours : dD.ActualRegularLabourHours) * GetRegularLabourRate(resource);
            total += (isPlanned ? dD.PlannedOvertimeLabourHours : dD.ActualOvertimeLabourHours) * GetOvertimeLabourRate(resource);
            total += (isPlanned ? dD.PlannedPremiumLabourHours : dD.ActualPremiumLabourHours) * GetPremiumLabourRate(resource);
            total += (isPlanned ? dD.PlannedRegularTravelHours : dD.ActualRegularTravelHours) * GetRegularTravelRate(resource);
            total += (isPlanned ? dD.PlannedOvertimeTravelHours : dD.ActualOvertimeTravelHours) * GetOvertimeTravelRate(resource);
            total += (isPlanned ? dD.PlannedPremiumTravelHours : dD.ActualPremiumTravelHours) * GetPremiumTravelRate(resource);
            // All expenses
            total += isPlanned ? dD.PlannedMileageCost : dD.ActualMileageCost;
            total += isPlanned ? dD.PlannedPerDiemCost : dD.ActualPerDiemCost;
            total += isPlanned ? dD.PlannedFlightCost : dD.ActualFlightCost;
            total += isPlanned ? dD.PlannedRentalCarCost : dD.ActualRentalCarCost;
            total += isPlanned ? dD.PlannedHotelCost : dD.ActualHotelCost;
            return total;
        }

        internal static bool IsHoursType(string dataType)
        {
            return dataType == "ServiceReg" || dataType == "ServiceOT" || dataType == "ServicePrem" ||
                   dataType == "TravelReg" || dataType == "TravelOT" || dataType == "TravelPrem";
        }

        // Rate helpers
        internal static decimal GetRegularLabourRate(CommissioningResource resource)
        {
            decimal r = resource.RegularLabourRate, d = 1 - (resource.DiscountPercent / 100m);
            return resource.IsEmergency ? resource.PremiumLabourRate * d : r * d;
        }
        internal static decimal GetOvertimeLabourRate(CommissioningResource resource)
        {
            decimal r = resource.OvertimeLabourRate, d = 1 - (resource.DiscountPercent / 100m);
            return resource.IsEmergency ? resource.PremiumLabourRate * d : r * d;
        }
        internal static decimal GetPremiumLabourRate(CommissioningResource resource)
        {
            return resource.PremiumLabourRate * (1 - (resource.DiscountPercent / 100m));
        }
        internal static decimal GetRegularTravelRate(CommissioningResource resource)
        {
            decimal r = resource.RegularTravelRate, d = 1 - (resource.DiscountPercent / 100m);
            return resource.IsEmergency ? resource.PremiumTravelRate * d : r * d;
        }
        internal static decimal GetOvertimeTravelRate(CommissioningResource resource)
        {
            decimal r = resource.OvertimeTravelRate, d = 1 - (resource.DiscountPercent / 100m);
            return resource.IsEmergency ? resource.PremiumTravelRate * d : r * d;
        }
        internal static decimal GetPremiumTravelRate(CommissioningResource resource)
        {
            return resource.PremiumTravelRate * (1 - (resource.DiscountPercent / 100m));
        }

        internal static string FormatValue(decimal value, string dataType)
        {
            return IsHoursType(dataType) ? value.ToString("F1") : value.ToString("C2");
        }
    }
} 