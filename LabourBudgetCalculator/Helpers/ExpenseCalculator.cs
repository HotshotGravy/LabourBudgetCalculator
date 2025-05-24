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
                    // First or last day gets the full travel distance
                    mileageCost = mileageRate * travelDistance;
                }
                else if (!isRentalCarUsed && dailyTravelDistance > 0)
                {
                    // For middle days, only apply mileage if driving and no rental car
                    // Daily travel is round trip (multiply by 2)
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
            // No hotel on the last day of travel or if hotel not required
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
            // Check if rental car is required
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
            // If not flying, no flight cost
            if (travelMethod != "Flight")
                return 0;

            // Flight costs only apply on travel days or first/last days (if no separate travel)
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
            // Apply per diem if there's any work or travel on the day
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
            // Apply markup to expenses except mileage and per diem
            decimal expensesWithMarkup = (hotelCost + rentalCarCost + flightCost) *
                                        (1 + (markupPercentage / 100m));

            // Add mileage and per diem without markup
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

            // Debug output
            System.Diagnostics.Debug.WriteLine($"Day {dayIndex}: First={isFirstDay}, Last={isLastDay}, TravelTo={isTravelToDay}, TravelFrom={isTravelFromDay}");
            System.Diagnostics.Debug.WriteLine($"Travel Method: {resource.TravelMethod}, Hotel: {resource.HotelRequired}, Rental: {resource.RentalCarRequired}");

            // Calculate each expense type
            dayData.PlannedMileageCost = CalculateMileageCost(
                resource.MileageRate,
                resource.TravelDistance,
                resource.DailyTravelDistance,
                isFirstDay || isLastDay,
                resource.RentalCarRequired,
                resource.TravelMethod);

            dayData.PlannedHotelCost = CalculateHotelCost(
                resource.HotelRate,
                resource.HotelRequired,
                isLastDay);

            dayData.PlannedRentalCarCost = CalculateRentalCarCost(
                resource.RentalCarRate,
                resource.RentalCarRequired);

            dayData.PlannedFlightCost = CalculateFlightCost(
                resource.FlightCost,
                resource.TravelMethod,
                isTravelDay,
                isFirstDay,
                isLastDay,
                separateTravelTo,
                separateTravelFrom);

            decimal laborHours = dayData.GetPlannedLabourHoursTotal();
            decimal travelHours = dayData.GetPlannedTravelHoursTotal();
            dayData.PlannedPerDiemCost = CalculatePerDiemCost(
                resource.PerDiemRate,
                laborHours,
                travelHours);

            // Debug output of calculated expenses
            System.Diagnostics.Debug.WriteLine($"Calculated expenses: Mileage={dayData.PlannedMileageCost}, Hotel={dayData.PlannedHotelCost}, " +
                $"Rental={dayData.PlannedRentalCarCost}, Flight={dayData.PlannedFlightCost}, PerDiem={dayData.PlannedPerDiemCost}");
        }

        public static void InitializeActualsFromPlanned(ResourceDayData dayData)
        {
            // Only initialize if actuals haven't been set yet
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

        /// <summary>
        /// Calculates all expenses for a resource
        /// </summary>
        public static void CalculateResourceExpenses(CommissioningResource resource)
        {
            if (resource.DailyData == null || !resource.DailyData.Any())
                return;

            int totalDays = resource.DailyData.Count;
            bool separateTravelTo = resource.SeparateTravelTo;
            bool separateTravelFrom = resource.SeparateTravelFrom;

            foreach (var kvp in resource.DailyData)
            {
                int dayKey = kvp.Key;
                ResourceDayData dayData = kvp.Value;

                // Special handling for travel days
                int dayIndex = 0;
                if (dayKey == -1) // Travel TO day
                {
                    dayIndex = 0;
                }
                else if (dayKey == resource.DaysOnSite) // Travel FROM day
                {
                    dayIndex = totalDays - 1;
                }
                else
                {
                    dayIndex = dayKey;
                }

                CalculateDayExpenses(
                    dayData,
                    resource,
                    dayIndex,
                    totalDays,
                    separateTravelTo,
                    separateTravelFrom);
                InitializeActualsFromPlanned(dayData);
            }
        }
    }
}