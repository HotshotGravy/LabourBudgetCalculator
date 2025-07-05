import { ResourceDayData, ResourceDayDataClass } from './ResourceDayData';
import { DailyHourBreakdown, DailyHourBreakdownClass } from './DailyHourBreakdown';
import { HourCategorizerUtil } from '../utils/HourCategorizerUtil';

export interface CommissioningResource {
  // Basic properties
  resourceID: string;
  technicianName: string;
  startDate: Date;
  daysOnSite: number;
  hoursPerDay: number;
  defaultStartTime: string;
  lunchDuration: number;
  rateSheetName: string;
  discountPercent: number;
  isEmergency: boolean;

  // Rates
  regularLabourRate: number;
  overtimeLabourRate: number;
  premiumLabourRate: number;
  regularTravelRate: number;
  overtimeTravelRate: number;
  premiumTravelRate: number;

  // Travel settings
  separateTravelTo: boolean;
  separateTravelFrom: boolean;
  travelMethod: string;
  travelDistance: number;
  travelTime: number;
  dailyTravelDistance: number;
  dailyTravelTime: number;

  // Expense settings
  flightCost: number;
  rentalCarRequired: boolean;
  rentalCarRate: number;
  hotelRequired: boolean;
  hotelRate: number;
  mileageRate: number;
  perDiemRate: number;
  otherExpenses: number;

  // Daily data
  dailyData: { [key: number]: ResourceDayData };

  // Calculated totals
  plannedResourceTotal: number;
  actualResourceTotal: number;
  forecastResourceTotal: number;
  plannedServiceAndTravelChargesTotal: number;
  actualServiceAndTravelChargesTotal: number;
  plannedExpensesTotal: number;
  actualExpensesTotal: number;

  // State
  isDirty: boolean;
}

export class CommissioningResourceClass implements CommissioningResource {
  // Basic properties
  resourceID: string;
  technicianName: string = 'New Resource';
  startDate: Date = new Date();
  daysOnSite: number = 5;
  hoursPerDay: number = 8;
  defaultStartTime: string = '07:00';
  lunchDuration: number = 0.5;
  rateSheetName: string = '';
  discountPercent: number = 0;
  isEmergency: boolean = false;

  // Rates
  regularLabourRate: number = 0;
  overtimeLabourRate: number = 0;
  premiumLabourRate: number = 0;
  regularTravelRate: number = 0;
  overtimeTravelRate: number = 0;
  premiumTravelRate: number = 0;

  // Travel settings
  separateTravelTo: boolean = false;
  separateTravelFrom: boolean = false;
  travelMethod: string = 'Driving';
  travelDistance: number = 0;
  travelTime: number = 0;
  dailyTravelDistance: number = 0;
  dailyTravelTime: number = 0;

  // Expense settings
  flightCost: number = 0;
  rentalCarRequired: boolean = false;
  rentalCarRate: number = 0;
  hotelRequired: boolean = false;
  hotelRate: number = 0;
  mileageRate: number = 0;
  perDiemRate: number = 0;
  otherExpenses: number = 0;

  // Daily data
  dailyData: { [key: number]: ResourceDayData } = {};

  // Calculated totals
  plannedResourceTotal: number = 0;
  actualResourceTotal: number = 0;
  forecastResourceTotal: number = 0;
  plannedServiceAndTravelChargesTotal: number = 0;
  actualServiceAndTravelChargesTotal: number = 0;
  plannedExpensesTotal: number = 0;
  actualExpensesTotal: number = 0;

  // State
  isDirty: boolean = true;

  constructor(data?: Partial<CommissioningResource>) {
    this.resourceID = crypto.randomUUID();
    
    if (data) {
      Object.assign(this, data);
      if (data.startDate) {
        this.startDate = new Date(data.startDate);
      }
      if (data.dailyData) {
        // Convert dates in dailyData
        this.dailyData = {};
        Object.entries(data.dailyData).forEach(([key, value]) => {
          this.dailyData[parseInt(key)] = {
            ...value,
            date: new Date(value.date)
          };
        });
      }
    }
  }

  initializeFromSchedule(): void {
    try {
      const newDailyData: { [key: number]: ResourceDayData } = {};
      const effectiveStartDate = this.startDate;
      let totalEngagementDays = this.daysOnSite;
      if (this.separateTravelTo) totalEngagementDays++;
      if (this.separateTravelFrom) totalEngagementDays++;
      let currentEngagementDay = 0;

      // For performance, pre-calculate the discount factor
      const discountFactor = 1 - (this.discountPercent / 100);

      // --- Separate Travel To Site Day ---
      if (this.separateTravelTo) {
        this.processTravelToDay(newDailyData, effectiveStartDate, currentEngagementDay, discountFactor);
        currentEngagementDay++;
      }

      // --- Work Days ---
      this.processWorkDays(newDailyData, effectiveStartDate, totalEngagementDays, currentEngagementDay, discountFactor);
      currentEngagementDay += this.daysOnSite;

      // --- Separate Travel From Site Day ---
      if (this.separateTravelFrom) {
        this.processTravelFromDay(newDailyData, effectiveStartDate, currentEngagementDay, discountFactor);
      }

      // --- Set all Actual values from Planned values ---
      Object.values(newDailyData).forEach(dayEntry => {
        this.copyPlannedToActual(dayEntry);
      });

      this.dailyData = newDailyData;
      this.isDirty = false;
    } catch (error) {
      console.error(`Error in InitializeFromSchedule for ${this.technicianName}:`, error);
      this.dailyData = this.dailyData || {};
      this.isDirty = true;
    }
  }

  private processTravelToDay(
    dailyData: { [key: number]: ResourceDayData },
    effectiveStartDate: Date,
    currentEngagementDay: number,
    discountFactor: number
  ): void {
    const travelToDate = new Date(effectiveStartDate);
    travelToDate.setDate(travelToDate.getDate() - 1);
    const travelToHoursInput = this.travelTime > 0 ? this.travelTime : 8;

    const travelToBreakdown = HourCategorizerUtil.categorizeDailyHours(
      0, travelToHoursInput, travelToDate.getDay(), this.isEmergency, false) as DailyHourBreakdownClass;

    let travelToDay: ResourceDayData;
    if (this.dailyData && this.dailyData[-1]) {
      travelToDay = this.dailyData[-1];
    } else {
      travelToDay = new ResourceDayDataClass();
    }

    travelToDay.date = travelToDate;
    travelToDay.plannedStartTime = this.defaultStartTime;
    travelToDay.plannedRegularLabourHours = travelToBreakdown.regularLabourHours;
    travelToDay.plannedOvertimeLabourHours = travelToBreakdown.overtimeLabourHours;
    travelToDay.plannedPremiumLabourHours = travelToBreakdown.premiumLabourHours;
    travelToDay.plannedRegularTravelHours = travelToBreakdown.regularTravelHours;
    travelToDay.plannedOvertimeTravelHours = travelToBreakdown.overtimeTravelHours;
    travelToDay.plannedPremiumTravelHours = travelToBreakdown.premiumTravelHours;

    const isLastDayForHotel = (currentEngagementDay === this.daysOnSite + (this.separateTravelFrom ? 1 : 0) + 1);
    travelToDay.plannedHotelCost = this.hotelRequired && !isLastDayForHotel ? this.hotelRate : 0;
    travelToDay.plannedRentalCarCost = this.rentalCarRequired ? this.rentalCarRate : 0;
    travelToDay.plannedFlightCost = this.travelMethod === 'Flight' ? this.flightCost : 0;
    travelToDay.plannedMileageCost = this.travelMethod === 'Driving' && this.travelDistance > 0 ? this.travelDistance * this.mileageRate : 0;
    travelToDay.plannedPerDiemCost = travelToBreakdown.getTotalPlannedHours() > 0 ? this.perDiemRate : 0;

    // Calculate end time
    const startTime = this.parseTime(travelToDay.plannedStartTime);
    if (startTime) {
      const endTime = new Date(startTime.getTime() + travelToHoursInput * 60 * 60 * 1000);
      travelToDay.plannedEndTime = this.formatTime(endTime);
    }

    dailyData[-1] = travelToDay;
  }

  private processWorkDays(
    dailyData: { [key: number]: ResourceDayData },
    effectiveStartDate: Date,
    totalEngagementDays: number,
    currentEngagementDay: number,
    discountFactor: number
  ): void {
    for (let dayIndex = 0; dayIndex < this.daysOnSite; dayIndex++) {
      const currentDate = new Date(effectiveStartDate);
      currentDate.setDate(currentDate.getDate() + dayIndex);

      let workDayPlannedLabour = this.hoursPerDay;
      let workDayPlannedTravel = this.dailyTravelTime * 2;

      const isFirstWorkDayForMainTravel = (dayIndex === 0 && !this.separateTravelTo);
      const isLastWorkDayForMainTravel = (dayIndex === this.daysOnSite - 1 && !this.separateTravelFrom);

      if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel) {
        workDayPlannedTravel = (this.travelTime * 2) > workDayPlannedTravel ? (this.travelTime * 2) : workDayPlannedTravel;
      } else if (isFirstWorkDayForMainTravel) {
        workDayPlannedTravel = this.travelTime > workDayPlannedTravel ? this.travelTime : workDayPlannedTravel;
      } else if (isLastWorkDayForMainTravel) {
        workDayPlannedTravel = this.travelTime > workDayPlannedTravel ? this.travelTime : workDayPlannedTravel;
      }

      const breakdown = HourCategorizerUtil.categorizeDailyHours(
        workDayPlannedLabour, workDayPlannedTravel, currentDate.getDay(), this.isEmergency, false) as DailyHourBreakdownClass;

      let entry: ResourceDayData;
      if (this.dailyData && this.dailyData[dayIndex]) {
        entry = this.dailyData[dayIndex];
      } else {
        entry = new ResourceDayDataClass();
      }

      entry.date = currentDate;
      entry.plannedStartTime = this.defaultStartTime;
      entry.plannedRegularLabourHours = breakdown.regularLabourHours;
      entry.plannedOvertimeLabourHours = breakdown.overtimeLabourHours;
      entry.plannedPremiumLabourHours = breakdown.premiumLabourHours;
      entry.plannedRegularTravelHours = breakdown.regularTravelHours;
      entry.plannedOvertimeTravelHours = breakdown.overtimeTravelHours;
      entry.plannedPremiumTravelHours = breakdown.premiumTravelHours;

      const isLastDayForHotel = (currentEngagementDay + dayIndex + 1 === totalEngagementDays);
      entry.plannedHotelCost = this.hotelRequired && !isLastDayForHotel ? this.hotelRate : 0;
      entry.plannedRentalCarCost = this.rentalCarRequired ? this.rentalCarRate : 0;

      entry.plannedFlightCost = 0;
      if (this.travelMethod === 'Flight') {
        if (isFirstWorkDayForMainTravel) entry.plannedFlightCost += this.flightCost;
        if (isLastWorkDayForMainTravel && !(isFirstWorkDayForMainTravel && this.daysOnSite === 1)) entry.plannedFlightCost += this.flightCost;
        if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel && this.daysOnSite === 1) entry.plannedFlightCost = this.flightCost * 2;
      }

      entry.plannedMileageCost = 0;
      if (this.travelMethod === 'Driving') {
        if (isFirstWorkDayForMainTravel) entry.plannedMileageCost += this.travelDistance * this.mileageRate;
        if (isLastWorkDayForMainTravel && !(isFirstWorkDayForMainTravel && this.daysOnSite === 1)) entry.plannedMileageCost += this.travelDistance * this.mileageRate;
        if (isFirstWorkDayForMainTravel && isLastWorkDayForMainTravel && this.daysOnSite === 1) entry.plannedMileageCost = this.travelDistance * 2 * this.mileageRate;

        if (!isFirstWorkDayForMainTravel && !isLastWorkDayForMainTravel && !this.rentalCarRequired && this.dailyTravelDistance > 0) {
          entry.plannedMileageCost = this.dailyTravelDistance * 2 * this.mileageRate;
        }
      }

      entry.plannedPerDiemCost = breakdown.getTotalPlannedHours() > 0 ? this.perDiemRate : 0;

      // Calculate end time
      const startTime = this.parseTime(entry.plannedStartTime);
      if (startTime) {
        const totalWorkHoursForEndTime = entry.plannedRegularLabourHours + entry.plannedOvertimeLabourHours + entry.plannedPremiumLabourHours;
        const endTime = new Date(startTime.getTime() + (totalWorkHoursForEndTime + this.lunchDuration) * 60 * 60 * 1000);
        entry.plannedEndTime = this.formatTime(endTime);
      }

      dailyData[dayIndex] = entry;
    }
  }

  private processTravelFromDay(
    dailyData: { [key: number]: ResourceDayData },
    effectiveStartDate: Date,
    currentEngagementDay: number,
    discountFactor: number
  ): void {
    const travelFromDate = new Date(effectiveStartDate);
    travelFromDate.setDate(travelFromDate.getDate() + this.daysOnSite);
    const travelFromHoursInput = this.travelTime > 0 ? this.travelTime : 8;

    const travelFromBreakdown = HourCategorizerUtil.categorizeDailyHours(
      0, travelFromHoursInput, travelFromDate.getDay(), this.isEmergency, false) as DailyHourBreakdownClass;

    let travelFromDay: ResourceDayData;
    if (this.dailyData && this.dailyData[this.daysOnSite]) {
      travelFromDay = this.dailyData[this.daysOnSite];
    } else {
      travelFromDay = new ResourceDayDataClass();
    }

    travelFromDay.date = travelFromDate;
    travelFromDay.plannedStartTime = this.defaultStartTime;
    travelFromDay.plannedRegularLabourHours = travelFromBreakdown.regularLabourHours;
    travelFromDay.plannedOvertimeLabourHours = travelFromBreakdown.overtimeLabourHours;
    travelFromDay.plannedPremiumLabourHours = travelFromBreakdown.premiumLabourHours;
    travelFromDay.plannedRegularTravelHours = travelFromBreakdown.regularTravelHours;
    travelFromDay.plannedOvertimeTravelHours = travelFromBreakdown.overtimeTravelHours;
    travelFromDay.plannedPremiumTravelHours = travelFromBreakdown.premiumTravelHours;

    travelFromDay.plannedHotelCost = 0;
    travelFromDay.plannedRentalCarCost = this.rentalCarRequired ? this.rentalCarRate : 0;
    travelFromDay.plannedFlightCost = this.travelMethod === 'Flight' ? this.flightCost : 0;
    travelFromDay.plannedMileageCost = this.travelMethod === 'Driving' && this.travelDistance > 0 ? this.travelDistance * this.mileageRate : 0;
    travelFromDay.plannedPerDiemCost = travelFromBreakdown.getTotalPlannedHours() > 0 ? this.perDiemRate : 0;

    // Calculate end time
    const startTime = this.parseTime(travelFromDay.plannedStartTime);
    if (startTime) {
      const endTime = new Date(startTime.getTime() + travelFromHoursInput * 60 * 60 * 1000);
      travelFromDay.plannedEndTime = this.formatTime(endTime);
    }

    dailyData[this.daysOnSite] = travelFromDay;
  }

  private copyPlannedToActual(dayEntry: ResourceDayData): void {
    // Labour Hours
    dayEntry.actualRegularLabourHours = dayEntry.plannedRegularLabourHours;
    dayEntry.actualOvertimeLabourHours = dayEntry.plannedOvertimeLabourHours;
    dayEntry.actualPremiumLabourHours = dayEntry.plannedPremiumLabourHours;

    // Travel Hours
    dayEntry.actualRegularTravelHours = dayEntry.plannedRegularTravelHours;
    dayEntry.actualOvertimeTravelHours = dayEntry.plannedOvertimeTravelHours;
    dayEntry.actualPremiumTravelHours = dayEntry.plannedPremiumTravelHours;

    // Expenses
    dayEntry.actualHotelCost = dayEntry.plannedHotelCost;
    dayEntry.actualRentalCarCost = dayEntry.plannedRentalCarCost;
    dayEntry.actualFlightCost = dayEntry.plannedFlightCost;
    dayEntry.actualMileageCost = dayEntry.plannedMileageCost;
    dayEntry.actualPerDiemCost = dayEntry.plannedPerDiemCost;

    // Start and End Times
    dayEntry.actualStartTime = dayEntry.plannedStartTime;
    dayEntry.actualEndTime = dayEntry.plannedEndTime;
  }

  private getEffectiveRate(regularRate: number, overtimeRate: number, premiumRate: number, isRegular: boolean, isOvertime: boolean): number {
    // Calculate discount factor once
    const discountFactor = 1 - (this.discountPercent / 100);

    // Emergency rate logic - everything uses premium rate
    if (this.isEmergency)
      return premiumRate * discountFactor;

    // Normal rate selection
    if (isRegular)
      return regularRate * discountFactor;
    else if (isOvertime)
      return overtimeRate * discountFactor;
    else
      return premiumRate * discountFactor;
  }

  calculateResourceTotals(): void {
    try {
      this.plannedServiceAndTravelChargesTotal = 0;
      this.actualServiceAndTravelChargesTotal = 0;
      this.plannedExpensesTotal = 0;
      this.actualExpensesTotal = 0;
      let forecastServiceAndTravelChargesTotal = 0;
      let forecastExpensesTotal = 0;

      if (!this.dailyData || Object.keys(this.dailyData).length === 0) {
        this.plannedResourceTotal = 0;
        this.actualResourceTotal = 0;
        this.forecastResourceTotal = 0;
        return;
      }

      // Pre-calculate discount factor and effective rates
      const discountFactor = 1 - (this.discountPercent / 100);

      // Define effective rates
      const effRegLabourRate = (this.isEmergency ? this.premiumLabourRate : this.regularLabourRate) * discountFactor;
      const effOTLabourRate = (this.isEmergency ? this.premiumLabourRate : this.overtimeLabourRate) * discountFactor;
      const effPremLabourRate = this.premiumLabourRate * discountFactor;
      const effRegTravelRate = (this.isEmergency ? this.premiumTravelRate : this.regularTravelRate) * discountFactor;
      const effOTTravelRate = (this.isEmergency ? this.premiumTravelRate : this.overtimeTravelRate) * discountFactor;
      const effPremTravelRate = this.premiumTravelRate * discountFactor;

      const today = new Date();

      // Process all days at once to reduce property access overhead
      const daysInOrder = Object.values(this.dailyData).sort((a, b) => a.date.getTime() - b.date.getTime());

      daysInOrder.forEach(day => {
        // Calculate planned service charges
        const plannedLabourCharges = (day.plannedRegularLabourHours * effRegLabourRate) +
                                    (day.plannedOvertimeLabourHours * effOTLabourRate) +
                                    (day.plannedPremiumLabourHours * effPremLabourRate);

        const plannedTravelCharges = (day.plannedRegularTravelHours * effRegTravelRate) +
                                    (day.plannedOvertimeTravelHours * effOTTravelRate) +
                                    (day.plannedPremiumTravelHours * effPremTravelRate);

        this.plannedServiceAndTravelChargesTotal += plannedLabourCharges + plannedTravelCharges;

        // Calculate planned expenses
        const plannedExpenses = day.plannedHotelCost + day.plannedPerDiemCost + day.plannedMileageCost +
                               day.plannedFlightCost + day.plannedRentalCarCost;
        this.plannedExpensesTotal += plannedExpenses;

        // Calculate actual service charges
        const actualLabourCharges = (day.actualRegularLabourHours * effRegLabourRate) +
                                   (day.actualOvertimeLabourHours * effOTLabourRate) +
                                   (day.actualPremiumLabourHours * effPremLabourRate);

        const actualTravelCharges = (day.actualRegularTravelHours * effRegTravelRate) +
                                   (day.actualOvertimeTravelHours * effOTTravelRate) +
                                   (day.actualPremiumTravelHours * effPremTravelRate);

        this.actualServiceAndTravelChargesTotal += actualLabourCharges + actualTravelCharges;

        // Calculate actual expenses
        const actualExpenses = day.actualHotelCost + day.actualPerDiemCost + day.actualMileageCost +
                              day.actualFlightCost + day.actualRentalCarCost;
        this.actualExpensesTotal += actualExpenses;

        // For forecast: use actuals for past/current days, planned for future days
        if (day.date <= today) {
          // Past or current day - use actual values for forecast
          forecastServiceAndTravelChargesTotal += actualLabourCharges + actualTravelCharges;
          forecastExpensesTotal += actualExpenses;
        } else {
          // Future day - use planned values for forecast
          forecastServiceAndTravelChargesTotal += plannedLabourCharges + plannedTravelCharges;
          forecastExpensesTotal += plannedExpenses;
        }
      });

      // Add other expenses to all totals
      this.plannedExpensesTotal += this.otherExpenses;
      this.actualExpensesTotal += this.otherExpenses;
      forecastExpensesTotal += this.otherExpenses;

      // Calculate final totals
      this.plannedResourceTotal = this.plannedServiceAndTravelChargesTotal + this.plannedExpensesTotal;
      this.actualResourceTotal = this.actualServiceAndTravelChargesTotal + this.actualExpensesTotal;
      this.forecastResourceTotal = forecastServiceAndTravelChargesTotal + forecastExpensesTotal;

      this.isDirty = false;
    } catch (error) {
      console.error(`Error in CalculateResourceTotals for ${this.technicianName}:`, error);
      this.isDirty = true;
    }
  }

  private parseTime(timeString: string): Date | null {
    const [hours, minutes] = timeString.split(':').map(Number);
    if (isNaN(hours) || isNaN(minutes)) return null;
    
    const date = new Date();
    date.setHours(hours, minutes, 0, 0);
    return date;
  }

  private formatTime(date: Date): string {
    return date.toTimeString().slice(0, 5);
  }
}

export interface DailyDataKvp {
  dayKey: number;
  data: ResourceDayData;
} 