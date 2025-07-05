import { RateSheet, RateSheetClass } from '../models/RateSheet';

export class DataManager {
  private static readonly STORAGE_KEY = 'labour-budget-calculator';
  private static readonly RATE_SHEETS_KEY = 'rate-sheets';

  static saveRateSheets(rateSheets: RateSheet[]): void {
    try {
      const data = JSON.stringify(rateSheets);
      localStorage.setItem(`${this.STORAGE_KEY}-${this.RATE_SHEETS_KEY}`, data);
    } catch (error) {
      console.error('Error saving rate sheets:', error);
      alert(`Error saving rate sheets: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  static loadRateSheets(): RateSheet[] {
    let rateSheets: RateSheet[] = [];

    try {
      const data = localStorage.getItem(`${this.STORAGE_KEY}-${this.RATE_SHEETS_KEY}`);
      if (data) {
        const parsed = JSON.parse(data);
        rateSheets = parsed.map((sheet: any) => new RateSheetClass(sheet.name));
        Object.assign(rateSheets, parsed);
      }
    } catch (error) {
      console.error('Error loading rate sheets:', error);
      alert(`Error loading rate sheets: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }

    // If no rate sheets or error loading, create a default one
    if (rateSheets.length === 0) {
      rateSheets.push(this.createDefaultRateSheet());
    }

    return rateSheets;
  }

  private static createDefaultRateSheet(): RateSheet {
    const defaultSheet = new RateSheetClass('Automation 2025');
    defaultSheet.regularLabourRate = 155;
    defaultSheet.overtimeLabourRate = 232.5;
    defaultSheet.premiumLabourRate = 310;
    defaultSheet.regularTravelRate = 124;
    defaultSheet.overtimeTravelRate = 186;
    defaultSheet.premiumTravelRate = 248;
    defaultSheet.hotelCost = 120;
    defaultSheet.perDiemRate = 80;
    defaultSheet.mileageRate = 0.70;
    defaultSheet.rentalCarRate = 120;
    defaultSheet.flightCost = 400;
    return defaultSheet;
  }

  // Generic save/load methods for other data
  static saveData<T>(key: string, data: T): void {
    try {
      const jsonData = JSON.stringify(data);
      localStorage.setItem(`${this.STORAGE_KEY}-${key}`, jsonData);
    } catch (error) {
      console.error(`Error saving data for key ${key}:`, error);
    }
  }

  static loadData<T>(key: string, defaultValue: T): T {
    try {
      const data = localStorage.getItem(`${this.STORAGE_KEY}-${key}`);
      if (data) {
        return JSON.parse(data);
      }
    } catch (error) {
      console.error(`Error loading data for key ${key}:`, error);
    }
    return defaultValue;
  }

  static clearData(key?: string): void {
    if (key) {
      localStorage.removeItem(`${this.STORAGE_KEY}-${key}`);
    } else {
      // Clear all app data
      const keys = Object.keys(localStorage);
      keys.forEach(k => {
        if (k.startsWith(this.STORAGE_KEY)) {
          localStorage.removeItem(k);
        }
      });
    }
  }
} 