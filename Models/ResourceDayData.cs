namespace LabourBudgetCalculator.Models
{
    [Serializable]
    public class ResourceDayData
    {
        public DateTime Date { get; set; }
        // ... existing code ...
        public decimal GetActualExpensesTotal()
        {
            return ActualHotelCost + ActualPerDiemCost + ActualMileageCost +
                   ActualFlightCost + ActualRentalCarCost;
        }

        // Add manual override for day type
        public DayType? ManualDayType { get; set; } // null = auto, otherwise manual override
    }

    // Enum for day type override
    public enum DayType
    {
        Work,
        Travel,
        Holdover,
        Nil
    }
} 