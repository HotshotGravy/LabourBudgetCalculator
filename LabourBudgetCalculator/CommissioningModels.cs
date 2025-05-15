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
        public List<CommissioningResource> Resources { get; set; }

        public CommissioningProject()
        {
            Resources = new List<CommissioningResource>();
        }
    }

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

        public CommissioningResource()
        {
            DailySchedule = new List<CommissioningDay>();
        }
    }

    [Serializable]
    public class CommissioningDay
    {
        public DateTime Date { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal LunchDuration { get; set; } // in hours
        public decimal LaborHours { get; set; }
        public decimal TravelHours { get; set; }
        public bool IsHoldoverDay { get; set; }
        public bool IsTravelDay { get; set; }

        // Actual tracking (for future use)
        public decimal ActualLaborHours { get; set; }
        public decimal ActualTravelHours { get; set; }
    }
}