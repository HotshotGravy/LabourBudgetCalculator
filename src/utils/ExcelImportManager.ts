import * as XLSX from 'xlsx';
import dayjs from 'dayjs';
import { TrackingData, ResourceTrackingData, DayTrackingData, DayValues } from '../models/TrackingData';

export interface ExcelRow {
  chargeId: string;
  date: string;
  chargeType: string;
  employee: string;
  item: string;
  description: string;
  quantity: number;
  rate: number;
  amount: number;
  chargeStage: string;
  invoiceNumber: string;
  invoiceDate: string;
}

export interface ImportResult {
  success: boolean;
  message: string;
  processedRows: number;
  skippedRows: number;
  errors: string[];
  validationWarnings: string[];
}

export interface ValidationWarning {
  row: number;
  message: string;
  expectedAmount: number;
  actualAmount: number;
  userChoice: 'useQuantity' | 'useAmount' | 'skip';
}

export class ExcelImportManager {
  /**
   * Parse Excel file and extract charge data
   */
  static parseExcelFile(file: File): Promise<ExcelRow[]> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e) => {
        try {
          const data = new Uint8Array(e.target?.result as ArrayBuffer);
          const workbook = XLSX.read(data, { type: 'array' });
          const sheetName = workbook.SheetNames[0];
          const worksheet = workbook.Sheets[sheetName];
          
          // Convert to JSON with headers
          const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1 }) as any[][];
          
          if (jsonData.length < 2) {
            reject(new Error('Excel file must have at least a header row and one data row'));
            return;
          }
          
          const headers = jsonData[0] as string[];
          const rows = jsonData.slice(1);
          
          // Map headers to expected column names
          const columnMap = this.mapHeaders(headers);
          console.log('Headers found:', headers);
          console.log('Column map:', columnMap);
          
          const parsedRows: ExcelRow[] = [];
          
          for (let i = 0; i < rows.length; i++) {
            const row = rows[i];
            if (!row || row.length === 0) continue;
            
            console.log(`Raw row ${i + 2}:`, row);
            console.log(`Row length: ${row.length}`);
            
            try {
              const parsedRow = this.parseRow(row, columnMap, i + 2); // +2 for header row and 1-based indexing
              if (parsedRow) {
                parsedRows.push(parsedRow);
                console.log(`Parsed row ${i + 2}:`, parsedRow);
              } else {
                console.log(`Skipped row ${i + 2} - missing essential data`);
              }
            } catch (error) {
              console.warn(`Error parsing row ${i + 2}:`, error);
            }
          }
          
          console.log(`Total parsed rows: ${parsedRows.length}`);
          
          resolve(parsedRows);
        } catch (error) {
          reject(error);
        }
      };
      
      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsArrayBuffer(file);
    });
  }
  
  /**
   * Map Excel headers to expected column names
   */
  private static mapHeaders(headers: string[]): { [key: string]: number } {
    const columnMap: { [key: string]: number } = {};
    
    headers.forEach((header, index) => {
      const normalizedHeader = header?.toString().toLowerCase().trim();
      
      console.log(`Header ${index}: "${header}" -> normalized: "${normalizedHeader}"`);
      
      if (normalizedHeader.includes('charge id') || normalizedHeader.includes('chargeid')) {
        columnMap.chargeId = index;
      } else if (normalizedHeader === 'date') {
        columnMap.date = index;
      } else if (normalizedHeader.includes('charge type') || normalizedHeader.includes('chargetype')) {
        columnMap.chargeType = index;
      } else if (normalizedHeader.includes('employee')) {
        columnMap.employee = index;
      } else if (normalizedHeader.includes('item')) {
        columnMap.item = index;
      } else if (normalizedHeader.includes('description')) {
        columnMap.description = index;
      } else if (normalizedHeader.includes('quantity')) {
        columnMap.quantity = index;
      } else if (normalizedHeader.includes('rate')) {
        columnMap.rate = index;
      } else if (normalizedHeader.includes('amount')) {
        columnMap.amount = index;
      } else if (normalizedHeader.includes('charge stage') || normalizedHeader.includes('chargestage')) {
        columnMap.chargeStage = index;
      } else if (normalizedHeader.includes('invoice number') || normalizedHeader.includes('invoicenumber')) {
        columnMap.invoiceNumber = index;
      } else if (normalizedHeader.includes('invoice date') || normalizedHeader.includes('invoicedate')) {
        columnMap.invoiceDate = index;
      }
    });
    
    return columnMap;
  }
  
  /**
   * Parse a single row of data
   */
  private static parseRow(row: any[], columnMap: { [key: string]: number }, rowNumber: number): ExcelRow | null {
    const getValue = (key: string): string => {
      const index = columnMap[key];
      let value = index !== undefined && row[index] !== undefined ? String(row[index]).trim() : '';
      
      // Convert Excel date serial numbers to YYYY-MM-DD format
      if (key === 'date' && value && !isNaN(Number(value)) && Number(value) > 1000) {
        // Excel dates are days since January 1, 1900
        const excelDate = Number(value);
        const date = new Date((excelDate - 25569) * 86400 * 1000); // Convert to milliseconds
        value = date.toISOString().split('T')[0]; // Get YYYY-MM-DD format
        console.log(`Converted Excel date ${excelDate} to ${value}`);
      }
      
      console.log(`getValue('${key}'): index=${index}, value="${value}"`);
      return value;
    };
    
    const getNumber = (key: string): number => {
      const value = getValue(key);
      const num = parseFloat(value.replace(/[^\d.-]/g, ''));
      return isNaN(num) ? 0 : num;
    };
    
    const chargeId = getValue('chargeId');
    const date = getValue('date');
    const chargeType = getValue('chargeType');
    const employee = getValue('employee');
    const item = getValue('item');
    
    console.log(`Row ${rowNumber} values:`, { chargeId, date, chargeType, employee, item });
    
    // Skip rows without essential data
    if (!date || !chargeType || !employee || !item) {
      console.log(`Row ${rowNumber} missing:`, { 
        date: !date, 
        chargeType: !chargeType, 
        employee: !employee, 
        item: !item 
      });
      return null;
    }
    
    return {
      chargeId,
      date,
      chargeType,
      employee,
      item,
      description: getValue('description'),
      quantity: getNumber('quantity'),
      rate: getNumber('rate'),
      amount: getNumber('amount'),
      chargeStage: getValue('chargeStage'),
      invoiceNumber: getValue('invoiceNumber'),
      invoiceDate: getValue('invoiceDate')
    };
  }
  
  /**
   * Find matching resource using fuzzy matching
   */
  static findMatchingResource(employeeName: string, resourceNames: string[]): { match: string | null; confidence: number } {
    const normalizedEmployee = employeeName.toLowerCase().trim();
    
    // Exact match
    const exactMatch = resourceNames.find(name => name.toLowerCase().trim() === normalizedEmployee);
    if (exactMatch) {
      return { match: exactMatch, confidence: 1.0 };
    }
    
    // Fuzzy matching using simple similarity
    let bestMatch: string | null = null;
    let bestConfidence = 0;
    
    for (const resourceName of resourceNames) {
      const normalizedResource = resourceName.toLowerCase().trim();
      
      // Check if one contains the other (e.g., "Jeffrey Bean" contains "Jeff Bean")
      if (normalizedEmployee.includes(normalizedResource) || normalizedResource.includes(normalizedEmployee)) {
        const confidence = Math.min(normalizedEmployee.length, normalizedResource.length) / 
                          Math.max(normalizedEmployee.length, normalizedResource.length);
        if (confidence > bestConfidence) {
          bestMatch = resourceName;
          bestConfidence = confidence;
        }
      }
      
      // Check for common name variations
      const employeeWords = normalizedEmployee.split(/\s+/);
      const resourceWords = normalizedResource.split(/\s+/);
      
      if (employeeWords.length === 2 && resourceWords.length === 2) {
        // Check for first name variations (e.g., "Jeff" vs "Jeffrey")
        const firstNameMatch = employeeWords[0] === resourceWords[0] || 
                              employeeWords[0].includes(resourceWords[0]) || 
                              resourceWords[0].includes(employeeWords[0]);
        const lastNameMatch = employeeWords[1] === resourceWords[1];
        
        if (firstNameMatch && lastNameMatch) {
          const confidence = 0.9; // High confidence for name variations
          if (confidence > bestConfidence) {
            bestMatch = resourceName;
            bestConfidence = confidence;
          }
        }
      }
    }
    
    return { match: bestMatch, confidence: bestConfidence };
  }
  
  /**
   * Validate quantity vs rate × amount
   */
  static validateQuantityRateAmount(quantity: number, rate: number, amount: number): { isValid: boolean; expectedAmount: number; actualAmount: number } {
    const expectedAmount = quantity * rate;
    const tolerance = 0.01; // Allow for small rounding differences
    const isValid = Math.abs(expectedAmount - amount) <= tolerance;
    
    return {
      isValid,
      expectedAmount,
      actualAmount: amount
    };
  }
  
  /**
   * Map Excel data to tracking data updates
   */
  static mapExcelDataToUpdates(
    excelRows: ExcelRow[], 
    trackingData: TrackingData,
    validationWarnings: ValidationWarning[]
  ): { [resourceId: string]: { [date: string]: Partial<DayValues> } } {
    const updates: { [resourceId: string]: { [date: string]: Partial<DayValues> } } = {};
    
    console.log('Mapping Excel data to updates...');
    console.log('Excel rows:', excelRows);
    
    // Get all resource names for fuzzy matching
    const resourceNames = trackingData.resources.map(r => r.resourceName);
    console.log('Available resource names:', resourceNames);
    
    for (const row of excelRows) {
      console.log(`Processing row for employee: ${row.employee}, date: ${row.date}`);
      
      // Find matching resource
      const { match: resourceName, confidence } = this.findMatchingResource(row.employee, resourceNames);
      console.log(`Resource match: ${resourceName} (confidence: ${confidence})`);
      
      if (!resourceName || confidence < 0.7) {
        console.log(`Skipping row - no good resource match found`);
        continue; // Skip if no good match found
      }
      
      // Find the resource in tracking data
      const resource = trackingData.resources.find(r => r.resourceName === resourceName);
      if (!resource) {
        console.log(`Resource not found in tracking data: ${resourceName}`);
        continue;
      }
      
      // Check if date exists in tracking data
      const dayData = resource.days.find(d => d.date === row.date);
      if (!dayData) {
        console.log(`Date not found in tracking data: ${row.date}`);
        continue;
      }
      
      console.log(`Found matching resource and date, processing updates...`);
      
      // Initialize updates for this resource and date if not exists
      if (!updates[resource.resourceId]) {
        updates[resource.resourceId] = {};
      }
      if (!updates[resource.resourceId][row.date]) {
        updates[resource.resourceId][row.date] = {};
      }
      
      // Map the data based on charge type and item
      this.mapRowToUpdates(row, updates[resource.resourceId][row.date], validationWarnings);
    }
    
    console.log('Final updates:', updates);
    return updates;
  }
  
  /**
   * Map a single Excel row to actual value updates
   */
  private static mapRowToUpdates(
    row: ExcelRow, 
    updates: Partial<DayValues>, 
    validationWarnings: ValidationWarning[]
  ): void {
    const { chargeType, item, description, quantity, rate, amount } = row;
    
    // Validate quantity vs rate × amount
    const validation = this.validateQuantityRateAmount(quantity, rate, amount);
    if (!validation.isValid) {
      validationWarnings.push({
        row: 0, // Will be set by caller
        message: `Quantity × Rate (${validation.expectedAmount.toFixed(2)}) ≠ Amount (${validation.actualAmount.toFixed(2)})`,
        expectedAmount: validation.expectedAmount,
        actualAmount: validation.actualAmount,
        userChoice: 'useQuantity' // Default choice
      });
    }
    
    if (chargeType.toLowerCase().includes('time-based')) {
      // Handle labor and travel hours
      if (item.toLowerCase().includes('[service]')) {
        if (item.toLowerCase().includes('regular')) {
          if (item.toLowerCase().includes('travel')) {
            updates.regularTravel = (updates.regularTravel || 0) + quantity;
          } else {
            updates.regularLabour = (updates.regularLabour || 0) + quantity;
          }
        } else if (item.toLowerCase().includes('overtime')) {
          if (item.toLowerCase().includes('travel')) {
            updates.overtimeTravel = (updates.overtimeTravel || 0) + quantity;
          } else {
            updates.overtimeLabour = (updates.overtimeLabour || 0) + quantity;
          }
        } else if (item.toLowerCase().includes('emergency') || item.toLowerCase().includes('premium')) {
          if (item.toLowerCase().includes('travel')) {
            updates.premiumTravel = (updates.premiumTravel || 0) + quantity;
          } else {
            updates.premiumLabour = (updates.premiumLabour || 0) + quantity;
          }
        }
      }
    } else if (chargeType.toLowerCase().includes('expense-based')) {
      // Handle expenses
      if (item.toLowerCase().includes('[expenses, cogs]')) {
        if (item.toLowerCase().includes('per diem')) {
          updates.perDiem = (updates.perDiem || 0) + quantity;
        } else if (item.toLowerCase().includes('mileage')) {
          updates.mileage = (updates.mileage || 0) + quantity;
        } else if (item.toLowerCase().includes('travel')) {
          // Use description to categorize travel expenses
          const desc = description.toLowerCase();
          if (desc.includes('gas')) {
            // Ignore gas expenses as requested
            return;
          } else if (desc.includes('rental car') || desc.includes('car rental')) {
            updates.carRental = (updates.carRental || 0) + amount;
          } else if (desc.includes('hotel')) {
            updates.hotel = (updates.hotel || 0) + amount;
          } else if (desc.includes('flight') || desc.includes('airfare')) {
            updates.flight = (updates.flight || 0) + amount;
          }
        }
      }
    }
  }
  
  /**
   * Apply updates to tracking data
   */
  static applyUpdatesToTrackingData(
    trackingData: TrackingData,
    updates: { [resourceId: string]: { [date: string]: Partial<DayValues> } }
  ): TrackingData {
    const updatedTrackingData = { ...trackingData };
    
    updatedTrackingData.resources = trackingData.resources.map(resource => {
      const resourceUpdates = updates[resource.resourceId];
      if (!resourceUpdates) return resource;
      
      const updatedResource = { ...resource };
      updatedResource.days = resource.days.map(day => {
        const dayUpdates = resourceUpdates[day.date];
        if (!dayUpdates) return day;
        
        const updatedDay = { ...day };
        updatedDay.actual = { ...day.actual };
        
        // Apply updates to actual values
        Object.entries(dayUpdates).forEach(([key, value]) => {
          if (value !== undefined) {
            (updatedDay.actual as any)[key] = value;
          }
        });
        
        // Recalculate deltas
        updatedDay.delta = this.calculateDelta(updatedDay.actual, day.planned);
        
        return updatedDay;
      });
      
      return updatedResource;
    });
    
    return updatedTrackingData;
  }
  
  /**
   * Calculate delta between actual and planned values
   */
  private static calculateDelta(actual: DayValues, planned: DayValues): DayValues {
    return {
      regularLabour: (actual.regularLabour || 0) - (planned.regularLabour || 0),
      overtimeLabour: (actual.overtimeLabour || 0) - (planned.overtimeLabour || 0),
      premiumLabour: (actual.premiumLabour || 0) - (planned.premiumLabour || 0),
      regularTravel: (actual.regularTravel || 0) - (planned.regularTravel || 0),
      overtimeTravel: (actual.overtimeTravel || 0) - (planned.overtimeTravel || 0),
      premiumTravel: (actual.premiumTravel || 0) - (planned.premiumTravel || 0),
      mileage: (actual.mileage || 0) - (planned.mileage || 0),
      perDiem: (actual.perDiem || 0) - (planned.perDiem || 0),
      flight: (actual.flight || 0) - (planned.flight || 0),
      carRental: (actual.carRental || 0) - (planned.carRental || 0),
      hotel: (actual.hotel || 0) - (planned.hotel || 0)
    };
  }
} 