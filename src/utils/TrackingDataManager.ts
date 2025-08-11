import { TrackingData, ResourceTrackingData, DayTrackingData, DayValues, TrackerFileData, EstimatorFileData } from '../models/TrackingData';
import { ResourceData } from '../components/QuickEstimator';
import { RateSheet } from '../models/RateSheet';
import { calculateEstimate } from './estimatorEngine';
import dayjs from 'dayjs';

export class TrackingDataManager {
  // Generate a unique identifier for a project based on its data
  static generateProjectId(projectData: { projectNumber: string; customer: string; projectDescription: string }): string {
    const projectNumber = projectData.projectNumber.trim() || 'Unknown';
    const customer = projectData.customer.trim() || 'Unknown';
    const description = projectData.projectDescription.trim() || 'Support';
    return `${projectNumber}-${customer}-${description}`.replace(/[^a-zA-Z0-9-]/g, '-');
  }

  // Save tracking data to a .trk file
  static saveTrackingData(trackingData: TrackingData, projectData: { projectNumber: string; customer: string; projectDescription: string; technician: string }, filename?: string): Promise<string | null> {
    return new Promise((resolve, reject) => {
      const projectId = this.generateProjectId(projectData);
      const today = dayjs().format('YYYY-MM-DD');
      const defaultFilename = filename || `${projectId}-Tracking-${today}.trk`;
      
      // Create the data to save with file type validation
      const saveData: TrackerFileData = {
        ...trackingData,
        fileType: 'tracker',
        version: '1.0',
        projectData, // Include the project data for reference
        savedAt: new Date().toISOString()
      };
      
      const blob = new Blob([JSON.stringify(saveData, null, 2)], { type: 'application/json' });
      
      // Create a temporary link to trigger the save dialog
      const link = document.createElement('a');
      const url = URL.createObjectURL(blob);
      link.href = url;
      link.download = defaultFilename;
      link.style.display = 'none';
      
      // Add to DOM and trigger download
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      // Store the save location for future reference
      const saveInfo = {
        projectId,
        filename: defaultFilename,
        timestamp: Date.now()
      };
      
      // Store in localStorage for future reference
      const savedFiles = JSON.parse(localStorage.getItem('savedTrackingFiles') || '[]');
      savedFiles.push(saveInfo);
      localStorage.setItem('savedTrackingFiles', JSON.stringify(savedFiles));
      
      resolve(defaultFilename);
    });
  }

  // Check if a filename already exists
  static checkFilenameExists(filename: string): boolean {
    const savedFiles = JSON.parse(localStorage.getItem('savedTrackingFiles') || '[]');
    return savedFiles.some((file: any) => file.filename === filename);
  }

  // Load tracking data from a .trk file with validation
  static loadTrackingData(file: File): Promise<TrackingData> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e) => {
        try {
          const content = e.target?.result as string;
          const data = JSON.parse(content);
          
          // Validate file type
          if (!data.fileType || data.fileType !== 'tracker') {
            throw new Error('This file is not a tracker file. Please select a .trk file.');
          }
          
          // Validate version
          if (!data.version) {
            throw new Error('Invalid file format: missing version information');
          }
          
          // Validate the data structure
          if (!data.projectId || !data.projectName || !data.resources) {
            throw new Error('Invalid tracking data file format');
          }
          
          // Convert the loaded data back to proper TrackingData format
          const trackingData: TrackingData = {
            projectId: data.projectId,
            projectName: data.projectName,
            quotedAmount: data.quotedAmount || 0,
            resources: data.resources,
            lastUpdated: new Date(data.lastUpdated || Date.now()),
            projectData: data.projectData
          };
          
          resolve(trackingData);
        } catch (error) {
          reject(new Error(`Failed to parse tracking data file: ${(error as Error).message}`));
        }
      };
      
      reader.onerror = () => {
        reject(new Error('Failed to read file'));
      };
      
      reader.readAsText(file);
    });
  }

  // Validate if a file is a tracker file without loading it
  static validateTrackerFile(file: File): Promise<boolean> {
    return new Promise((resolve) => {
      const reader = new FileReader();
      
      reader.onload = (e) => {
        try {
          const content = e.target?.result as string;
          const data = JSON.parse(content);
          
          // Check if it's a tracker file
          if (data.fileType === 'tracker' && data.version) {
            resolve(true);
          } else {
            resolve(false);
          }
        } catch (error) {
          resolve(false);
        }
      };
      
      reader.onerror = () => {
        resolve(false);
      };
      
      reader.readAsText(file);
    });
  }

  // Check if a tracking file exists for a project
  static async checkTrackingFileExists(projectData: { projectNumber: string; customer: string; projectDescription: string }): Promise<boolean> {
    const projectId = this.generateProjectId(projectData);
    const savedFiles = JSON.parse(localStorage.getItem('savedTrackingFiles') || '[]');
    
    // Check if we have a saved file for this project
    return savedFiles.some((file: any) => file.projectId === projectId);
  }

  static saveToCSV(trackingData: TrackingData): void {
    const csvContent = this.generateCSVContent(trackingData);
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', `Tracking_Data_${trackingData.projectName}_${dayjs().format('YYYY-MM-DD')}.csv`);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }

  private static generateCSVContent(trackingData: TrackingData): string {
    const headers = [
      'Project Name',
      'Resource',
      'Day',
      'Date',
      'Category',
      'Subcategory',
      'Planned',
      'Actual',
      'Delta'
    ];

    const rows: string[] = [headers.join(',')];

    trackingData.resources.forEach(resource => {
      resource.days.forEach(day => {
        // Labour Hours
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Labour Hours,Regular Labour,${day.planned.regularLabour},${day.actual.regularLabour},${day.delta.regularLabour}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Labour Hours,Overtime Labour,${day.planned.overtimeLabour},${day.actual.overtimeLabour},${day.delta.overtimeLabour}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Labour Hours,Premium Labour,${day.planned.premiumLabour},${day.actual.premiumLabour},${day.delta.premiumLabour}`);
        
        // Travel Hours
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Travel Hours,Regular Travel,${day.planned.regularTravel},${day.actual.regularTravel},${day.delta.regularTravel}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Travel Hours,Overtime Travel,${day.planned.overtimeTravel},${day.actual.overtimeTravel},${day.delta.overtimeTravel}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Travel Hours,Premium Travel,${day.planned.premiumTravel},${day.actual.premiumTravel},${day.delta.premiumTravel}`);
        
        // Expenses
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Expenses,Mileage,${day.planned.mileage},${day.actual.mileage},${day.delta.mileage}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Expenses,Per Diem,${day.planned.perDiem},${day.actual.perDiem},${day.delta.perDiem}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Expenses,Flight,${day.planned.flight},${day.actual.flight},${day.delta.flight}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Expenses,Car Rental,${day.planned.carRental},${day.actual.carRental},${day.delta.carRental}`);
        rows.push(`${trackingData.projectName},${resource.resourceName},${day.dayNumber},${day.date},Expenses,Hotel,${day.planned.hotel},${day.actual.hotel},${day.delta.hotel}`);
      });
    });

    return rows.join('\n');
  }

  static initializeTrackingData(
    resources: ResourceData[],
    rateSheets: RateSheet[],
    projectData: { projectNumber: string; customer: string; projectDescription: string; technician: string },
    startDate: dayjs.Dayjs | null,
    endDate: dayjs.Dayjs | null,
    projectRateSheetName?: string
  ): TrackingData {
    const projectName = projectData.projectDescription.trim() || 'Support';
    const projectId = Date.now().toString();

    const resourceTrackingData: ResourceTrackingData[] = resources.map(resource => {
      // Use the project rate sheet if provided, otherwise fall back to first rate sheet
      const rateSheet = projectRateSheetName 
        ? rateSheets.find(s => s.name === projectRateSheetName) || rateSheets[0]
        : rateSheets[0];
      const customSheet = {
        ...rateSheet,
        hotelCost: resource.hotelCost,
        rentalCarRate: resource.rentalCarRate,
        flightCost: resource.flightCost,
        mileageRate: resource.mileageRate,
        perDiemRate: resource.perDiemRate
      };

      const calcResult = calculateEstimate({
        daysOnSite: resource.daysOnSite,
        hoursPerDay: resource.hoursPerDay,
        startDayOfWeek: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].indexOf(resource.startDay),
        holdoverDayEnabled: resource.holdoverDayEnabled,
        holdoverDayOfWeek: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].indexOf(resource.holdoverDayOfWeek),
        separateTravelTo: resource.separateTravelTo,
        separateTravelFrom: resource.separateTravelFrom,
        travelMethod: resource.travelMethod,
        travelDistance: resource.travelDistance,
        travelTime: resource.travelTime,
        dailyTravelDistance: resource.dailyTravelDistance,
        dailyTravelTime: resource.dailyTravelTime,
        rateSheet: customSheet,
        discountPercent: resource.discountPercent,
        isEmergency: resource.isEmergency,
        hotelRequired: resource.hotelRequired,
        rentalCarRequired: resource.rentalCarRequired,
        otherExpenses: resource.otherExpenses,
        manualOverrides: resource.manualOverrides,
        includeSaturdays: resource.includeSaturdays,
        includeSundays: resource.includeSundays,
        otBefore7After5: resource.otBefore7After5
      });

      // Debug: Log the resource start day and day details
      console.log(`Resource ${resource.name} (${resource.id}): startDay=${resource.startDay}, days:`, 
        calcResult.dayDetails.map(day => ({
          dayNumber: day.dayNumber,
          dayOfWeek: day.dayOfWeek,
          type: day.type
        }))
      );

      const days: DayTrackingData[] = calcResult.dayDetails.map((day, dayIndex) => {
        const plannedValues: DayValues = {
          regularLabour: day.regularLabourHours || 0,
          overtimeLabour: day.overtimeLabourHours || 0,
          premiumLabour: day.premiumLabourHours || 0,
          regularTravel: day.regularTravelHours || 0,
          overtimeTravel: day.overtimeTravelHours || 0,
          premiumTravel: day.premiumTravelHours || 0,
          mileage: (day.mileageCost || 0) * 1.1,
          perDiem: day.perDiem || 0,
          flight: (day.airfareCost || 0) * 1.1,
          carRental: (day.rentalCarCost || 0) * 1.1,
          hotel: (day.hotelCost || 0) * 1.1
        };

        // Initially, actual = planned
        const actualValues: DayValues = { ...plannedValues };

        // Calculate deltas (initially zero since actual = planned)
        const deltaValues: DayValues = this.calculateDelta(actualValues, plannedValues);

        return {
          dayNumber: day.dayNumber, // Use the original day number from estimator
          dayOfWeek: day.dayOfWeek, // Use the day of week calculated by estimator
          date: startDate ? startDate.add(day.dayNumber - 1, 'day').format('YYYY-MM-DD') : '',
          type: day.type, // Include the day type
          isHoldover: day.isHoldover, // Include holdover information
          planned: plannedValues,
          actual: actualValues,
          delta: deltaValues
        };
      });

      return {
        resourceId: resource.id,
        resourceName: resource.name,
        days,
        isMinimized: false
      };
    });

    return {
      projectId,
      projectName,
      quotedAmount: 0,
      resources: resourceTrackingData,
      lastUpdated: new Date(),
      projectData
    };
  }

  static calculateDelta(actual: DayValues, planned: DayValues): DayValues {
    return {
      regularLabour: (actual.regularLabour ?? 0) - (planned.regularLabour ?? 0),
      overtimeLabour: (actual.overtimeLabour ?? 0) - (planned.overtimeLabour ?? 0),
      premiumLabour: (actual.premiumLabour ?? 0) - (planned.premiumLabour ?? 0),
      regularTravel: (actual.regularTravel ?? 0) - (planned.regularTravel ?? 0),
      overtimeTravel: (actual.overtimeTravel ?? 0) - (planned.overtimeTravel ?? 0),
      premiumTravel: (actual.premiumTravel ?? 0) - (planned.premiumTravel ?? 0),
      mileage: (actual.mileage ?? 0) - (planned.mileage ?? 0),
      perDiem: (actual.perDiem ?? 0) - (planned.perDiem ?? 0),
      flight: (actual.flight ?? 0) - (planned.flight ?? 0),
      carRental: (actual.carRental ?? 0) - (planned.carRental ?? 0),
      hotel: (actual.hotel ?? 0) - (planned.hotel ?? 0)
    };
  }

  static calculateSummaryMetrics(trackingData: TrackingData, rateSheet: any): {
    planned: number;
    forecast: number;
    versusPlanned: number;
    versusQuoted: number;
  } {
    let planned = 0;
    let forecast = 0;
    let versusPlanned = 0;

    trackingData.resources.forEach(resource => {
      resource.days.forEach(day => {
        const rs = rateSheet || {};
        // Labour and travel charges
        const plannedLabour =
          (day.planned.regularLabour || 0) * (rs.regularLabourRate || 0) +
          (day.planned.overtimeLabour || 0) * (rs.overtimeLabourRate || 0) +
          (day.planned.premiumLabour || 0) * (rs.premiumLabourRate || 0);
        const plannedTravel =
          (day.planned.regularTravel || 0) * (rs.regularTravelRate || 0) +
          (day.planned.overtimeTravel || 0) * (rs.overtimeTravelRate || 0) +
          (day.planned.premiumTravel || 0) * (rs.premiumTravelRate || 0);
        // Calculate expenses (same as QuickEstimator's Grand Total calculation)
        // The planned values in tracking data already include the 10% markup from initializeTrackingData
        const plannedExpenses = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + 
                               (day.planned.flight || 0) + (day.planned.carRental || 0) + 
                               (day.planned.hotel || 0);
        const plannedDayTotal = plannedLabour + plannedTravel + plannedExpenses;

        const actualLabour =
          (day.actual.regularLabour || 0) * (rs.regularLabourRate || 0) +
          (day.actual.overtimeLabour || 0) * (rs.overtimeLabourRate || 0) +
          (day.actual.premiumLabour || 0) * (rs.premiumLabourRate || 0);
        const actualTravel =
          (day.actual.regularTravel || 0) * (rs.regularTravelRate || 0) +
          (day.actual.overtimeTravel || 0) * (rs.overtimeTravelRate || 0) +
          (day.actual.premiumTravel || 0) * (rs.premiumTravelRate || 0);
        
        // Calculate actual expenses (same as QuickEstimator's Grand Total calculation)
        const actualExpenses = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + 
                              (day.actual.flight || 0) + (day.actual.carRental || 0) + 
                              (day.actual.hotel || 0);
        const actualDayTotal = actualLabour + actualTravel + actualExpenses;

        planned += plannedDayTotal;
        forecast += actualDayTotal;
        versusPlanned += (actualDayTotal - plannedDayTotal);
      });
    });

    return {
      planned,
      forecast,
      versusPlanned,
      versusQuoted: forecast - trackingData.quotedAmount
    };
  }
} 