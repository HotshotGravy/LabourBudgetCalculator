export interface DayValues {
  regularLabour: number;
  overtimeLabour: number;
  premiumLabour: number;
  regularTravel: number;
  overtimeTravel: number;
  premiumTravel: number;
  mileage: number;
  perDiem: number;
  flight: number;
  carRental: number;
  hotel: number;
}

export interface DayTrackingData {
  dayNumber: number;
  dayOfWeek: number; // 0=Sunday, 1=Monday, etc.
  date: string;
  planned: DayValues;
  actual: DayValues;
  delta: DayValues;
}

export interface ResourceTrackingData {
  resourceId: string;
  resourceName: string;
  days: DayTrackingData[];
  isMinimized: boolean;
}

export interface TrackingData {
  projectId: string;
  projectName: string;
  quotedAmount: number;
  resources: ResourceTrackingData[];
  lastUpdated: Date;
  projectData?: ProjectData;
}

export interface ProjectData {
  projectNumber: string;
  customer: string;
  projectDescription: string;
  technician: string;
}

// File type validation interfaces
export interface FileMetadata {
  fileType: 'tracker' | 'estimator';
  version: string;
  savedAt: string;
}

export interface TrackerFileData extends TrackingData, FileMetadata {
  fileType: 'tracker';
  projectData: ProjectData; // Make required for tracker files
}

export interface EstimatorFileData extends FileMetadata {
  fileType: 'estimator';
  id: string;
  name: string;
  timestamp: number;
  projectData: ProjectData;
  resources: any[]; // This will be the estimator's resource format
} 