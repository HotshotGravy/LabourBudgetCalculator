export enum DayType {
  Work = 'Work',
  Travel = 'Travel',
  Holdover = 'Holdover',
  Nil = 'Nil'
}

export interface ResourceDayData {
  date: Date;
  manualDayType?: DayType;
  
  // Planned values
  plannedStartTime: string;
  plannedEndTime: string;
  plannedRegularLabourHours: number;
  plannedOvertimeLabourHours: number;
  plannedPremiumLabourHours: number;
  plannedRegularTravelHours: number;
  plannedOvertimeTravelHours: number;
  plannedPremiumTravelHours: number;
  plannedHotelCost: number;
  plannedPerDiemCost: number;
  plannedMileageCost: number;
  plannedFlightCost: number;
  plannedRentalCarCost: number;
  
  // Actual values
  actualStartTime: string;
  actualEndTime: string;
  actualRegularLabourHours: number;
  actualOvertimeLabourHours: number;
  actualPremiumLabourHours: number;
  actualRegularTravelHours: number;
  actualOvertimeTravelHours: number;
  actualPremiumTravelHours: number;
  actualHotelCost: number;
  actualPerDiemCost: number;
  actualMileageCost: number;
  actualFlightCost: number;
  actualRentalCarCost: number;
}

export class ResourceDayDataClass implements ResourceDayData {
  date: Date = new Date();
  manualDayType?: DayType;
  
  // Planned values
  plannedStartTime: string = '07:00';
  plannedEndTime: string = '15:30';
  plannedRegularLabourHours: number = 0;
  plannedOvertimeLabourHours: number = 0;
  plannedPremiumLabourHours: number = 0;
  plannedRegularTravelHours: number = 0;
  plannedOvertimeTravelHours: number = 0;
  plannedPremiumTravelHours: number = 0;
  plannedHotelCost: number = 0;
  plannedPerDiemCost: number = 0;
  plannedMileageCost: number = 0;
  plannedFlightCost: number = 0;
  plannedRentalCarCost: number = 0;
  
  // Actual values
  actualStartTime: string = '07:00';
  actualEndTime: string = '15:30';
  actualRegularLabourHours: number = 0;
  actualOvertimeLabourHours: number = 0;
  actualPremiumLabourHours: number = 0;
  actualRegularTravelHours: number = 0;
  actualOvertimeTravelHours: number = 0;
  actualPremiumTravelHours: number = 0;
  actualHotelCost: number = 0;
  actualPerDiemCost: number = 0;
  actualMileageCost: number = 0;
  actualFlightCost: number = 0;
  actualRentalCarCost: number = 0;

  constructor(data?: Partial<ResourceDayData>) {
    if (data) {
      Object.assign(this, data);
      if (data.date) {
        this.date = new Date(data.date);
      }
    }
  }

  // Helper methods to calculate totals
  getPlannedLabourHoursTotal(): number {
    return this.plannedRegularLabourHours + this.plannedOvertimeLabourHours + this.plannedPremiumLabourHours;
  }

  getActualLabourHoursTotal(): number {
    return this.actualRegularLabourHours + this.actualOvertimeLabourHours + this.actualPremiumLabourHours;
  }

  getPlannedTravelHoursTotal(): number {
    return this.plannedRegularTravelHours + this.plannedOvertimeTravelHours + this.plannedPremiumTravelHours;
  }

  getActualTravelHoursTotal(): number {
    return this.actualRegularTravelHours + this.actualOvertimeTravelHours + this.actualPremiumTravelHours;
  }

  getPlannedExpensesTotal(): number {
    return this.plannedHotelCost + this.plannedPerDiemCost + this.plannedMileageCost +
           this.plannedFlightCost + this.plannedRentalCarCost;
  }

  getActualExpensesTotal(): number {
    return this.actualHotelCost + this.actualPerDiemCost + this.actualMileageCost +
           this.actualFlightCost + this.actualRentalCarCost;
  }
} 