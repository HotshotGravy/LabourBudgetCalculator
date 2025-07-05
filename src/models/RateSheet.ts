export interface RateSheet {
  name: string;
  regularLabourRate: number;
  overtimeLabourRate: number;
  premiumLabourRate: number;
  regularTravelRate: number;
  overtimeTravelRate: number;
  premiumTravelRate: number;
  
  // Expense rates associated with this sheet
  hotelCost: number;
  perDiemRate: number;
  mileageRate: number;
  rentalCarRate: number;
  flightCost: number; // Typical flight cost associated with this rate profile
}

export class RateSheetClass implements RateSheet {
  name: string = 'Unnamed Rate Sheet';
  regularLabourRate: number = 0;
  overtimeLabourRate: number = 0;
  premiumLabourRate: number = 0;
  regularTravelRate: number = 0;
  overtimeTravelRate: number = 0;
  premiumTravelRate: number = 0;
  hotelCost: number = 0;
  perDiemRate: number = 0;
  mileageRate: number = 0;
  rentalCarRate: number = 0;
  flightCost: number = 0;

  constructor(name?: string) {
    if (name) {
      this.name = name;
    }
  }
} 