using LabourBudgetCalculator.Models;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LabourBudgetCalculator
{
    [Serializable]
    public class CommissioningResource
    {
        public string ResourceID { get; set; }
        public string TechnicianName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Rate information
        public decimal RegularLabourRate { get; set; }
        public decimal OvertimeLabourRate { get; set; }
        public decimal PremiumLabourRate { get; set; }
        public decimal RegularTravelRate { get; set; }
        public decimal OvertimeTravelRate { get; set; }
        public decimal PremiumTravelRate { get; set; }

        // Travel and expense information
        public bool SeparateTravelTo { get; set; }
        public bool SeparateTravelFrom { get; set; }
        public string TravelMethod { get; set; }
        public decimal TravelDistance { get; set; }
        public decimal TravelTime { get; set; }
        public decimal DailyTravelDistance { get; set; }
        public decimal DailyTravelTime { get; set; }

        // Expense rates
        public decimal FlightCost { get; set; }
        public bool RentalCarRequired { get; set; }
        public decimal RentalCarRate { get; set; }
        public bool HotelRequired { get; set; }
        public decimal HotelRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal PerDiemRate { get; set; }

        // Daily schedule
        public List<CommissioningDay> DailySchedule { get; set; }

        // Additional properties needed for CommissioningResultsWindow
        public Dictionary<int, ResourceDayData> DailyData { get; set; } = new Dictionary<int, ResourceDayData>();
        public bool IsDirty { get; set; } = false;
        public int DaysOnSite { get; set; }
        public decimal HoursPerDay { get; set; }
        public string DefaultStartTime { get; set; }
        public decimal LunchDuration { get; set; }

        public CommissioningResource()
        {
            DailySchedule = new List<CommissioningDay>();
        }

        public void InitializeFromSchedule()
        {
            // Create default daily data entries
            for (int day = 0; day < DaysOnSite; day++)
            {
                if (!DailyData.ContainsKey(day))
                {
                    DailyData[day] = new ResourceDayData();

                    // Set some default values
                    DateTime date = StartDate.AddDays(day);
                    bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                    if (!isWeekend)
                    {
                        DailyData[day].PlannedRegularLabourHours = HoursPerDay;
                        DailyData[day].PlannedPerDiemCost = PerDiemRate;
                    }
                }
            }

            // Add travel days if needed
            if (SeparateTravelTo && !DailyData.ContainsKey(-1))
            {
                DailyData[-1] = new ResourceDayData
                {
                    PlannedRegularTravelHours = 8,
                    PlannedPerDiemCost = PerDiemRate
                };
            }

            if (SeparateTravelFrom && !DailyData.ContainsKey(DaysOnSite))
            {
                DailyData[DaysOnSite] = new ResourceDayData
                {
                    PlannedRegularTravelHours = 8,
                    PlannedPerDiemCost = PerDiemRate
                };
            }

            IsDirty = true;
        }
    }
}