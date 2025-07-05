export enum CalculationDayType {
  WorkDay = 'WorkDay',
  TravelTo = 'TravelTo',
  TravelFrom = 'TravelFrom',
  None = 'None'
}

export interface DayDetail {
  dayNumber: number;
  dayOfWeek: number; // 0 = Sunday, 1 = Monday, etc.
  type: CalculationDayType;
  regularLabourHours: number;
  overtimeLabourHours: number;
  premiumLabourHours: number;
  totalLabourHours: number;
  regularTravelHours: number;
  overtimeTravelHours: number;
  premiumTravelHours: number;
  totalTravelHours: number;
  labourCost: number;
  travelCost: number;
  hotelCost: number;
  perDiem: number;
  mileageCost: number;
  rentalCarCost: number;
  airfareCost: number;
  totalDayCost: number;
}

export interface CalculationResult {
  totalLabourCost: number;
  totalTravelCost: number;
  totalExpenses: number;
  grandTotal: number;
  totalDays: number;
  totalLabourHours: number;
  totalTravelHours: number;
  
  // Daily breakdown
  dayDetails: DayDetail[];
} 