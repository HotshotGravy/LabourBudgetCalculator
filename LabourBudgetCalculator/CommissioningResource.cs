using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using LabourBudgetCalculator.Models; // For ResourceDayData
using System.IO;
using System.Text;

namespace LabourBudgetCalculator
{
    [Serializable]
    public class CommissioningResource
    {
        // Basic properties
        public string ResourceID { get; set; }
        public string TechnicianName { get; set; }
        public DateTime StartDate { get; set; }
        public int DaysOnSite { get; set; }
        public decimal HoursPerDay { get; set; }
        public string DefaultStartTime { get; set; }
        public decimal LunchDuration { get; set; }
        public string RateSheetName { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsEmergency { get; set; }

        // Rates
        public decimal RegularLabourRate { get; set; }
        public decimal OvertimeLabourRate { get; set; }
        public decimal PremiumLabourRate { get; set; }
        public decimal RegularTravelRate { get; set; }
        public decimal OvertimeTravelRate { get; set; }
        public decimal PremiumTravelRate { get; set; }

        // Travel settings
        public bool SeparateTravelTo { get; set; }
        public bool SeparateTravelFrom { get; set; }
        public string TravelMethod { get; set; }
        public decimal TravelDistance { get; set; }
        public decimal TravelTime { get; set; }
        public decimal DailyTravelDistance { get; set; }
        public decimal DailyTravelTime { get; set; }

        // Expense settings
        public decimal FlightCost { get; set; }
        public bool RentalCarRequired { get; set; }
        public decimal RentalCarRate { get; set; }
        public bool HotelRequired { get; set; }
        public decimal HotelRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal PerDiemRate { get; set; }
        public decimal OtherExpenses { get; set; }

        // The runtime dictionary - not directly serialized
        [XmlIgnore]
        public Dictionary<int, ResourceDayData> DailyData { get; set; }

        // XML serialization surrogate property
        [XmlArray("DailyDataItems")]
        [XmlArrayItem("DayEntry")]
        public DailyDataKvp[] DailyDataForXml
        {
            get
            {
                if (DailyData == null || !DailyData.Any())
                {
                    return new DailyDataKvp[0]; // Return empty array instead of null
                }
                return DailyData.Select(kvp => new DailyDataKvp { DayKey = kvp.Key, Data = kvp.Value }).ToArray();
            }
            set
            {
                System.Diagnostics.Debug.WriteLine($"DailyDataForXml setter called for {TechnicianName ?? "unnamed resource"}. Items count: {value?.Length ?? 0}");
                DailyData = new Dictionary<int, ResourceDayData>();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"Processing day {item.DayKey}, Date: {item.Data?.Date}");

                            // Ensure day data has valid defaults
                            if (item.Data == null)
                                item.Data = new ResourceDayData();

                            // Validate date - set to minimum date if invalid
                            if (item.Data.Date == DateTime.MinValue)
                            {
                                item.Data.Date = StartDate.AddDays(item.DayKey);
                                System.Diagnostics.Debug.WriteLine($"Set date for day {item.DayKey} to {item.Data.Date}");
                            }

                            if (!DailyData.ContainsKey(item.DayKey))
                            {
                                DailyData.Add(item.DayKey, item.Data);
                                System.Diagnostics.Debug.WriteLine($"Added day {item.DayKey} to DailyData dictionary");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error adding daily data item for day {item.DayKey}: {ex.Message}");
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"DailyDataForXml setter finished. Final DailyData count: {DailyData?.Count ?? 0}");
            }
        }

        // Non-serialized runtime properties
        [XmlIgnore]
        public bool IsDirty { get; set; }

        [XmlIgnore] public decimal PlannedResourceTotal { get; private set; }
        [XmlIgnore] public decimal ActualResourceTotal { get; private set; }
        [XmlIgnore] public decimal ForecastResourceTotal { get; private set; }
        [XmlIgnore] public decimal PlannedServiceAndTravelChargesTotal { get; private set; }
        [XmlIgnore] public decimal ActualServiceAndTravelChargesTotal { get; private set; }
        [XmlIgnore] public decimal PlannedExpensesTotal { get; private set; }
        [XmlIgnore] public decimal ActualExpensesTotal { get; private set; }

        public CommissioningResource()
        {
            ResourceID = Guid.NewGuid().ToString();
            TechnicianName = "New Resource";
            DailyData = new Dictionary<int, ResourceDayData>();
            IsDirty = true;
            StartDate = DateTime.Today;
            DaysOnSite = 5;
            HoursPerDay = 8m;
            DefaultStartTime = "07:00";
            LunchDuration = 0.5m;
            TravelMethod = "Driving";
        }

        /// <summary>
        /// Rebuilds the DailyData dictionary based on current resource settings
        /// </summary>
        public void InitializeFromSchedule()
        {
            try
            {
                var newDailyData = new Dictionary<int, ResourceDayData>();
                DateTime effectiveStartDate = this.StartDate;

                // Handle travel to site day if needed
                if (SeparateTravelTo)
                {
                    ResourceDayData travelToDay = new ResourceDayData
                    {
                        Date = effectiveStartDate.AddDays(-1),
                        PlannedStartTime = DefaultStartTime,
                        ActualStartTime = DefaultStartTime,
                        PlannedRegularTravelHours = TravelTime > 0 ? TravelTime : 8,
                        ActualRegularTravelHours = TravelTime > 0 ? TravelTime : 8,
                        PlannedPerDiemCost = PerDiemRate,
                        ActualPerDiemCost = PerDiemRate,
                        PlannedHotelCost = HotelRequired ? HotelRate : 0,
                        ActualHotelCost = HotelRequired ? HotelRate : 0
                    };

                    // Set end time based on start time and travel duration
                    if (TimeSpan.TryParse(travelToDay.PlannedStartTime, out TimeSpan startTime))
                    {
                        TimeSpan endTime = startTime.Add(TimeSpan.FromHours((double)travelToDay.PlannedRegularTravelHours));
                        travelToDay.PlannedEndTime = endTime.ToString(@"hh\:mm");
                        travelToDay.ActualEndTime = travelToDay.PlannedEndTime;
                    }

                    // Add travel day to dictionary with key -1
                    newDailyData[-1] = travelToDay;
                }

                // Create entries for each work day
                for (int dayIndex = 0; dayIndex < DaysOnSite; dayIndex++)
                {
                    DateTime currentDate = effectiveStartDate.AddDays(dayIndex);

                    // Check if we already have data for this day
                    ResourceDayData existingEntry = null;
                    if (DailyData != null && DailyData.TryGetValue(dayIndex, out existingEntry))
                    {
                        existingEntry.Date = currentDate;
                    }

                    ResourceDayData entry = existingEntry ?? new ResourceDayData
                    {
                        Date = currentDate,
                        PlannedStartTime = DefaultStartTime,
                        ActualStartTime = DefaultStartTime
                    };

                    bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday ||
                                     currentDate.DayOfWeek == DayOfWeek.Sunday;

                    // Default planning values if not already set
                    if (existingEntry == null)
                    {
                        // Set planned hours based on regular work week
                        entry.PlannedRegularLabourHours = isWeekend ? 0 : HoursPerDay;
                        entry.PlannedOvertimeLabourHours = 0;
                        entry.PlannedPremiumLabourHours = isWeekend ? HoursPerDay : 0;

                        // Set daily travel if applicable
                        entry.PlannedRegularTravelHours = DailyTravelTime * 2; // Round trip

                        // Initialize actual to match planned
                        entry.ActualRegularLabourHours = entry.PlannedRegularLabourHours;
                        entry.ActualOvertimeLabourHours = entry.PlannedOvertimeLabourHours;
                        entry.ActualPremiumLabourHours = entry.PlannedPremiumLabourHours;
                        entry.ActualRegularTravelHours = entry.PlannedRegularTravelHours;

                        // Set per diem and hotel costs
                        entry.PlannedPerDiemCost = PerDiemRate;
                        entry.ActualPerDiemCost = PerDiemRate;

                        // Hotel is needed except for the last day
                        entry.PlannedHotelCost = HotelRequired && dayIndex < DaysOnSite - 1 ? HotelRate : 0;
                        entry.ActualHotelCost = entry.PlannedHotelCost;

                        // Set mileage costs based on daily travel
                        if (DailyTravelDistance > 0 && MileageRate > 0)
                        {
                            entry.PlannedMileageCost = DailyTravelDistance * 2 * MileageRate; // Round trip
                            entry.ActualMileageCost = entry.PlannedMileageCost;
                        }

                        // Set rental car if required
                        if (RentalCarRequired)
                        {
                            entry.PlannedRentalCarCost = RentalCarRate;
                            entry.ActualRentalCarCost = RentalCarRate;
                        }
                    }

                    // Calculate planned end time based on start time and work duration
                    if (TimeSpan.TryParse(entry.PlannedStartTime, out TimeSpan st))
                    {
                        decimal totalWorkHours = entry.PlannedRegularLabourHours +
                                               entry.PlannedOvertimeLabourHours +
                                               entry.PlannedPremiumLabourHours;

                        entry.PlannedEndTime = st.Add(TimeSpan.FromHours((double)(totalWorkHours + LunchDuration)))
                                                .ToString(@"hh\:mm");

                        if (string.IsNullOrEmpty(entry.ActualEndTime))
                        {
                            entry.ActualEndTime = entry.PlannedEndTime;
                        }
                    }

                    // Add this day to the dictionary
                    newDailyData[dayIndex] = entry;
                }

                // Handle travel from site day if needed
                if (SeparateTravelFrom)
                {
                    ResourceDayData travelFromDay = new ResourceDayData
                    {
                        Date = effectiveStartDate.AddDays(DaysOnSite),
                        PlannedStartTime = DefaultStartTime,
                        ActualStartTime = DefaultStartTime,
                        PlannedRegularTravelHours = TravelTime > 0 ? TravelTime : 8,
                        ActualRegularTravelHours = TravelTime > 0 ? TravelTime : 8,
                        PlannedPerDiemCost = PerDiemRate,
                        ActualPerDiemCost = PerDiemRate
                        // No hotel needed for travel home day
                    };

                    // Set end time
                    if (TimeSpan.TryParse(travelFromDay.PlannedStartTime, out TimeSpan startTime))
                    {
                        TimeSpan endTime = startTime.Add(TimeSpan.FromHours((double)travelFromDay.PlannedRegularTravelHours));
                        travelFromDay.PlannedEndTime = endTime.ToString(@"hh\:mm");
                        travelFromDay.ActualEndTime = travelFromDay.PlannedEndTime;
                    }

                    // Add travel day to dictionary with key = DaysOnSite
                    newDailyData[DaysOnSite] = travelFromDay;
                }

                // Replace the existing dictionary with the new one
                DailyData = newDailyData;
                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeFromSchedule for {TechnicianName}: {ex.Message}");
                // Create an empty dictionary if initialization fails
                DailyData = new Dictionary<int, ResourceDayData>();
                IsDirty = true;
            }
        }

        /// <summary>
        /// Calculates resource totals based on the DailyData
        /// </summary>
        public void CalculateResourceTotals()
        {
            try
            {
                // Initialize all totals to zero
                PlannedServiceAndTravelChargesTotal = 0;
                ActualServiceAndTravelChargesTotal = 0;
                PlannedExpensesTotal = 0;
                ActualExpensesTotal = 0;

                // If no daily data, nothing to calculate
                if (DailyData == null || !DailyData.Any())
                {
                    PlannedResourceTotal = 0;
                    ActualResourceTotal = 0;
                    ForecastResourceTotal = 0;
                    return;
                }

                // Calculate discount factor
                decimal discountFactor = 1 - (DiscountPercent / 100m);

                // Determine effective rates based on emergency status
                decimal effectiveRegLabourRate = IsEmergency ? PremiumLabourRate : RegularLabourRate;
                decimal effectiveOTLabourRate = IsEmergency ? PremiumLabourRate : OvertimeLabourRate;
                decimal effectivePremLabourRate = PremiumLabourRate;

                decimal effectiveRegTravelRate = IsEmergency ? PremiumTravelRate : RegularTravelRate;
                decimal effectiveOTTravelRate = IsEmergency ? PremiumTravelRate : OvertimeTravelRate;
                decimal effectivePremTravelRate = PremiumTravelRate;

                // Apply discount to rates
                effectiveRegLabourRate *= discountFactor;
                effectiveOTLabourRate *= discountFactor;
                effectivePremLabourRate *= discountFactor;
                effectiveRegTravelRate *= discountFactor;
                effectiveOTTravelRate *= discountFactor;
                effectivePremTravelRate *= discountFactor;

                // Process each day's data
                foreach (var day in DailyData.Values.OrderBy(d => d.Date))
                {
                    // Calculate labour charges
                    PlannedServiceAndTravelChargesTotal += (day.PlannedRegularLabourHours * effectiveRegLabourRate) +
                                                         (day.PlannedOvertimeLabourHours * effectiveOTLabourRate) +
                                                         (day.PlannedPremiumLabourHours * effectivePremLabourRate);

                    ActualServiceAndTravelChargesTotal += (day.ActualRegularLabourHours * effectiveRegLabourRate) +
                                                        (day.ActualOvertimeLabourHours * effectiveOTLabourRate) +
                                                        (day.ActualPremiumLabourHours * effectivePremLabourRate);

                    // Calculate travel charges
                    PlannedServiceAndTravelChargesTotal += (day.PlannedRegularTravelHours * effectiveRegTravelRate) +
                                                         (day.PlannedOvertimeTravelHours * effectiveOTTravelRate) +
                                                         (day.PlannedPremiumTravelHours * effectivePremTravelRate);

                    ActualServiceAndTravelChargesTotal += (day.ActualRegularTravelHours * effectiveRegTravelRate) +
                                                        (day.ActualOvertimeTravelHours * effectiveOTTravelRate) +
                                                        (day.ActualPremiumTravelHours * effectivePremTravelRate);

                    // Calculate expenses
                    PlannedExpensesTotal += day.PlannedHotelCost + day.PlannedPerDiemCost + day.PlannedMileageCost +
                                          day.PlannedFlightCost + day.PlannedRentalCarCost;

                    ActualExpensesTotal += day.ActualHotelCost + day.ActualPerDiemCost + day.ActualMileageCost +
                                         day.ActualFlightCost + day.ActualRentalCarCost;
                }

                // Add other fixed expenses
                PlannedExpensesTotal += OtherExpenses;
                ActualExpensesTotal += OtherExpenses;

                // Calculate resource totals
                PlannedResourceTotal = PlannedServiceAndTravelChargesTotal + PlannedExpensesTotal;
                ActualResourceTotal = ActualServiceAndTravelChargesTotal + ActualExpensesTotal;

                // For forecast, typically use actual for completed days and planned for future days
                // This simplified implementation just uses actual
                ForecastResourceTotal = ActualResourceTotal;

                // Resource is no longer dirty after calculation
                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateResourceTotals for {TechnicianName}: {ex.Message}");
                // Set IsDirty flag to indicate we need to recalculate
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Helper class for XML serialization of the DailyData dictionary's KeyValuePair.
    /// </summary>
    [Serializable]
    public class DailyDataKvp
    {
        public int DayKey { get; set; }
        public ResourceDayData Data { get; set; }

        public DailyDataKvp()
        {
            Data = new ResourceDayData();
        }
    }
}