using System;
using System.Collections.Generic;
using System.Linq;
using LabourBudgetCalculator.Models;

namespace LabourBudgetCalculator.Helpers
{
    /// <summary>
    /// Utility class for calculating expenses consistently across the application
    /// </summary>
    public static class ExpenseCalculator
    {
        /// <summary>
        /// Calculates the mileage cost based on travel configuration
        /// </summary>
        public static decimal CalculateMileageCost(
            decimal mileageRate,
            decimal travelDistance,
            decimal dailyTravelDistance,
            bool isFirstOrLastDay,
            bool isRentalCarUsed,
            string travelMethod)
        {
            decimal mileageCost = 0;

            if (travelMethod == "Driving")
            {
                if (isFirstOrLastDay && travelDistance > 0)
                {
                    mileageCost = mileageRate * travelDistance;
                }
                else if (!isRentalCarUsed && dailyTravelDistance > 0)
                {
                    mileageCost = mileageRate * (dailyTravelDistance * 2);
                }
            }
            return mileageCost;
        }

        /// <summary>
        /// Calculates the hotel cost based on travel configuration
        /// </summary>
        public static decimal CalculateHotelCost(
            decimal hotelRate,
            bool hotelRequired,
            bool isLastDay)
        {
            if (!hotelRequired || isLastDay)
                return 0;
            return hotelRate;
        }

        /// <summary>
        /// Calculates the rental car cost based on travel configuration
        /// </summary>
        public static decimal CalculateRentalCarCost(
           decimal rentalCarRate,
           bool rentalCarRequired)
        {
            if (!rentalCarRequired)
                return 0;
            return rentalCarRate;
        }

        /// <summary>
        /// Calculates the flight cost based on travel configuration
        /// </summary>
        public static decimal CalculateFlightCost(
           decimal flightCost,
           string travelMethod,
           bool isTravelDay,
           bool isFirstDay,
           bool isLastDay,
           bool separateTravelTo,
           bool separateTravelFrom)
        {
            if (travelMethod != "Flight")
                return 0;

            if (isTravelDay ||
                (isFirstDay && !separateTravelTo) ||
                (isLastDay && !separateTravelFrom))
            {
                return flightCost;
            }
            return 0;
        }

        /// <summary>
        /// Calculates the per diem cost based on work configuration
        /// </summary>
        public static decimal CalculatePerDiemCost(
            decimal perDiemRate,
            decimal laborHours,
            decimal travelHours)
        {
            if (laborHours > 0 || travelHours > 0)
                return perDiemRate;
            return 0;
        }

        /// <summary>
        /// Calculates the total expenses with appropriate markup
        /// </summary>
        public static decimal CalculateTotalExpensesWithMarkup(
            decimal hotelCost,
            decimal rentalCarCost,
            decimal flightCost,
            decimal mileageCost,
            decimal perDiemCost,
            decimal otherExpenses,
            decimal markupPercentage = 10)
        {
            decimal expensesWithMarkup = (hotelCost + rentalCarCost + flightCost) *
                                         (1 + (markupPercentage / 100m));
            decimal totalExpenses = expensesWithMarkup + mileageCost + perDiemCost + otherExpenses;
            return totalExpenses;
        }

        /// <summary>
        /// Calculates expenses for a day based on resource configuration
        /// </summary>
        public static void CalculateDayExpenses(
           ResourceDayData dayData,
           CommissioningResource resource,
           int dayIndex,
           int totalDays,
           bool separateTravelTo,
           bool separateTravelFrom)
        {
            bool isFirstDay = dayIndex == 0;
            bool isLastDay = dayIndex == (totalDays - 1);
            bool isTravelToDay = separateTravelTo && isFirstDay;
            bool isTravelFromDay = separateTravelFrom && isLastDay;
            bool isTravelDay = isTravelToDay || isTravelFromDay;

            System.Diagnostics.Debug.WriteLine($"Day {dayIndex}: First={isFirstDay}, Last={isLastDay}, TravelTo={isTravelToDay}, TravelFrom={isTravelFromDay}");
            System.Diagnostics.Debug.WriteLine($"Travel Method: {resource.TravelMethod}, Hotel: {resource.HotelRequired}, Rental: {resource.RentalCarRequired}");

            dayData.PlannedMileageCost = CalculateMileageCost(
                resource.MileageRate, resource.TravelDistance, resource.DailyTravelDistance,
                isFirstDay || isLastDay, resource.RentalCarRequired, resource.TravelMethod);

            dayData.PlannedHotelCost = CalculateHotelCost(
                resource.HotelRate, resource.HotelRequired, isLastDay);

            dayData.PlannedRentalCarCost = CalculateRentalCarCost(
                resource.RentalCarRate, resource.RentalCarRequired);

            dayData.PlannedFlightCost = CalculateFlightCost(
                resource.FlightCost, resource.TravelMethod, isTravelDay,
                isFirstDay, isLastDay, separateTravelTo, separateTravelFrom);

            decimal laborHours = dayData.GetPlannedLabourHoursTotal();
            decimal travelHours = dayData.GetPlannedTravelHoursTotal();
            dayData.PlannedPerDiemCost = CalculatePerDiemCost(
                resource.PerDiemRate, laborHours, travelHours);

            System.Diagnostics.Debug.WriteLine($"Calculated expenses: Mileage={dayData.PlannedMileageCost}, Hotel={dayData.PlannedHotelCost}, " +
                $"Rental={dayData.PlannedRentalCarCost}, Flight={dayData.PlannedFlightCost}, PerDiem={dayData.PlannedPerDiemCost}");
        }

        // This method is no longer needed if InitializeFromSchedule in CommissioningResource
        // is the sole authority for setting initial Actuals from Planned.
        // You can choose to delete it or keep it if it's used elsewhere,
        // but it should not be called from CalculateResourceExpenses.
        /*
        public static void InitializeActualsFromPlanned(ResourceDayData dayData)
        {
            if (dayData.ActualMileageCost == 0)
                dayData.ActualMileageCost = dayData.PlannedMileageCost;
            if (dayData.ActualHotelCost == 0)
                dayData.ActualHotelCost = dayData.PlannedHotelCost;
            if (dayData.ActualRentalCarCost == 0)
                dayData.ActualRentalCarCost = dayData.PlannedRentalCarCost;
            if (dayData.ActualFlightCost == 0)
                dayData.ActualFlightCost = dayData.PlannedFlightCost;
            if (dayData.ActualPerDiemCost == 0)
                dayData.ActualPerDiemCost = dayData.PlannedPerDiemCost;
        }
        */

        /// <summary>
        /// Calculates all expenses for a resource
        /// </summary>
        public static void CalculateResourceExpenses(CommissioningResource resource)
        {
            if (resource.DailyData == null || !resource.DailyData.Any())
                return;

            int totalEngagementDays = 0; // Recalculate based on actual keys present to be more robust
            if (resource.DailyData.ContainsKey(-1)) totalEngagementDays++; // Travel To
            totalEngagementDays += resource.DaysOnSite;
            if (resource.DailyData.ContainsKey(resource.DaysOnSite)) totalEngagementDays++; // Travel From

            // Create a sorted list of day entries to process them chronologically for dayIndex
            var orderedDays = resource.DailyData.OrderBy(kvp => kvp.Value.Date).ToList();
            if (!orderedDays.Any()) return;

            // Determine the actual number of distinct days represented in the data for day indexing
            // This count might differ from 'totalEngagementDays' if data is sparse or keys are not contiguous.
            // For robust day indexing, we'll use the position in the ordered list.
            int actualDayCountInSchedule = orderedDays.Count;


            for (int i = 0; i < actualDayCountInSchedule; i++)
            {
                var kvp = orderedDays[i];
                // int dayKey = kvp.Key; // dayKey might not be contiguous or match 'i' perfectly
                ResourceDayData dayData = kvp.Value;

                // Use 'i' as the chronological dayIndex for calculations within CalculateDayExpenses
                // This 'i' will be 0 for the earliest day, 1 for the next, etc.
                // 'actualDayCountInSchedule' is the total number of days being processed.
                CalculateDayExpenses(
                    dayData,
                    resource,
                    i, // Use chronological index 'i'
                    actualDayCountInSchedule, // Use the count of days we are actually iterating over
                    resource.SeparateTravelTo,
                    resource.SeparateTravelFrom);

                // DO NOT CALL InitializeActualsFromPlanned(dayData) HERE ANYMORE.
                // CommissioningResource.InitializeFromSchedule() is now responsible
                // for unconditionally setting Actuals from its calculated Planned values.
            }
        }
    }
}