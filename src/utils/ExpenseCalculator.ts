import { ResourceDayData } from '../models/ResourceDayData';

export class ExpenseCalculator {
  /**
   * Calculates the mileage cost based on travel configuration
   */
  static calculateMileageCost(
    mileageRate: number,
    travelDistance: number,
    dailyTravelDistance: number,
    isFirstOrLastDay: boolean,
    isRentalCarUsed: boolean,
    travelMethod: string
  ): number {
    let mileageCost = 0;

    if (isFirstOrLastDay && travelDistance > 0) {
      mileageCost = mileageRate * travelDistance;
    } else if (dailyTravelDistance > 0 && !isRentalCarUsed) {
      mileageCost = mileageRate * (dailyTravelDistance * 2);
    }
    return mileageCost;
  }

  /**
   * Calculates the hotel cost based on travel configuration
   */
  static calculateHotelCost(
    hotelRate: number,
    hotelRequired: boolean,
    isLastDay: boolean
  ): number {
    if (!hotelRequired || isLastDay)
      return 0;
    return hotelRate;
  }

  /**
   * Calculates the rental car cost based on travel configuration
   */
  static calculateRentalCarCost(
    rentalCarRate: number,
    rentalCarRequired: boolean
  ): number {
    if (!rentalCarRequired)
      return 0;
    return rentalCarRate;
  }

  /**
   * Calculates the flight cost based on travel configuration
   */
  static calculateFlightCost(
    flightCost: number,
    travelMethod: string,
    isTravelDay: boolean,
    isFirstDay: boolean,
    isLastDay: boolean,
    separateTravelTo: boolean,
    separateTravelFrom: boolean
  ): number {
    if (travelMethod !== 'Flight')
      return 0;

    if (isTravelDay ||
        (isFirstDay && !separateTravelTo) ||
        (isLastDay && !separateTravelFrom)) {
      return flightCost;
    }
    return 0;
  }

  /**
   * Calculates the per diem cost based on work configuration
   */
  static calculatePerDiemCost(
    perDiemRate: number,
    laborHours: number,
    travelHours: number
  ): number {
    if (laborHours > 0 || travelHours > 0)
      return perDiemRate;
    return 0;
  }

  /**
   * Calculates the total expenses with appropriate markup
   */
  static calculateTotalExpensesWithMarkup(
    hotelCost: number,
    rentalCarCost: number,
    flightCost: number,
    mileageCost: number,
    perDiemCost: number,
    otherExpenses: number,
    markupPercentage: number = 10
  ): number {
    const expensesWithMarkup = (hotelCost + rentalCarCost + flightCost) *
                               (1 + (markupPercentage / 100));
    const totalExpenses = expensesWithMarkup + mileageCost + perDiemCost + otherExpenses;
    return totalExpenses;
  }

  /**
   * Calculates expenses for a day based on resource configuration
   */
  static calculateDayExpenses(
    dayData: ResourceDayData,
    resource: any, // CommissioningResource type will be defined later
    dayIndex: number,
    totalDays: number,
    separateTravelTo: boolean,
    separateTravelFrom: boolean
  ): void {
    const isFirstDay = dayIndex === 0;
    const isLastDay = dayIndex === (totalDays - 1);
    const isTravelToDay = separateTravelTo && isFirstDay;
    const isTravelFromDay = separateTravelFrom && isLastDay;
    const isTravelDay = isTravelToDay || isTravelFromDay;

    console.log(`Day ${dayIndex}: First=${isFirstDay}, Last=${isLastDay}, TravelTo=${isTravelToDay}, TravelFrom=${isTravelFromDay}`);
    console.log(`Travel Method: ${resource.travelMethod}, Hotel: ${resource.hotelRequired}, Rental: ${resource.rentalCarRequired}`);

    dayData.plannedMileageCost = this.calculateMileageCost(
      resource.mileageRate, resource.travelDistance, resource.dailyTravelDistance,
      isFirstDay || isLastDay, resource.rentalCarRequired, resource.travelMethod);

    dayData.plannedHotelCost = this.calculateHotelCost(
      resource.hotelRate, resource.hotelRequired, isLastDay);

    dayData.plannedRentalCarCost = this.calculateRentalCarCost(
      resource.rentalCarRate, resource.rentalCarRequired);

    dayData.plannedFlightCost = this.calculateFlightCost(
      resource.flightCost, resource.travelMethod, isTravelDay,
      isFirstDay, isLastDay, separateTravelTo, separateTravelFrom);

    const laborHours = this.getPlannedLabourHoursTotal(dayData);
    const travelHours = this.getPlannedTravelHoursTotal(dayData);
    dayData.plannedPerDiemCost = this.calculatePerDiemCost(
      resource.perDiemRate, laborHours, travelHours);

    console.log(`Calculated expenses: Mileage=${dayData.plannedMileageCost}, Hotel=${dayData.plannedHotelCost}, ` +
      `Rental=${dayData.plannedRentalCarCost}, Flight=${dayData.plannedFlightCost}, PerDiem=${dayData.plannedPerDiemCost}`);
  }

  /**
   * Calculates all expenses for a resource
   */
  static calculateResourceExpenses(resource: any): void {
    if (!resource.dailyData || Object.keys(resource.dailyData).length === 0)
      return;

    let totalEngagementDays = 0;
    if (resource.dailyData[-1]) totalEngagementDays++; // Travel To
    totalEngagementDays += resource.daysOnSite;
    if (resource.dailyData[resource.daysOnSite]) totalEngagementDays++; // Travel From

    // Create a sorted list of day entries to process them chronologically for dayIndex
    const orderedDays = Object.entries(resource.dailyData)
      .sort(([, a], [, b]) => new Date((a as ResourceDayData).date).getTime() - new Date((b as ResourceDayData).date).getTime());
    
    if (orderedDays.length === 0) return;

    const actualDayCountInSchedule = orderedDays.length;

    for (let i = 0; i < actualDayCountInSchedule; i++) {
      const [, dayData] = orderedDays[i];

      // Use 'i' as the chronological dayIndex for calculations within CalculateDayExpenses
      this.calculateDayExpenses(
        dayData as ResourceDayData,
        resource,
        i, // Use chronological index 'i'
        actualDayCountInSchedule, // Use the count of days we are actually iterating over
        resource.separateTravelTo,
        resource.separateTravelFrom);
    }
  }

  // Helper methods for ResourceDayData calculations
  private static getPlannedLabourHoursTotal(dayData: ResourceDayData): number {
    return dayData.plannedRegularLabourHours + dayData.plannedOvertimeLabourHours + dayData.plannedPremiumLabourHours;
  }

  private static getPlannedTravelHoursTotal(dayData: ResourceDayData): number {
    return dayData.plannedRegularTravelHours + dayData.plannedOvertimeTravelHours + dayData.plannedPremiumTravelHours;
  }
} 