using System;

namespace LabourBudgetCalculator
{
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