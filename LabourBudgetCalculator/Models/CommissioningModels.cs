using System;
using System.Collections.Generic;
using System.Linq;

namespace LabourBudgetCalculator.Models
{
    /// <summary>
    /// Represents a single day's data for a specific resource
    /// </summary>
    public class ResourceDayData
    {
        // Service hours by rate type
        public decimal PlannedRegularLabourHours { get; set; } = 0;
        public decimal PlannedOvertimeLabourHours { get; set; } = 0;
        public decimal PlannedPremiumLabourHours { get; set; } = 0;

        public decimal ActualRegularLabourHours { get; set; } = 0;
        public decimal ActualOvertimeLabourHours { get; set; } = 0;
        public decimal ActualPremiumLabourHours { get; set; } = 0;

        // Travel hours by rate type
        public decimal PlannedRegularTravelHours { get; set; } = 0;
        public decimal PlannedOvertimeTravelHours { get; set; } = 0;
        public decimal PlannedPremiumTravelHours { get; set; } = 0;

        public decimal ActualRegularTravelHours { get; set; } = 0;
        public decimal ActualOvertimeTravelHours { get; set; } = 0;
        public decimal ActualPremiumTravelHours { get; set; } = 0;

        // Expense items
        public decimal PlannedMileageCost { get; set; } = 0;
        public decimal PlannedPerDiemCost { get; set; } = 0;
        public decimal PlannedFlightCost { get; set; } = 0;
        public decimal PlannedRentalCarCost { get; set; } = 0;
        public decimal PlannedHotelCost { get; set; } = 0;

        public decimal ActualMileageCost { get; set; } = 0;
        public decimal ActualPerDiemCost { get; set; } = 0;
        public decimal ActualFlightCost { get; set; } = 0;
        public decimal ActualRentalCarCost { get; set; } = 0;
        public decimal ActualHotelCost { get; set; } = 0;

        // Time tracking
        public string PlannedStartTime { get; set; } = "07:00";
        public string PlannedEndTime { get; set; } = "17:30";

        public string ActualStartTime { get; set; } = "07:00";
        public string ActualEndTime { get; set; } = "17:30";

        // Calculated properties
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
            return PlannedMileageCost + PlannedPerDiemCost + PlannedFlightCost + PlannedRentalCarCost + PlannedHotelCost;
        }

        public decimal GetActualExpensesTotal()
        {
            return ActualMileageCost + ActualPerDiemCost + ActualFlightCost + ActualRentalCarCost + ActualHotelCost;
        }
    }

    /// <summary>
    /// Represents a resource (technician) assigned to a commissioning project
    /// </summary>
    public class CommissioningResource
    {
        // Resource identification
        public string ResourceID { get; set; }
        public string TechnicianName { get; set; }

        // Labor rates
        public decimal RegularLabourRate { get; set; }
        public decimal OvertimeLabourRate { get; set; }
        public decimal PremiumLabourRate { get; set; }
        public decimal RegularTravelRate { get; set; }
        public decimal OvertimeTravelRate { get; set; }
        public decimal PremiumTravelRate { get; set; }

        // Travel method and rates
        public string TravelMethod { get; set; }
        public bool SeparateTravelTo { get; set; }
        public bool SeparateTravelFrom { get; set; }
        public decimal MileageRate { get; set; }
        public decimal PerDiemRate { get; set; }

        // Schedule information
        public DateTime? StartDate { get; set; }
        public int DaysOnSite { get; set; }
        public decimal HoursPerDay { get; set; }
        public string DefaultStartTime { get; set; }
        public decimal LunchDuration { get; set; }

        // Day by day data - key is the day index, value is the data for that day
        public Dictionary<int, ResourceDayData> DailyData { get; set; } = new Dictionary<int, ResourceDayData>();

        // Cached totals for performance
        public decimal? PlannedTotal { get; set; }
        public decimal? ActualTotal { get; set; }
        public bool IsDirty { get; set; } = true;

        // Constructor
        public CommissioningResource()
        {
            ResourceID = Guid.NewGuid().ToString();
            TechnicianName = "Unspecified";
            TravelMethod = "Driving";
            DefaultStartTime = "07:00";
            LunchDuration = 0.5m;
            HoursPerDay = 10m;
        }

        // Method to initialize daily data from schedule settings
        public void InitializeFromSchedule()
        {
            // Clear existing data
            DailyData.Clear();

            // Default schedule values
            DateTime startDate = StartDate ?? DateTime.Today;
            bool isSundayStart = startDate.DayOfWeek == DayOfWeek.Sunday;

            // Create data for each day
            for (int day = 0; day < DaysOnSite; day++)
            {
                ResourceDayData dayData = new ResourceDayData();
                DateTime currentDate = startDate.AddDays(day);
                bool isSaturday = currentDate.DayOfWeek == DayOfWeek.Saturday;
                bool isSunday = currentDate.DayOfWeek == DayOfWeek.Sunday;

                // Set default planned hours based on day of week
                if (isSunday)
                {
                    // Sundays are premium rate
                    dayData.PlannedPremiumLabourHours = HoursPerDay;
                }
                else if (isSaturday)
                {
                    // Saturdays are overtime rate
                    dayData.PlannedOvertimeLabourHours = HoursPerDay;
                }
                else
                {
                    // Weekdays: first 8 hours at regular rate, anything over at overtime
                    dayData.PlannedRegularLabourHours = Math.Min(8m, HoursPerDay);
                    dayData.PlannedOvertimeLabourHours = Math.Max(0m, HoursPerDay - 8m);
                }

                // Set default planned travel hours
                if (SeparateTravelTo && day == 0)
                {
                    // First day is separate travel day
                    dayData.PlannedRegularLabourHours = 0;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;

                    // Travel based on day of week
                    if (isSunday)
                    {
                        dayData.PlannedPremiumTravelHours = 8;
                    }
                    else if (isSaturday)
                    {
                        dayData.PlannedOvertimeTravelHours = 8;
                    }
                    else
                    {
                        dayData.PlannedRegularTravelHours = 8;
                    }
                }
                else if (SeparateTravelFrom && day == DaysOnSite - 1)
                {
                    // Last day is separate travel day
                    dayData.PlannedRegularLabourHours = 0;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;

                    // Travel based on day of week
                    if (isSunday)
                    {
                        dayData.PlannedPremiumTravelHours = 8;
                    }
                    else if (isSaturday)
                    {
                        dayData.PlannedOvertimeTravelHours = 8;
                    }
                    else
                    {
                        dayData.PlannedRegularTravelHours = 8;
                    }
                }
                else
                {
                    // Regular day with daily travel
                    decimal dailyTravelHours = 1.5m; // Default value

                    // Travel based on day of week
                    if (isSunday)
                    {
                        dayData.PlannedPremiumTravelHours = dailyTravelHours;
                    }
                    else if (isSaturday)
                    {
                        dayData.PlannedOvertimeTravelHours = dailyTravelHours;
                    }
                    else
                    {
                        dayData.PlannedRegularTravelHours = dailyTravelHours;
                    }
                }

                // Set default times
                dayData.PlannedStartTime = DefaultStartTime;
                TimeSpan startTime = TimeSpan.Parse(DefaultStartTime);
                TimeSpan lunchTimeSpan = TimeSpan.FromHours((double)LunchDuration);
                TimeSpan workTimeSpan = TimeSpan.FromHours((double)HoursPerDay);
                TimeSpan endTime = startTime.Add(workTimeSpan).Add(lunchTimeSpan);
                dayData.PlannedEndTime = $"{endTime.Hours:D2}:{endTime.Minutes:D2}";

                // Default expenses
                dayData.PlannedPerDiemCost = PerDiemRate;
                dayData.PlannedMileageCost = MileageRate * 40; // Assuming 40 miles per day

                // First day special expenses
                if (day == 0 && !SeparateTravelTo)
                {
                    if (TravelMethod == "Flight")
                    {
                        dayData.PlannedFlightCost = 400; // Default flight cost
                    }
                }

                // Hotel for all days except last
                if (day < DaysOnSite - 1)
                {
                    dayData.PlannedHotelCost = 170; // Default hotel cost
                }

                // Rental car for each day
                dayData.PlannedRentalCarCost = 120; // Default rental cost

                // Initialize actuals to match planned
                dayData.ActualRegularLabourHours = dayData.PlannedRegularLabourHours;
                dayData.ActualOvertimeLabourHours = dayData.PlannedOvertimeLabourHours;
                dayData.ActualPremiumLabourHours = dayData.PlannedPremiumLabourHours;

                dayData.ActualRegularTravelHours = dayData.PlannedRegularTravelHours;
                dayData.ActualOvertimeTravelHours = dayData.PlannedOvertimeTravelHours;
                dayData.ActualPremiumTravelHours = dayData.PlannedPremiumTravelHours;

                dayData.ActualMileageCost = dayData.PlannedMileageCost;
                dayData.ActualPerDiemCost = dayData.PlannedPerDiemCost;
                dayData.ActualFlightCost = dayData.PlannedFlightCost;
                dayData.ActualRentalCarCost = dayData.PlannedRentalCarCost;
                dayData.ActualHotelCost = dayData.PlannedHotelCost;

                dayData.ActualStartTime = dayData.PlannedStartTime;
                dayData.ActualEndTime = dayData.PlannedEndTime;

                // Add to daily data dictionary
                DailyData[day] = dayData;
            }

            // Mark as dirty to recalculate totals
            IsDirty = true;
        }

        // Method to calculate planned total for this resource
        public decimal CalculatePlannedTotal()
        {
            decimal total = 0;

            foreach (var entry in DailyData)
            {
                var dayData = entry.Value;

                // Labour costs
                total += (dayData.PlannedRegularLabourHours * RegularLabourRate);
                total += (dayData.PlannedOvertimeLabourHours * OvertimeLabourRate);
                total += (dayData.PlannedPremiumLabourHours * PremiumLabourRate);

                // Travel costs
                total += (dayData.PlannedRegularTravelHours * RegularTravelRate);
                total += (dayData.PlannedOvertimeTravelHours * OvertimeTravelRate);
                total += (dayData.PlannedPremiumTravelHours * PremiumTravelRate);

                // Expense costs
                total += dayData.PlannedMileageCost;
                total += dayData.PlannedPerDiemCost;
                total += dayData.PlannedFlightCost;
                total += dayData.PlannedRentalCarCost;
                total += dayData.PlannedHotelCost;
            }

            // Cache the result
            PlannedTotal = total;

            return total;
        }

        // Method to calculate actual total for this resource
        public decimal CalculateActualTotal()
        {
            decimal total = 0;

            foreach (var entry in DailyData)
            {
                var dayData = entry.Value;

                // Labour costs
                total += (dayData.ActualRegularLabourHours * RegularLabourRate);
                total += (dayData.ActualOvertimeLabourHours * OvertimeLabourRate);
                total += (dayData.ActualPremiumLabourHours * PremiumLabourRate);

                // Travel costs
                total += (dayData.ActualRegularTravelHours * RegularTravelRate);
                total += (dayData.ActualOvertimeTravelHours * OvertimeTravelRate);
                total += (dayData.ActualPremiumTravelHours * PremiumTravelRate);

                // Expense costs
                total += dayData.ActualMileageCost;
                total += dayData.ActualPerDiemCost;
                total += dayData.ActualFlightCost;
                total += dayData.ActualRentalCarCost;
                total += dayData.ActualHotelCost;
            }

            // Cache the result
            ActualTotal = total;

            return total;
        }

        // Method to update labor/travel hours from start/end times
        public void UpdateHoursFromTimes(int day, bool isActual = false)
        {
            if (!DailyData.ContainsKey(day))
                return;

            var dayData = DailyData[day];

            // Get start/end times
            string startTime = isActual ? dayData.ActualStartTime : dayData.PlannedStartTime;
            string endTime = isActual ? dayData.ActualEndTime : dayData.PlannedEndTime;

            if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
                return;

            // Parse times
            if (TimeSpan.TryParse(startTime, out TimeSpan start) &&
                TimeSpan.TryParse(endTime, out TimeSpan end))
            {
                // Handle overnight shifts
                if (end < start)
                {
                    end = end.Add(TimeSpan.FromHours(24));
                }

                // Calculate total hours minus lunch
                double totalHours = (end - start).TotalHours - (double)LunchDuration;
                if (totalHours < 0) totalHours = 0;

                decimal hours = (decimal)totalHours;

                // Get the day of week for this day
                DateTime currentDate = StartDate?.AddDays(day) ?? DateTime.Today.AddDays(day);
                bool isSaturday = currentDate.DayOfWeek == DayOfWeek.Saturday;
                bool isSunday = currentDate.DayOfWeek == DayOfWeek.Sunday;

                // Distribute hours based on day of week
                if (isActual)
                {
                    if (isSunday)
                    {
                        // Sunday - all premium
                        dayData.ActualPremiumLabourHours = hours;
                        dayData.ActualRegularLabourHours = 0;
                        dayData.ActualOvertimeLabourHours = 0;
                    }
                    else if (isSaturday)
                    {
                        // Saturday - all overtime
                        dayData.ActualPremiumLabourHours = 0;
                        dayData.ActualRegularLabourHours = 0;
                        dayData.ActualOvertimeLabourHours = hours;
                    }
                    else
                    {
                        // Weekday - regular up to 8, then overtime
                        dayData.ActualPremiumLabourHours = 0;
                        dayData.ActualRegularLabourHours = Math.Min(8m, hours);
                        dayData.ActualOvertimeLabourHours = Math.Max(0m, hours - 8m);
                    }
                }
                else
                {
                    if (isSunday)
                    {
                        // Sunday - all premium
                        dayData.PlannedPremiumLabourHours = hours;
                        dayData.PlannedRegularLabourHours = 0;
                        dayData.PlannedOvertimeLabourHours = 0;
                    }
                    else if (isSaturday)
                    {
                        // Saturday - all overtime
                        dayData.PlannedPremiumLabourHours = 0;
                        dayData.PlannedRegularLabourHours = 0;
                        dayData.PlannedOvertimeLabourHours = hours;
                    }
                    else
                    {
                        // Weekday - regular up to 8, then overtime
                        dayData.PlannedPremiumLabourHours = 0;
                        dayData.PlannedRegularLabourHours = Math.Min(8m, hours);
                        dayData.PlannedOvertimeLabourHours = Math.Max(0m, hours - 8m);
                    }
                }
            }

            // Mark as dirty
            IsDirty = true;
        }
    }

    /// <summary>
    /// Represents a commissioning project with multiple resources
    /// </summary>
    public class CommissioningProject
    {
        // Project identification
        public string ProjectID { get; set; }
        public string ProjectName { get; set; }

        // Project schedule
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalDays { get; set; }

        // Project financials
        public decimal InitialEstimate { get; set; }
        public decimal? PlannedTotal { get; set; }
        public decimal? CurrentTotal { get; set; }
        public decimal? ForecastTotal { get; set; }

        // Resource list
        public List<CommissioningResource> Resources { get; } = new List<CommissioningResource>();

        // Tracking state
        public bool IsDirty { get; set; } = true;

        // Constructor
        public CommissioningProject()
        {
            ProjectID = Guid.NewGuid().ToString();
            ProjectName = "New Commissioning Project";
            TotalDays = 7; // Default to one week
        }

        // Method to calculate all project totals
        public void CalculateTotals()
        {
            decimal planned = 0;
            decimal current = 0;

            foreach (var resource in Resources)
            {
                planned += resource.CalculatePlannedTotal();
                current += resource.CalculateActualTotal();
            }

            // Calculate forecast based on actuals + remaining planned
            decimal forecast = current;
            int completedDays = 0;
            int totalDays = 0;

            foreach (var resource in Resources)
            {
                if (resource.DailyData != null)
                {
                    // Count days with actual data (considered completed)
                    foreach (var entry in resource.DailyData)
                    {
                        var day = entry.Key;
                        var dayData = entry.Value;

                        // Check if this day has actuals recorded
                        bool hasActuals =
                            dayData.ActualRegularLabourHours > 0 ||
                            dayData.ActualOvertimeLabourHours > 0 ||
                            dayData.ActualPremiumLabourHours > 0 ||
                            dayData.ActualRegularTravelHours > 0 ||
                            dayData.ActualOvertimeTravelHours > 0 ||
                            dayData.ActualPremiumTravelHours > 0;

                        if (hasActuals)
                        {
                            completedDays++;
                        }

                        totalDays++;
                    }
                }
            }

            // Calculate completion percentage
            decimal completionPercent = totalDays > 0 ?
                (decimal)completedDays / totalDays : 0;

            // If not everything is complete, add remaining planned
            if (completionPercent < 1)
            {
                forecast += (planned - current) * (1 - completionPercent);
            }

            // Update properties
            PlannedTotal = planned;
            CurrentTotal = current;
            ForecastTotal = forecast;

            // Reset dirty flag
            IsDirty = false;
        }

        // Method to initialize all resources from schedule settings
        public void InitializeAllResources()
        {
            foreach (var resource in Resources)
            {
                resource.InitializeFromSchedule();
            }

            // Mark as dirty
            IsDirty = true;
        }

        // Add a new resource to the project
        public CommissioningResource AddResource()
        {
            var resource = new CommissioningResource();
            Resources.Add(resource);
            IsDirty = true;
            return resource;
        }

        // Remove a resource from the project
        public bool RemoveResource(string resourceId)
        {
            var resource = Resources.FirstOrDefault(r => r.ResourceID == resourceId);
            if (resource != null)
            {
                Resources.Remove(resource);
                IsDirty = true;
                return true;
            }
            return false;
        }
    }
}