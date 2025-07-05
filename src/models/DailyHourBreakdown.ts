export interface DailyHourBreakdown {
  regularLabourHours: number;
  overtimeLabourHours: number;
  premiumLabourHours: number;
  regularTravelHours: number;
  overtimeTravelHours: number;
  premiumTravelHours: number;
}

export class DailyHourBreakdownClass implements DailyHourBreakdown {
  regularLabourHours: number = 0;
  overtimeLabourHours: number = 0;
  premiumLabourHours: number = 0;
  regularTravelHours: number = 0;
  overtimeTravelHours: number = 0;
  premiumTravelHours: number = 0;

  constructor(data?: Partial<DailyHourBreakdown>) {
    if (data) {
      Object.assign(this, data);
    }
  }

  // Method to get total planned hours
  getTotalPlannedHours(): number {
    return this.regularLabourHours + this.overtimeLabourHours + this.premiumLabourHours +
           this.regularTravelHours + this.overtimeTravelHours + this.premiumTravelHours;
  }
} 