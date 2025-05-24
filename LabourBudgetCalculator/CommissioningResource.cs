using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using LabourBudgetCalculator.Models; // For ResourceDayData and DailyHourBreakdown
using LabourBudgetCalculator.Helpers; // For HourCategorizerUtil
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
        public bool IsEmergency { get; set; } // Used by HourCategorizerUtil

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
        public decimal TravelTime { get; set; } // Total travel time for separate travel days
        public decimal DailyTravelDistance { get; set; }
        public decimal DailyTravelTime { get; set; } // One-way daily travel

        // Expense settings
        public decimal FlightCost { get; set; }
        public bool RentalCarRequired { get; set; }
        public decimal RentalCarRate { get; set; }
        public bool HotelRequired { get; set; }
        public decimal HotelRate { get; set; }
        public decimal MileageRate { get; set; }
        public decimal PerDiemRate { get; set; }
        public decimal OtherExpenses { get; set; }

        [XmlIgnore]
        public Dictionary<int, ResourceDayData> DailyData { get; set; }

        [XmlArray("DailyDataItems")]
        [XmlArrayItem("DayEntry")]
        public DailyDataKvp[] DailyDataForXml
        {
            get
            {
                if (DailyData == null || !DailyData.Any())
                {
                    return new DailyDataKvp[0];
                }
                return DailyData.Select(kvp => new DailyDataKvp { DayKey = kvp.Key, Data = kvp.Value }).ToArray();
            }
            set
            {
                DailyData = new Dictionary<int, ResourceDayData>();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        try
                        {
                            if (item.Data == null) item.Data = new ResourceDayData();
                            if (item.Data.Date == DateTime.MinValue && StartDate != DateTime.MinValue)
                            {
                                // Attempt to reconstruct date if StartDate is valid
                                item.Data.Date = StartDate.AddDays(item.DayKey >= 0 ? item.DayKey : (item.DayKey == -1 && SeparateTravelTo ? -1 : 0));
                            }
                            if (!DailyData.ContainsKey(item.DayKey))
                            {
                                DailyData.Add(item.DayKey, item.Data);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error adding daily data item for day {item.DayKey}: {ex.Message}");
                        }
                    }
                }
            }
        }

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

        public void InitializeFromSchedule()
        {
            try
            {
                var newDailyData = new Dictionary<int, ResourceDayData>();
                DateTime effectiveStartDate = this.StartDate;
                int totalEngagementDays = DaysOnSite;
                if (SeparateTravelTo) totalEngagementDays++;
                if (SeparateTravelFrom) totalEngagementDays++;
                int currentEngagementDay = 0;

                // --- Separate Travel To Site Day ---
                if (SeparateTravelTo)
                {
                    currentEngagementDay++;
                    DateTime travelToDate = effectiveStartDate.AddDays(-1);
                    decimal travelToHoursInput = TravelTime > 0 ? TravelTime : 8m; // Total travel hours for this day

                    DailyHourBreakdown travelToBreakdown = HourCategorizerUtil.CategorizeDailyHours(
                        0m, travelToHoursInput, travelToDate.DayOfWeek, this.IsEmergency, false);

                    ResourceDayData travelToDay = null;
                    if (DailyData == null || !DailyData.TryGetValue(-1, out travelToDay))
                    {
                        travelToDay = new ResourceDayData();
                    }
                    travelToDay.Date = travelToDate;
                    travelToDay.PlannedStartTime = DefaultStartTime;
                    travelToDay.PlannedRegularLabourHours = travelToBreakdown.RegularLabourHours;
                    travelToDay.PlannedOvertimeLabourHours = travelToBreakdown.OvertimeLabourHours;
                    travelToDay.PlannedPremiumLabourHours = travelToBreakdown.PremiumLabourHours;
                    travelToDay.PlannedRegularTravelHours = travelToBreakdown.RegularTravelHours;
                    travelToDay.PlannedOvertimeTravelHours = travelToBreakdown.OvertimeTravelHours;
                    travelToDay.PlannedPremiumTravelHours = travelToBreakdown.PremiumTravelHours;

                    bool isLastDayForHotel = (currentEngagementDay == totalEngagementDays);
                    travelToDay.PlannedHotelCost = HotelRequired && !isLastDayForHotel ? HotelRate : 0;
                    travelToDay.PlannedRentalCarCost = RentalCarRequired ? RentalCarRate : 0;
                    travelToDay.PlannedFlightCost = TravelMethod == "Flight" ? FlightCost : 0;
                    travelToDay.PlannedMileageCost = TravelMethod == "Driving" && TravelDistance > 0 ? TravelDistance * MileageRate : 0;
                    travelToDay.PlannedPerDiemCost = travelToBreakdown.GetTotalPlannedHours() > 0 ? PerDiemRate : 0;

                    if (TimeSpan.TryParse(travelToDay.PlannedStartTime, out TimeSpan st))
                        travelToDay.PlannedEndTime = st.Add(TimeSpan.FromHours((double)travelToHoursInput)).ToString(@"hh\:mm");
                    newDailyData[-1] = travelToDay;
                }

                // --- Work Days ---
                for (int dayIndex = 0; dayIndex < DaysOnSite; dayIndex++)
                {
                    currentEngagementDay++;
                    DateTime currentDate = effectiveStartDate.AddDays(dayIndex);

                    // Determine total planned labour and travel for this workday
                    decimal workDayPlannedLabour = HoursPerDay;
                    decimal workDayPlannedTravel = DailyTravelTime * 2; // Default daily travel (round trip)

                    bool isFirstWorkDayForMainTravel = (dayIndex == 0 && !SeparateTravelTo);
                    bool isLastWorkDayForMainTravel = (dayIndex == DaysOnSite - 1 && !SeparateTravelFrom);

                    if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel)
                    {
                        // Single work day acting as both arrival and departure travel event
                        // Use TravelTime if it's greater than default daily travel, implies it's the main journey time
                        workDayPlannedTravel = (TravelTime * 2) > workDayPlannedTravel ? (TravelTime * 2) : workDayPlannedTravel; // Assuming TravelTime is one-way
                                                                                                                                  // If TravelTime is already round-trip for this scenario, just assign TravelTime
                    }
                    else if (isFirstWorkDayForMainTravel)
                    {
                        workDayPlannedTravel = TravelTime > workDayPlannedTravel ? TravelTime : workDayPlannedTravel;
                    }
                    else if (isLastWorkDayForMainTravel)
                    {
                        workDayPlannedTravel = TravelTime > workDayPlannedTravel ? TravelTime : workDayPlannedTravel;
                    }
                    // If neither, workDayPlannedTravel remains DailyTravelTime * 2

                    DailyHourBreakdown breakdown = HourCategorizerUtil.CategorizeDailyHours(
                        workDayPlannedLabour, workDayPlannedTravel, currentDate.DayOfWeek, this.IsEmergency, false);

                    ResourceDayData entry = null;
                    if (DailyData == null || !DailyData.TryGetValue(dayIndex, out entry))
                    {
                        entry = new ResourceDayData();
                    }
                    entry.Date = currentDate;
                    entry.PlannedStartTime = DefaultStartTime;
                    entry.PlannedRegularLabourHours = breakdown.RegularLabourHours;
                    entry.PlannedOvertimeLabourHours = breakdown.OvertimeLabourHours;
                    entry.PlannedPremiumLabourHours = breakdown.PremiumLabourHours;
                    entry.PlannedRegularTravelHours = breakdown.RegularTravelHours;
                    entry.PlannedOvertimeTravelHours = breakdown.OvertimeTravelHours;
                    entry.PlannedPremiumTravelHours = breakdown.PremiumTravelHours;

                    bool isLastDayForHotel = (currentEngagementDay == totalEngagementDays);
                    entry.PlannedHotelCost = HotelRequired && !isLastDayForHotel ? HotelRate : 0;
                    entry.PlannedRentalCarCost = RentalCarRequired ? RentalCarRate : 0;

                    entry.PlannedFlightCost = 0;
                    if (TravelMethod == "Flight")
                    {
                        if (isFirstWorkDayForMainTravel) entry.PlannedFlightCost += FlightCost;
                        if (isLastWorkDayForMainTravel && !(isFirstWorkDayForMainTravel && DaysOnSite == 1)) entry.PlannedFlightCost += FlightCost; // Add if distinct from first day event
                        if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel && DaysOnSite == 1) entry.PlannedFlightCost = FlightCost * 2; // If one day site, two flights
                    }

                    entry.PlannedMileageCost = 0;
                    if (TravelMethod == "Driving")
                    {
                        if (isFirstWorkDayForMainTravel) entry.PlannedMileageCost += TravelDistance * MileageRate;
                        if (isLastWorkDayForMainTravel && !(isFirstWorkDayForMainTravel && DaysOnSite == 1)) entry.PlannedMileageCost += TravelDistance * MileageRate;
                        if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel && DaysOnSite == 1) entry.PlannedMileageCost = TravelDistance * 2 * MileageRate; // Round trip mileage

                        if (!isFirstWorkDayForMainTravel && !isLastWorkDayForMainTravel && !RentalCarRequired && DailyTravelDistance > 0)
                        {
                            entry.PlannedMileageCost = DailyTravelDistance * 2 * MileageRate; // Daily commute
                        }
                    }

                    entry.PlannedPerDiemCost = breakdown.GetTotalPlannedHours() > 0 ? PerDiemRate : 0;

                    if (TimeSpan.TryParse(entry.PlannedStartTime, out TimeSpan st))
                    {
                        decimal totalWorkHoursForEndTime = entry.PlannedRegularLabourHours + entry.PlannedOvertimeLabourHours + entry.PlannedPremiumLabourHours;
                        entry.PlannedEndTime = st.Add(TimeSpan.FromHours((double)(totalWorkHoursForEndTime + LunchDuration))).ToString(@"hh\:mm");
                    }
                    newDailyData[dayIndex] = entry;
                }

                // --- Separate Travel From Site Day ---
                if (SeparateTravelFrom)
                {
                    currentEngagementDay++;
                    DateTime travelFromDate = effectiveStartDate.AddDays(DaysOnSite);
                    decimal travelFromHoursInput = TravelTime > 0 ? TravelTime : 8m;

                    DailyHourBreakdown travelFromBreakdown = HourCategorizerUtil.CategorizeDailyHours(
                        0m, travelFromHoursInput, travelFromDate.DayOfWeek, this.IsEmergency, false);

                    ResourceDayData travelFromDay = null;
                    if (DailyData == null || !DailyData.TryGetValue(DaysOnSite, out travelFromDay))
                    {
                        travelFromDay = new ResourceDayData();
                    }
                    travelFromDay.Date = travelFromDate;
                    travelFromDay.PlannedStartTime = DefaultStartTime;
                    travelFromDay.PlannedRegularLabourHours = travelFromBreakdown.RegularLabourHours;
                    travelFromDay.PlannedOvertimeLabourHours = travelFromBreakdown.OvertimeLabourHours;
                    travelFromDay.PlannedPremiumLabourHours = travelFromBreakdown.PremiumLabourHours;
                    travelFromDay.PlannedRegularTravelHours = travelFromBreakdown.RegularTravelHours;
                    travelFromDay.PlannedOvertimeTravelHours = travelFromBreakdown.OvertimeTravelHours;
                    travelFromDay.PlannedPremiumTravelHours = travelFromBreakdown.PremiumTravelHours;

                    travelFromDay.PlannedHotelCost = 0;
                    travelFromDay.PlannedRentalCarCost = RentalCarRequired ? RentalCarRate : 0;
                    travelFromDay.PlannedFlightCost = TravelMethod == "Flight" ? FlightCost : 0;
                    travelFromDay.PlannedMileageCost = TravelMethod == "Driving" && TravelDistance > 0 ? TravelDistance * MileageRate : 0;
                    travelFromDay.PlannedPerDiemCost = travelFromBreakdown.GetTotalPlannedHours() > 0 ? PerDiemRate : 0;

                    if (TimeSpan.TryParse(travelFromDay.PlannedStartTime, out TimeSpan st))
                        travelFromDay.PlannedEndTime = st.Add(TimeSpan.FromHours((double)travelFromHoursInput)).ToString(@"hh\:mm");
                    newDailyData[DaysOnSite] = travelFromDay;
                }

                // Preserve actuals
                if (DailyData != null)
                {
                    foreach (var kvp in DailyData)
                    {
                        if (newDailyData.TryGetValue(kvp.Key, out ResourceDayData newEntryToUpdate) && kvp.Value != null)
                        {
                            // A more robust check for whether actuals have been meaningfully set by user
                            bool actualsAreSet = kvp.Value.ActualRegularLabourHours != 0 || kvp.Value.ActualOvertimeLabourHours != 0 || /* ... more checks ...*/
                                                 kvp.Value.ActualHotelCost != default || // Check against default, not just 0
                                                 (kvp.Value.ActualStartTime != null && kvp.Value.ActualStartTime != newEntryToUpdate.PlannedStartTime) ||
                                                 (kvp.Value.ActualEndTime != null && kvp.Value.ActualEndTime != newEntryToUpdate.PlannedEndTime);


                            if (actualsAreSet) // Simplified condition, refine if needed
                            {
                                newEntryToUpdate.ActualRegularLabourHours = kvp.Value.ActualRegularLabourHours;
                                newEntryToUpdate.ActualOvertimeLabourHours = kvp.Value.ActualOvertimeLabourHours;
                                newEntryToUpdate.ActualPremiumLabourHours = kvp.Value.ActualPremiumLabourHours;
                                newEntryToUpdate.ActualRegularTravelHours = kvp.Value.ActualRegularTravelHours;
                                newEntryToUpdate.ActualOvertimeTravelHours = kvp.Value.ActualOvertimeTravelHours;
                                newEntryToUpdate.ActualPremiumTravelHours = kvp.Value.ActualPremiumTravelHours;
                                newEntryToUpdate.ActualStartTime = kvp.Value.ActualStartTime;
                                newEntryToUpdate.ActualEndTime = kvp.Value.ActualEndTime;
                                newEntryToUpdate.ActualHotelCost = kvp.Value.ActualHotelCost;
                                newEntryToUpdate.ActualMileageCost = kvp.Value.ActualMileageCost;
                                newEntryToUpdate.ActualPerDiemCost = kvp.Value.ActualPerDiemCost;
                                newEntryToUpdate.ActualFlightCost = kvp.Value.ActualFlightCost;
                                newEntryToUpdate.ActualRentalCarCost = kvp.Value.ActualRentalCarCost;
                            }
                            else
                            { // If no significant actuals were set, copy from planned to keep actuals in sync initially
                                newEntryToUpdate.ActualRegularLabourHours = newEntryToUpdate.PlannedRegularLabourHours;
                                newEntryToUpdate.ActualOvertimeLabourHours = newEntryToUpdate.PlannedOvertimeLabourHours;
                                newEntryToUpdate.ActualPremiumLabourHours = newEntryToUpdate.PlannedPremiumLabourHours;
                                newEntryToUpdate.ActualRegularTravelHours = newEntryToUpdate.PlannedRegularTravelHours;
                                newEntryToUpdate.ActualOvertimeTravelHours = newEntryToUpdate.PlannedOvertimeTravelHours;
                                newEntryToUpdate.ActualPremiumTravelHours = newEntryToUpdate.PlannedPremiumTravelHours;
                                newEntryToUpdate.ActualStartTime = newEntryToUpdate.PlannedStartTime;
                                newEntryToUpdate.ActualEndTime = newEntryToUpdate.PlannedEndTime;
                                newEntryToUpdate.ActualHotelCost = newEntryToUpdate.PlannedHotelCost;
                                newEntryToUpdate.ActualMileageCost = newEntryToUpdate.PlannedMileageCost;
                                newEntryToUpdate.ActualPerDiemCost = newEntryToUpdate.PlannedPerDiemCost;
                                newEntryToUpdate.ActualFlightCost = newEntryToUpdate.PlannedFlightCost;
                                newEntryToUpdate.ActualRentalCarCost = newEntryToUpdate.PlannedRentalCarCost;
                            }
                        }
                    }
                }

                DailyData = newDailyData;
                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeFromSchedule for {TechnicianName}: {ex.Message}");
                DailyData = DailyData ?? new Dictionary<int, ResourceDayData>();
                IsDirty = true;
            }
        }

        public void CalculateResourceTotals()
        {
            try
            {
                PlannedServiceAndTravelChargesTotal = 0;
                ActualServiceAndTravelChargesTotal = 0;
                PlannedExpensesTotal = 0;
                ActualExpensesTotal = 0;

                if (DailyData == null || !DailyData.Any())
                {
                    PlannedResourceTotal = 0; ActualResourceTotal = 0; ForecastResourceTotal = 0;
                    return;
                }

                decimal discountFactor = 1 - (DiscountPercent / 100m);
                decimal effRegLabourRate = (IsEmergency ? PremiumLabourRate : RegularLabourRate) * discountFactor;
                decimal effOTLabourRate = (IsEmergency ? PremiumLabourRate : OvertimeLabourRate) * discountFactor;
                decimal effPremLabourRate = PremiumLabourRate * discountFactor;
                decimal effRegTravelRate = (IsEmergency ? PremiumTravelRate : RegularTravelRate) * discountFactor;
                decimal effOTTravelRate = (IsEmergency ? PremiumTravelRate : OvertimeTravelRate) * discountFactor;
                decimal effPremTravelRate = PremiumTravelRate * discountFactor;

                foreach (var day in DailyData.Values.OrderBy(d => d.Date))
                {
                    PlannedServiceAndTravelChargesTotal += (day.PlannedRegularLabourHours * effRegLabourRate) +
                                                           (day.PlannedOvertimeLabourHours * effOTLabourRate) +
                                                           (day.PlannedPremiumLabourHours * effPremLabourRate) +
                                                           (day.PlannedRegularTravelHours * effRegTravelRate) +
                                                           (day.PlannedOvertimeTravelHours * effOTTravelRate) +
                                                           (day.PlannedPremiumTravelHours * effPremTravelRate);

                    ActualServiceAndTravelChargesTotal += (day.ActualRegularLabourHours * effRegLabourRate) +
                                                          (day.ActualOvertimeLabourHours * effOTLabourRate) +
                                                          (day.ActualPremiumLabourHours * effPremLabourRate) +
                                                          (day.ActualRegularTravelHours * effRegTravelRate) +
                                                          (day.ActualOvertimeTravelHours * effOTTravelRate) +
                                                          (day.ActualPremiumTravelHours * effPremTravelRate);

                    PlannedExpensesTotal += day.PlannedHotelCost + day.PlannedPerDiemCost + day.PlannedMileageCost +
                                          day.PlannedFlightCost + day.PlannedRentalCarCost;
                    ActualExpensesTotal += day.ActualHotelCost + day.ActualPerDiemCost + day.ActualMileageCost +
                                         day.ActualFlightCost + day.ActualRentalCarCost;
                }

                PlannedExpensesTotal += OtherExpenses;
                ActualExpensesTotal += OtherExpenses;

                PlannedResourceTotal = PlannedServiceAndTravelChargesTotal + PlannedExpensesTotal;
                ActualResourceTotal = ActualServiceAndTravelChargesTotal + ActualExpensesTotal;
                ForecastResourceTotal = ActualResourceTotal; // Simplified forecast
                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateResourceTotals for {TechnicianName}: {ex.Message}");
                IsDirty = true;
            }
        }
    }

    [Serializable]
    public class DailyDataKvp
    {
        public int DayKey { get; set; }
        public ResourceDayData Data { get; set; }
        public DailyDataKvp() { Data = new ResourceDayData(); }
    }
}