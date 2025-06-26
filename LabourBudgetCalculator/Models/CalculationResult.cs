using System;
using System.Collections.Generic;

namespace LabourBudgetCalculator.Models
{
    [Serializable]
    public class CalculationResult
    {
        public decimal TotalLabourCost { get; set; }
        public decimal TotalTravelCost { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrandTotal { get; set; }
        public int TotalDays { get; set; }
        public int TotalLabourHours { get; set; }
        public int TotalTravelHours { get; set; }

        // Daily breakdown
        public List<DayDetail> DayDetails { get; set; } = new List<DayDetail>();
    }

    [Serializable]
    public class DayDetail
    {
        public int DayNumber { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public CalculationDayType Type { get; set; }
        public int RegularLabourHours { get; set; }
        public int OvertimeLabourHours { get; set; }
        public int PremiumLabourHours { get; set; }
        public int TotalLabourHours { get; set; }
        public int RegularTravelHours { get; set; }
        public int OvertimeTravelHours { get; set; }
        public int PremiumTravelHours { get; set; }
        public int TotalTravelHours { get; set; }
        public decimal LabourCost { get; set; }
        public decimal TravelCost { get; set; }
        public decimal HotelCost { get; set; }
        public decimal PerDiem { get; set; }
        public decimal MileageCost { get; set; }
        public decimal RentalCarCost { get; set; }
        public decimal AirfareCost { get; set; }
        public decimal TotalDayCost { get; set; }
    }

    public enum CalculationDayType
    {
        WorkDay,
        TravelTo,
        TravelFrom,
        None
    }
}