import React, { useState, useEffect } from 'react';
import { Box, Typography, Button } from '@mui/material';
import { ResultsWindow } from './ResultsWindow';
import { TrackingData } from '../models/TrackingData';
import { RateSheet } from '../models/RateSheet';
import { DataManager } from '../utils/DataManager';

interface TrackingViewProps {
  trackingData: TrackingData;
  loadedFilename?: string | null;
  onBackToWelcome: () => void;
  darkMode?: boolean;
}

export const TrackingView: React.FC<TrackingViewProps> = ({
  trackingData,
  loadedFilename,
  onBackToWelcome,
  darkMode = false
}) => {
  const [resultsWindowOpen, setResultsWindowOpen] = useState(true);
  const [rateSheets] = useState<RateSheet[]>(DataManager.loadRateSheets());

  // Extract project data from tracking data
  const projectData = {
    projectNumber: trackingData.projectData?.projectNumber || 'Unknown',
    customer: trackingData.projectData?.customer || 'Unknown',
    projectDescription: trackingData.projectData?.projectDescription || trackingData.projectName || 'Project',
    technician: trackingData.projectData?.technician || ''
  };

  // Create mock resource data for the ResultsWindow
  const mockResources = trackingData.resources.map(resource => ({
    id: resource.resourceId,
    name: resource.resourceName,
    daysOnSite: resource.days.length,
    hoursPerDay: 8, // Default value
    startDay: 'Monday',
    holdoverDayEnabled: false,
    holdoverDayOfWeek: 'Sunday',
    separateTravelTo: false,
    separateTravelFrom: false,
    travelMethod: 'Driving',
    travelDistance: 0,
    travelTime: 0,
    dailyTravelDistance: 15,
    dailyTravelTime: 0.25,
    discountPercent: 0,
    isEmergency: false,
    hotelRequired: true,
    rentalCarRequired: false,
    otherExpenses: 0,
    hotelCost: 0,
    rentalCarRate: 0,
    flightCost: 0,
    mileageRate: 0,
    perDiemRate: 0,
    includeSaturdays: true,
    includeSundays: true,
    technician: '',
    startDate: null,
    endDate: null,
    selectedSheet: rateSheets[0]?.name || '',
    manualOverrides: new Map(),
    otBefore7After5: false
  }));

  return (
    <Box sx={{ 
      background: darkMode ? '#23262b' : '#fff', 
      minHeight: '100vh', 
      color: darkMode ? '#fff' : '#000',
      p: 1
    }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
        <Typography variant="h6" sx={{ color: darkMode ? '#fff' : '#000' }}>
          Project Tracking - {trackingData.projectName}
        </Typography>
        <Button
          variant="outlined"
          size="small"
          onClick={onBackToWelcome}
          sx={{ 
            color: darkMode ? '#fff' : '#000', 
            borderColor: darkMode ? '#444' : '#ccc' 
          }}
        >
          Back to Welcome
        </Button>
      </Box>

      <ResultsWindow
        open={resultsWindowOpen}
        onClose={() => setResultsWindowOpen(false)}
        resources={mockResources}
        rateSheets={rateSheets}
        projectData={projectData}
        startDate={null}
        endDate={null}
        darkMode={darkMode}
        trackingData={trackingData}
        loadedFilename={loadedFilename}
      />
    </Box>
  );
}; 