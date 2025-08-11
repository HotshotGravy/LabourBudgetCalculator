import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Box, Grid, Typography, TextField, Select, MenuItem, Checkbox, FormControlLabel, Button, InputAdornment, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, Tabs, Tab } from '@mui/material';
import { DataManager } from '../utils/DataManager';
import { RateSheet, RateSheetClass } from '../models/RateSheet';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import SaveIcon from '@mui/icons-material/Save';
import FileCopyIcon from '@mui/icons-material/FileCopy';
import FolderIcon from '@mui/icons-material/Folder';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import { calculateEstimate } from '../utils/estimatorEngine';
import { DayEditorDialog } from './DayEditorDialog';
import { DayType } from '../models/ResourceDayData';
import { CalculationDayType, DayDetail } from '../models/CalculationResult';
import { ResultsWindow } from './ResultsWindow';
import dayjs, { Dayjs } from 'dayjs';
import { DatePicker, LocalizationProvider, TimePicker } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';
import { TrackingDataManager } from '../utils/TrackingDataManager';

const daysOfWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

interface QuickEstimatorProps {
  darkMode?: boolean;
  onBackToWelcome?: () => void;
}

interface ManualDayOverride {
  dayType: DayType;
  labourHours: number;
  travelHours: number;
  includeExpenses?: boolean;
  startTime?: string;
}

export interface ResourceData {
  id: string;
  name: string;
  daysOnSite: number;
  hoursPerDay: number;
  startDay: string;
  holdoverDayEnabled: boolean;
  holdoverDayOfWeek: string;
  separateTravelTo: boolean;
  separateTravelFrom: boolean;
  travelMethod: string;
  travelDistance: number;
  travelTime: number;
  dailyTravelDistance: number;
  dailyTravelTime: number;
  discountPercent: number;
  isEmergency: boolean;
  hotelRequired: boolean;
  rentalCarRequired: boolean;
  otherExpenses: number;
  hotelCost: number;
  rentalCarRate: number;
  flightCost: number;
  mileageRate: number;
  perDiemRate: number;
  includeSaturdays: boolean;
  includeSundays: boolean;
  technician: string;
  startDate: string | null;
  endDate: string | null;
  manualOverrides: Map<number, ManualDayOverride>;
  otBefore7After5: boolean;
}

interface ProjectData {
  projectNumber: string;
  customer: string;
  projectDescription: string;
  technician: string;
}



const QuickEstimator: React.FC<QuickEstimatorProps> = ({ darkMode, onBackToWelcome }) => {
  // Calculate schedule box size based on available height (responsive)
  const scheduleBoxSize = 99.225;

  // Colors for dark mode
  const panelBg = darkMode ? '#2c2f36' : '#fff';
  const panelBorder = darkMode ? '#444' : '#ccc';
  const panelText = darkMode ? '#fff' : '#000';
  const scheduleActive = darkMode ? '#1b3f97' : '#1976d2';
  const scheduleInactive = darkMode ? '#222' : '#222';
  const scheduleActiveText = '#fff';
  const scheduleInactiveText = darkMode ? '#aaa' : '#aaa';

  // Rate sheet state
  const [rateSheets, setRateSheets] = useState<RateSheet[]>(DataManager.loadRateSheets());
  const [selectedSheet, setSelectedSheet] = useState<string>(rateSheets[0]?.name || '');
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editingSheet, setEditingSheet] = useState<RateSheet | null>(null);
  const [isNew, setIsNew] = useState(false);

  // Helper: get selected rate sheet
  const currentSheet = rateSheets.find(s => s.name === selectedSheet) || rateSheets[0];

  // Open editor for new or existing
  const handleEdit = (sheet?: RateSheet) => {
    setEditingSheet(sheet ? { ...sheet } : new RateSheetClass(''));
    setIsNew(!sheet);
    setEditDialogOpen(true);
  };

  // Save changes
  const handleSave = () => {
    if (!editingSheet) return;
    let newSheets = [...rateSheets];
    if (isNew) {
      newSheets.push(editingSheet);
    } else {
      newSheets = newSheets.map(s => s.name === editingSheet.name ? editingSheet : s);
    }
    setRateSheets(newSheets);
    DataManager.saveRateSheets(newSheets);
    setEditDialogOpen(false);
    setEditingSheet(null);
    setIsNew(false);
    setSelectedSheet(editingSheet.name);
  };

  // Delete
  const handleDelete = (name: string) => {
    if (rateSheets.length <= 1) return;
    const newSheets = rateSheets.filter(s => s.name !== name);
    setRateSheets(newSheets);
    DataManager.saveRateSheets(newSheets);
    if (selectedSheet === name) setSelectedSheet(newSheets[0].name);
  };

  // Change field in editor
  const handleEditField = (field: keyof RateSheet, value: any) => {
    if (!editingSheet) return;
    let updated = { ...editingSheet, [field]: value };
    if (field === 'regularLabourRate') {
      updated.overtimeLabourRate = Math.max(value * 1.5, updated.overtimeLabourRate || 0);
      updated.premiumLabourRate = Math.max(value * 2, updated.premiumLabourRate || 0);
    }
    if (field === 'regularTravelRate') {
      updated.overtimeTravelRate = Math.max(value * 1.5, updated.overtimeTravelRate || 0);
      updated.premiumTravelRate = Math.max(value * 2, updated.premiumTravelRate || 0);
    }
    // Validation: Overtime and premium must always be greater than regular
    if (updated.overtimeLabourRate <= updated.regularLabourRate) {
      updated.overtimeLabourRate = updated.regularLabourRate * 1.5;
      console.warn('Overtime labour rate must be greater than regular. Auto-corrected.');
    }
    if (updated.premiumLabourRate <= updated.overtimeLabourRate) {
      updated.premiumLabourRate = updated.overtimeLabourRate * 1.33;
      console.warn('Premium labour rate must be greater than overtime. Auto-corrected.');
    }
    if (updated.overtimeTravelRate <= updated.regularTravelRate) {
      updated.overtimeTravelRate = updated.regularTravelRate * 1.5;
      console.warn('Overtime travel rate must be greater than regular. Auto-corrected.');
    }
    if (updated.premiumTravelRate <= updated.overtimeTravelRate) {
      updated.premiumTravelRate = updated.overtimeTravelRate * 1.33;
      console.warn('Premium travel rate must be greater than overtime. Auto-corrected.');
    }
    setEditingSheet(updated);
  };

  // Add state for start time on site (default 8:00 am)
  const [startTimeOnSite, setStartTimeOnSite] = useState<Dayjs | null>(dayjs().hour(8).minute(0));

  // Helper function to get discounted rate
  const getDiscountedRate = (base: number, premium: number) => {
    let rate = currentResource.isEmergency ? premium : base;
    let discounted = rate * (1 - currentResource.discountPercent / 100);
    return currentResource.discountPercent > 0 ? `${rate.toFixed(2)} → ${discounted.toFixed(2)}` : rate.toFixed(2);
  };

  // Define colors for different day types
  const travelDayBg = darkMode ? '#afa436' : '#ffe066';
  const holdoverDayBg = darkMode ? '#2e7d32' : '#4caf50';

  // State for Reset All confirmation dialog
  const [resetAllDialogOpen, setResetAllDialogOpen] = useState(false);
  
  // State for Results window
  const [resultsWindowOpen, setResultsWindowOpen] = useState(false);
  const [trackingData, setTrackingData] = useState<any>(null);
  const [fileInputRef] = useState(() => React.createRef<HTMLInputElement>());

  // Save/Clone functionality state
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [cloneDialogOpen, setCloneDialogOpen] = useState(false);
  const [savedEstimatesDialogOpen, setSavedEstimatesDialogOpen] = useState(false);
  const [estimateName, setEstimateName] = useState('');
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null);

  // Handler to reset all fields to default values
  const handleResetAll = () => {
    setSelectedSheet(rateSheets[0]?.name || '');
    setProjectNumber('');
    setCustomer('');
    setProjectDescription('');
    setTechnician('');
    
    // Reset current resource fields
    updateCurrentResourceField('daysOnSite', 1);
    updateCurrentResourceField('hoursPerDay', 8);
    updateCurrentResourceField('startDay', 'Monday');
    updateCurrentResourceField('holdoverDayEnabled', false);
    updateCurrentResourceField('holdoverDayOfWeek', 'Sunday');
    updateCurrentResourceField('separateTravelTo', false);
    updateCurrentResourceField('separateTravelFrom', false);
    updateCurrentResourceField('travelMethod', 'Driving');
    updateCurrentResourceField('travelDistance', 0);
    updateCurrentResourceField('travelTime', 0);
    updateCurrentResourceField('dailyTravelDistance', 15);
    updateCurrentResourceField('dailyTravelTime', 0.25);
    updateCurrentResourceField('discountPercent', 0);
    updateCurrentResourceField('isEmergency', false);
    updateCurrentResourceField('hotelRequired', true);
    updateCurrentResourceField('rentalCarRequired', false);
    updateCurrentResourceField('otherExpenses', 0);
    updateCurrentResourceField('hotelCost', rateSheets[0]?.hotelCost ?? 0);
    updateCurrentResourceField('rentalCarRate', rateSheets[0]?.rentalCarRate ?? 0);
    updateCurrentResourceField('flightCost', rateSheets[0]?.flightCost ?? 0);
    updateCurrentResourceField('mileageRate', rateSheets[0]?.mileageRate ?? 0);
    updateCurrentResourceField('perDiemRate', rateSheets[0]?.perDiemRate ?? 0);
    updateCurrentResourceField('includeSaturdays', true);
    updateCurrentResourceField('includeSundays', true);
    updateCurrentResourceField('otBefore7After5', false);
    updateCurrentResourceField('startDate', null);
    updateCurrentResourceField('endDate', null);
    // Rate sheet is project-wide, not per-resource
    updateCurrentResourceField('manualOverrides', new Map());
    
    setResetAllDialogOpen(false);
  };

  // Helper: round to nearest increment
  function roundToNearest(value: number, increment: number) {
    return Math.round(value / increment) * increment;
  }

  // Helper: generate default estimate name
  const generateDefaultEstimateName = () => {
    const customerText = customer.trim() || 'Unknown Customer';
    const projectText = projectDescription.trim() || 'Support';
    return `${customerText} ${projectText}`;
  };

  // Helper: save current estimate state to file
  const saveCurrentEstimate = async (name: string, isClone: boolean = false): Promise<boolean> => {
    try {
      const projectData: ProjectData = {
        projectNumber,
        customer,
        projectDescription,
        technician
      };

      const estimateData = {
        fileType: 'estimator',
        version: '1.0',
        id: Date.now().toString(),
        name,
        timestamp: Date.now(),
        projectData,
        resources: resources.map(resource => ({
          ...resource,
          manualOverrides: Object.fromEntries(resource.manualOverrides)
        })),
        savedAt: new Date().toISOString()
      };

      // Create filename with .est extension
      const customerText = customer.trim() || 'Unknown';
      const projectText = projectDescription.trim() || 'Project';
      const today = dayjs().format('YYYY-MM-DD');
      const filename = `${customerText} ${projectText} ${name}-${today}.est`;

      // Create and download file
      const blob = new Blob([JSON.stringify(estimateData, null, 2)], { type: 'application/json' });
      const link = document.createElement('a');
      const url = URL.createObjectURL(blob);
      link.href = url;
      link.download = filename;
      link.style.display = 'none';
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      setSaveSuccess(`Estimate "${name}" saved successfully as ${filename}`);
      setTimeout(() => setSaveSuccess(null), 3000);
      
      return true;
    } catch (error) {
      const errorMessage = `Failed to save estimate: ${(error as Error).message}`;
      setSaveError(errorMessage);
      setTimeout(() => setSaveError(null), 5000);
      return false;
    }
  };

  // Helper: load estimate data from file
  const loadEstimateData = (estimate: any) => {
    try {
      // Validate file type
      if (!estimate.fileType || estimate.fileType !== 'estimator') {
        throw new Error('This file is not an estimator file. Please select a .est file.');
      }
      
      // Validate version
      if (!estimate.version) {
        throw new Error('Invalid file format: missing version information');
      }
      
      setProjectNumber(estimate.projectData.projectNumber);
      setCustomer(estimate.projectData.customer);
      setProjectDescription(estimate.projectData.projectDescription);
      
      setResources(estimate.resources.map((resource: any) => ({
        ...resource,
        manualOverrides: new Map(Object.entries(resource.manualOverrides || {}))
      })));
      setCurrentResourceIndex(0);
      
      setSaveSuccess(`Estimate "${estimate.name}" loaded successfully`);
      setTimeout(() => setSaveSuccess(null), 3000);
    } catch (error) {
      const errorMessage = `Failed to load estimate: ${(error as Error).message}`;
      setSaveError(errorMessage);
      setTimeout(() => setSaveError(null), 5000);
    }
  };

  // Add state for project info fields
  const [projectNumber, setProjectNumber] = useState('');
  const [customer, setCustomer] = useState('');
  const [projectDescription, setProjectDescription] = useState('');
  const [technician, setTechnician] = useState('');

  // Multiple resource state
  const [resources, setResources] = useState<ResourceData[]>(() => [
    {
      id: '1',
      name: 'Technician 1',
      daysOnSite: 1,
      hoursPerDay: 8,
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
      hotelCost: rateSheets[0]?.hotelCost ?? 0,
      rentalCarRate: rateSheets[0]?.rentalCarRate ?? 0,
      flightCost: rateSheets[0]?.flightCost ?? 0,
      mileageRate: rateSheets[0]?.mileageRate ?? 0,
      perDiemRate: rateSheets[0]?.perDiemRate ?? 0,
      includeSaturdays: true,
      includeSundays: true,
      technician: '',
      startDate: null,
      endDate: null,
      manualOverrides: new Map(),
      otBefore7After5: false,
    }
  ]);
  
  const [currentResourceIndex, setCurrentResourceIndex] = useState(0);
  const [addResourceDialogOpen, setAddResourceDialogOpen] = useState(false);
  const [newResourceName, setNewResourceName] = useState('');
  const [copyFromResourceIndex, setCopyFromResourceIndex] = useState(0);
  const [copyOnlyRateSheet, setCopyOnlyRateSheet] = useState(false);

  // Calculate current resource
  const currentResource = resources[currentResourceIndex];
  // Track which field was last edited to drive precedence in sync logic
  const [lastEdited, setLastEdited] = useState<'start' | 'end' | 'days' | null>(null);

  // Helper functions for resources
  const addResource = () => {
    const newResource: ResourceData = {
      id: Date.now().toString(),
      name: newResourceName || `Technician ${resources.length + 1}`,
      daysOnSite: copyOnlyRateSheet ? 1 : resources[copyFromResourceIndex].daysOnSite,
      hoursPerDay: copyOnlyRateSheet ? 8 : resources[copyFromResourceIndex].hoursPerDay,
      startDay: copyOnlyRateSheet ? 'Monday' : resources[copyFromResourceIndex].startDay,
      holdoverDayEnabled: copyOnlyRateSheet ? false : resources[copyFromResourceIndex].holdoverDayEnabled,
      holdoverDayOfWeek: copyOnlyRateSheet ? 'Sunday' : resources[copyFromResourceIndex].holdoverDayOfWeek,
      separateTravelTo: copyOnlyRateSheet ? false : resources[copyFromResourceIndex].separateTravelTo,
      separateTravelFrom: copyOnlyRateSheet ? false : resources[copyFromResourceIndex].separateTravelFrom,
      travelMethod: copyOnlyRateSheet ? 'Driving' : resources[copyFromResourceIndex].travelMethod,
      travelDistance: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].travelDistance,
      travelTime: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].travelTime,
      dailyTravelDistance: copyOnlyRateSheet ? 15 : resources[copyFromResourceIndex].dailyTravelDistance,
      dailyTravelTime: copyOnlyRateSheet ? 0.25 : resources[copyFromResourceIndex].dailyTravelTime,
      discountPercent: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].discountPercent,
      isEmergency: copyOnlyRateSheet ? false : resources[copyFromResourceIndex].isEmergency,
      hotelRequired: copyOnlyRateSheet ? true : resources[copyFromResourceIndex].hotelRequired,
      rentalCarRequired: copyOnlyRateSheet ? false : resources[copyFromResourceIndex].rentalCarRequired,
      otherExpenses: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].otherExpenses,
      hotelCost: copyOnlyRateSheet ? (currentSheet?.hotelCost ?? 0) : resources[copyFromResourceIndex].hotelCost,
      rentalCarRate: copyOnlyRateSheet ? (currentSheet?.rentalCarRate ?? 0) : resources[copyFromResourceIndex].rentalCarRate,
      flightCost: copyOnlyRateSheet ? (currentSheet?.flightCost ?? 0) : resources[copyFromResourceIndex].flightCost,
      mileageRate: copyOnlyRateSheet ? (currentSheet?.mileageRate ?? 0) : resources[copyFromResourceIndex].mileageRate,
      perDiemRate: copyOnlyRateSheet ? (currentSheet?.perDiemRate ?? 0) : resources[copyFromResourceIndex].perDiemRate,
      includeSaturdays: copyOnlyRateSheet ? true : resources[copyFromResourceIndex].includeSaturdays,
      includeSundays: copyOnlyRateSheet ? true : resources[copyFromResourceIndex].includeSundays,
      technician: '',
      startDate: copyOnlyRateSheet ? null : resources[copyFromResourceIndex].startDate,
      endDate: copyOnlyRateSheet ? null : resources[copyFromResourceIndex].endDate,
      // Rate sheet is project-wide, not per-resource
      manualOverrides: copyOnlyRateSheet ? new Map() : new Map(resources[copyFromResourceIndex].manualOverrides),
      otBefore7After5: false,
    };
    
    setResources([...resources, newResource]);
    setCurrentResourceIndex(resources.length);
    setAddResourceDialogOpen(false);
    setNewResourceName('');
  };

  const deleteResource = (index: number) => {
    if (resources.length <= 1) return;
    const newResources = resources.filter((_, i) => i !== index);
    setResources(newResources);
    if (currentResourceIndex >= index && currentResourceIndex > 0) {
      setCurrentResourceIndex(currentResourceIndex - 1);
    } else if (currentResourceIndex >= newResources.length) {
      setCurrentResourceIndex(newResources.length - 1);
    }
  };

  // Date variables using currentResource values
  const startDate = currentResource.startDate ? dayjs(currentResource.startDate) : null;
  const endDate = currentResource.endDate ? dayjs(currentResource.endDate) : null;
  
  const calcResult = useMemo(() => {
    const customSheet = {
      ...currentSheet,
      hotelCost: currentResource.hotelCost,
      rentalCarRate: currentResource.rentalCarRate,
      flightCost: currentResource.flightCost,
      mileageRate: currentResource.mileageRate,
      perDiemRate: currentResource.perDiemRate
    };

    return calculateEstimate({
      daysOnSite: currentResource.daysOnSite,
      hoursPerDay: currentResource.hoursPerDay,
      startDayOfWeek: daysOfWeek.indexOf(currentResource.startDay),
      holdoverDayEnabled: currentResource.holdoverDayEnabled,
      holdoverDayOfWeek: daysOfWeek.indexOf(currentResource.holdoverDayOfWeek),
      separateTravelTo: currentResource.separateTravelTo,
      separateTravelFrom: currentResource.separateTravelFrom,
      travelMethod: currentResource.travelMethod,
      travelDistance: currentResource.travelDistance,
      travelTime: currentResource.travelTime,
      dailyTravelDistance: currentResource.dailyTravelDistance,
      dailyTravelTime: currentResource.dailyTravelTime,
      rateSheet: customSheet,
      discountPercent: currentResource.discountPercent,
      isEmergency: currentResource.isEmergency,
      hotelRequired: currentResource.hotelRequired,
      rentalCarRequired: currentResource.rentalCarRequired,
      otherExpenses: currentResource.otherExpenses,
      manualOverrides: currentResource.manualOverrides,
      includeSaturdays: currentResource.includeSaturdays,
      includeSundays: currentResource.includeSundays,
      otBefore7After5: currentResource.otBefore7After5,
      startTimeOnSite
    });
  }, [
    currentResource, currentSheet, startTimeOnSite
  ]);

  const filteredDayDetails = calcResult.dayDetails;

  // Build a true calendar grid
  const calendarWeeks = [];
  if (filteredDayDetails.length > 0) {
    let days = [...filteredDayDetails];
    let firstDayOfWeek = days[0].dayOfWeek;
    let week = Array(7).fill(null);
    let dayIdx = 0;
    for (let i = 0; i < firstDayOfWeek; i++) week[i] = null;
    for (let i = firstDayOfWeek; i < 7 && dayIdx < days.length; i++) week[i] = days[dayIdx++];
    calendarWeeks.push(week);
    while (dayIdx < days.length) {
      let week = Array(7).fill(null);
      for (let i = 0; i < 7 && dayIdx < days.length; i++) week[i] = days[dayIdx++];
      calendarWeeks.push(week);
    }
  }

  // Functions to update current resource
  const updateCurrentResource = useCallback((updates: Partial<ResourceData>) => {
    setResources(prevResources => {
      const newResources = [...prevResources];
      const before = newResources[currentResourceIndex];
      const after = { ...before, ...updates } as ResourceData;
      console.debug('[QE] updateCurrentResource', { updates, before, after, currentResourceIndex });
      newResources[currentResourceIndex] = after;
      return newResources;
    });
  }, [currentResourceIndex]);

  const updateCurrentResourceField = useCallback((field: keyof ResourceData, value: any) => {
    console.debug('[QE] updateCurrentResourceField', { field, value });
    updateCurrentResource({ [field]: value });
  }, [updateCurrentResource]);

  // Date logic effects
  useEffect(() => {
    // When both dates are present and the last edit was a date (start or end), compute inclusive Days
    if (startDate && endDate && lastEdited !== 'days') {
      const diff = endDate.diff(startDate, 'day') + 1;
      console.debug('[QE] Effect Dates->Days', {
        startDate: startDate.format('YYYY-MM-DD'),
        endDate: endDate.format('YYYY-MM-DD'),
        lastEdited,
        currentDays: currentResource.daysOnSite,
        computedDiff: diff
      });
      if (diff > 0 && currentResource.daysOnSite !== diff) {
        updateCurrentResourceField('daysOnSite', diff);
      }
    }
  }, [startDate, endDate, currentResource.daysOnSite, updateCurrentResourceField, lastEdited]);

  useEffect(() => {
    // If Days or Start changed, derive End = Start + (Days - 1). Do not override when End was the last edit
    if (lastEdited !== 'end' && startDate && currentResource.daysOnSite && (!endDate || !endDate.isSame(startDate.add(currentResource.daysOnSite - 1, 'day'), 'day'))) {
      console.debug('[QE] Effect Derive End', {
        lastEdited,
        startDate: startDate?.format('YYYY-MM-DD'),
        days: currentResource.daysOnSite,
        prevEnd: endDate?.format('YYYY-MM-DD')
      });
      updateCurrentResourceField('endDate', startDate.add(currentResource.daysOnSite - 1, 'day').format('YYYY-MM-DD'));
    }
  }, [startDate, currentResource.daysOnSite, endDate, updateCurrentResourceField, lastEdited]);

  useEffect(() => {
    // If only End exists and Days changed (or End changed first), derive Start = End - (Days - 1). Do not override when Start was the last edit
    if (lastEdited !== 'start' && endDate && currentResource.daysOnSite && !startDate) {
      console.debug('[QE] Effect Derive Start', {
        lastEdited,
        endDate: endDate?.format('YYYY-MM-DD'),
        days: currentResource.daysOnSite,
        prevStart: currentResource.startDate
      });
      updateCurrentResourceField('startDate', endDate.subtract(currentResource.daysOnSite - 1, 'day').format('YYYY-MM-DD'));
    }
  }, [endDate, currentResource.daysOnSite, startDate, updateCurrentResourceField, lastEdited]);

  useEffect(() => {
    if (startDate && endDate && endDate.isBefore(startDate, 'day')) {
      console.debug('[QE] Effect Guard end < start: clearing end', {
        startDate: startDate.format('YYYY-MM-DD'), endDate: endDate.format('YYYY-MM-DD')
      });
      updateCurrentResourceField('endDate', null);
    }
  }, [startDate, endDate, updateCurrentResourceField]);

  useEffect(() => {
    if (startDate) {
      console.debug('[QE] Effect Start->StartDay', { startDate: startDate.format('YYYY-MM-DD'), startDay: daysOfWeek[startDate.day()] });
      updateCurrentResourceField('startDay', daysOfWeek[startDate.day()]);
    }
  }, [startDate, updateCurrentResourceField]);

  // Date change handlers (use MUI DatePicker signatures)
  const handleStartDateChange = (val: Dayjs | null) => {
    console.debug('[QE] handleStartDateChange', { val: val ? val.format('YYYY-MM-DD') : null });
    setLastEdited('start');
    
    // Update all resources with the new start date and start day
    setResources(prevResources => {
      const newResources = [...prevResources];
      const newStartDate = val ? val.format('YYYY-MM-DD') : null;
      const newStartDay = val ? daysOfWeek[val.day()] : 'Monday';
      
      newResources.forEach((resource, index) => {
        newResources[index] = {
          ...resource,
          startDate: newStartDate,
          startDay: newStartDay
        };
      });
      
      return newResources;
    });
    
    // Immediately derive daysOnSite if an endDate is already selected
    if (val && endDate) {
      const diff = endDate.diff(val, 'day') + 1;
      if (diff > 0 && currentResource.daysOnSite !== diff) {
        updateCurrentResourceField('daysOnSite', diff);
      }
    }
  };

  const handleEndDateChange = (val: Dayjs | null) => {
    console.debug('[QE] handleEndDateChange', { val: val ? val.format('YYYY-MM-DD') : null });
    setLastEdited('end');
    
    // Update all resources with the new end date
    setResources(prevResources => {
      const newResources = [...prevResources];
      const newEndDate = val ? val.format('YYYY-MM-DD') : null;
      
      newResources.forEach((resource, index) => {
        newResources[index] = {
          ...resource,
          endDate: newEndDate
        };
      });
      
      return newResources;
    });
    
    // Immediately derive daysOnSite if a startDate is already selected
    if (val && startDate) {
      const diff = val.diff(startDate, 'day') + 1;
      if (diff > 0 && currentResource.daysOnSite !== diff) {
        updateCurrentResourceField('daysOnSite', diff);
      }
    }
  };

  const handleDaysOnSiteChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = Math.max(1, Number(e.target.value) || 1);
    console.debug('[QE] handleDaysOnSiteChange', { value });
    setLastEdited('days');
    // Batch updates atomically to avoid alternating UI states
    setResources(prevResources => {
      const resourcesCopy = [...prevResources];
      const current = resourcesCopy[currentResourceIndex];
      const next: ResourceData = { ...current, daysOnSite: value };
      const start = next.startDate ? dayjs(next.startDate) : null;
      const end = next.endDate ? dayjs(next.endDate) : null;
      if (start) {
        const computedEnd = start.add(value - 1, 'day');
        next.endDate = computedEnd.format('YYYY-MM-DD');
      } else if (end) {
        const computedStart = end.subtract(value - 1, 'day');
        next.startDate = computedStart.format('YYYY-MM-DD');
        next.startDay = daysOfWeek[computedStart.day()];
      }
      resourcesCopy[currentResourceIndex] = next;
      return resourcesCopy;
    });
  };

  const handleClearDates = () => {
    // Clear dates for all resources
    setResources(prevResources => {
      const newResources = [...prevResources];
      newResources.forEach((resource, index) => {
        newResources[index] = {
          ...resource,
          startDate: null,
          endDate: null,
          startDay: 'Monday'
        };
      });
      return newResources;
    });
  };

  // Save/Clone handlers
  const handleSaveClick = () => {
    setEstimateName(generateDefaultEstimateName());
    setSaveDialogOpen(true);
  };

  const handleCloneClick = () => {
    const defaultName = generateDefaultEstimateName();
    setEstimateName(defaultName ? `Copy of ${defaultName}` : 'Copy of Estimate');
    setCloneDialogOpen(true);
  };

  const handleSaveConfirm = async () => {
    if (estimateName.trim()) {
      const success = await saveCurrentEstimate(estimateName.trim());
      if (success) {
        setSaveDialogOpen(false);
        setEstimateName('');
      }
    }
  };

  const handleCloneConfirm = async () => {
    if (estimateName.trim()) {
      const success = await saveCurrentEstimate(estimateName.trim(), true);
      if (success) {
        setCloneDialogOpen(false);
        setEstimateName('');
      }
    }
  };



  // Track button handler
  const handleTrackClick = async () => {
    // Open results window directly with current estimate data
    setResultsWindowOpen(true);
  };

  // File input handler for tracking data
  const handleFileLoad = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    
    try {
      const loadedTrackingData = await TrackingDataManager.loadTrackingData(file);
      setTrackingData(loadedTrackingData);
      setResultsWindowOpen(true);
    } catch (error) {
      alert('Failed to load tracking file: ' + (error as Error).message);
    }
    
    // Reset file input
    event.target.value = '';
  };

  // File input handler for estimate data
  const handleEstimateFileLoad = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    
    try {
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const content = e.target?.result as string;
          const estimateData = JSON.parse(content);
          
          // Validate the data structure
          if (!estimateData.projectData || !estimateData.resources) {
            throw new Error('Invalid estimate file format');
          }
          
          loadEstimateData(estimateData);
          setSavedEstimatesDialogOpen(false);
        } catch (error) {
          setSaveError('Failed to parse estimate file: ' + (error as Error).message);
          setTimeout(() => setSaveError(null), 5000);
        }
      };
      
      reader.onerror = () => {
        setSaveError('Failed to read estimate file');
        setTimeout(() => setSaveError(null), 5000);
      };
      
      reader.readAsText(file);
    } catch (error) {
      setSaveError('Failed to load estimate file: ' + (error as Error).message);
      setTimeout(() => setSaveError(null), 5000);
    }
    
    // Reset file input
    event.target.value = '';
  };



  // Excel export handler
  const handleExportToExcel = async () => {
    let customerToUse = customer.trim();
    if (!customerToUse) {
      const customerPrompt = prompt('Please enter the customer name:');
      if (!customerPrompt || customerPrompt.trim() === '') {
        alert('Customer name is required for export.');
        return;
      }
      customerToUse = customerPrompt.trim();
    }

    // Use TrackingDataManager to get multi-resource data
    const exportProjectData = {
      projectNumber,
      customer,
      projectDescription,
      technician: technician
    };
    
    const trackingData = TrackingDataManager.initializeTrackingData(
      resources,
      rateSheets,
      exportProjectData,
      startDate,
      endDate,
      selectedSheet
    );

    // Calculate project-wide totals from all resources
    const projectTotals = {
      totalLabourHours: 0,
      totalTravelHours: 0,
      totalLabourCost: 0,
      totalTravelCost: 0,
      totalExpenses: 0,
      grandTotal: 0,
      totalDays: 0
    };

    // Find earliest start date and latest end date across all resources
    let earliestStartDate: dayjs.Dayjs | null = null;
    let latestEndDate: dayjs.Dayjs | null = null;

    trackingData.resources.forEach(resource => {
      resource.days.forEach(day => {
        // Sum up totals
        const dayLabourHours = (day.planned.regularLabour || 0) + (day.planned.overtimeLabour || 0) + (day.planned.premiumLabour || 0);
        const dayTravelHours = (day.planned.regularTravel || 0) + (day.planned.overtimeTravel || 0) + (day.planned.premiumTravel || 0);
        
        projectTotals.totalLabourHours += dayLabourHours;
        projectTotals.totalTravelHours += dayTravelHours;
        projectTotals.totalDays++;

        // Calculate costs using the project's rate sheet (same for all resources)
        const projectRateSheet = rateSheets.find(s => s.name === selectedSheet) || rateSheets[0];
        const dayLabourCost = 
          (day.planned.regularLabour || 0) * projectRateSheet.regularLabourRate +
          (day.planned.overtimeLabour || 0) * projectRateSheet.overtimeLabourRate +
          (day.planned.premiumLabour || 0) * projectRateSheet.premiumLabourRate;
        const dayTravelCost = 
          (day.planned.regularTravel || 0) * projectRateSheet.regularTravelRate +
          (day.planned.overtimeTravel || 0) * projectRateSheet.overtimeTravelRate +
          (day.planned.premiumTravel || 0) * projectRateSheet.premiumTravelRate;
        
        projectTotals.totalLabourCost += dayLabourCost;
        projectTotals.totalTravelCost += dayTravelCost;
        projectTotals.totalExpenses += (day.planned.mileage || 0) + (day.planned.perDiem || 0) + 
                                      (day.planned.flight || 0) + (day.planned.carRental || 0) + 
                                      (day.planned.hotel || 0);

        // Track date range
        if (day.date) {
          const dayDate = dayjs(day.date);
          if (!earliestStartDate || dayDate.isBefore(earliestStartDate)) {
            earliestStartDate = dayDate;
          }
          if (!latestEndDate || dayDate.isAfter(latestEndDate)) {
            latestEndDate = dayDate;
          }
        }
      });
    });

    projectTotals.grandTotal = projectTotals.totalLabourCost + projectTotals.totalTravelCost + projectTotals.totalExpenses;

    // Build the summary sheet row by row, matching the user's requirements
    const summaryRows = [];
    // 1. Header
    summaryRows.push(['Time and Expense Estimate', '', '']); // 1
    // 2-5: Project info
    summaryRows.push(['Customer:', customer, '']); // 2
    summaryRows.push(['Description:', projectDescription, '']); // 3
    summaryRows.push(['Project Number:', projectNumber, '']); // 4
    summaryRows.push(['Number of Resources:', resources.length.toString(), '']); // 5
    // 6: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 6
    // 7-9: Date info
    summaryRows.push(['Start Date On Site:', earliestStartDate ? (earliestStartDate as dayjs.Dayjs).format('YYYY-MM-DD') : '', '']); // 7
    summaryRows.push(['End Site Date:', latestEndDate ? (latestEndDate as dayjs.Dayjs).format('YYYY-MM-DD') : '', '']); // 8
    summaryRows.push(['Total Days:', projectTotals.totalDays.toString(), '']); // 9
    // 10-11: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 10
    summaryRows.push(['', '', '']); // 11
    // 12-15: Rates/options (label in A, value in B)
    summaryRows.push(['Rates:', selectedSheet, '']); // 12
    summaryRows.push(['Separate Travel Days:', 'See Daily Breakdown', '']); // 13
    summaryRows.push(['Travel Method to Site Area:', 'See Daily Breakdown', '']); // 14
    summaryRows.push(['Emergency Rates:', 'See Daily Breakdown', '']); // 15
    // 16-17: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 16
    summaryRows.push(['', '', '']); // 17
    // 18: Summary table header
    summaryRows.push(['', 'Hours', 'Cost']); // 18
    // 19-21: Summary table
    summaryRows.push(['Labour:', projectTotals.totalLabourHours.toString(), projectTotals.totalLabourCost.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 19
    summaryRows.push(['Travel:', projectTotals.totalTravelHours.toString(), projectTotals.totalTravelCost.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 20
    summaryRows.push(['Expenses (Cost + 10%, not incl. per diem):', 'N/A', projectTotals.totalExpenses.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 21
    // 22: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 22
    // 23: Grand total
    summaryRows.push(['Grand Total:', (projectTotals.totalLabourHours + projectTotals.totalTravelHours).toString(), projectTotals.grandTotal.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 23

    // Create workbook and worksheet
    const workbook = new ExcelJS.Workbook();
    const ws = workbook.addWorksheet('Summary');
    ws.views = [{ showGridLines: false }];
    ws.columns = [ { width: 28 }, { width: 22 }, { width: 18 } ];
    for (const row of summaryRows) ws.addRow(row);

    // Merges
    ws.mergeCells('A1:C1'); // header
    ws.mergeCells('A6:C6');
    // Merge A10:C11 and A16:C17 for spacers (removes border between these rows)
    ws.mergeCells('A10:C11');
    ws.mergeCells('A16:C17');
    ws.mergeCells('A22:C22');
    // Merge B and C for rows 2 to 15 (except ignored rows)
    for (let r = 2; r <= 15; r++) {
      if (![6, 10, 11, 16, 17, 22].includes(r)) {
        ws.mergeCells(`B${r}:C${r}`);
      }
    }

    // Header row
    ws.getCell('A1').font = { bold: true, size: 18 };
    ws.getCell('A1').alignment = { horizontal: 'center', vertical: 'middle' };
    ws.getCell('A1').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFD9E1F2' } };
    ws.getRow(1).height = 28;

    // Project info (2-5): A right, B/C left
    for (let r = 2; r <= 5; r++) {
      ws.getCell(`A${r}`).font = { size: 12 };
      ws.getCell(`A${r}`).alignment = { horizontal: 'right', vertical: 'middle' };
      ws.getCell(`B${r}`).font = { bold: true, size: 12 };
      ws.getCell(`B${r}`).alignment = { horizontal: 'left', vertical: 'middle' };
    }
    // Row 6 merged, empty, very light gray
    ws.getCell('A6').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
    // Date info (7-9): A right, B/C left
    for (let r = 7; r <= 9; r++) {
      ws.getCell(`A${r}`).font = { size: 12 };
      ws.getCell(`A${r}`).alignment = { horizontal: 'right', vertical: 'middle' };
      ws.getCell(`B${r}`).font = { bold: true, size: 12 };
      ws.getCell(`B${r}`).alignment = { horizontal: 'left', vertical: 'middle' };
    }
    // Rows 10-11 merged, empty, very light gray
    ws.getCell('A10').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
    ws.getCell('A16').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
    // Rates/options (12-15): label in A, value in B/C left
    for (let r = 12; r <= 15; r++) {
      ws.getCell(`A${r}`).font = { size: 12 };
      ws.getCell(`A${r}`).alignment = { horizontal: 'right', vertical: 'middle' };
      ws.getCell(`B${r}`).font = { bold: true, size: 12 };
      ws.getCell(`B${r}`).alignment = { horizontal: 'left', vertical: 'middle' };
    }
    // Rows 18: summary table header
    ws.getCell('B18').font = { bold: true, size: 12 };
    ws.getCell('C18').font = { bold: true, size: 12 };
    ws.getCell('B18').alignment = { horizontal: 'center', vertical: 'middle' };
    ws.getCell('C18').alignment = { horizontal: 'center', vertical: 'middle' };
    ws.getCell('B18').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    ws.getCell('C18').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    ws.getRow(18).height = 22;
    // Rows 19-21: summary table
    for (let r = 19; r <= 21; r++) {
      ws.getCell(`A${r}`).font = { size: 12 };
      ws.getCell(`A${r}`).alignment = { horizontal: 'right', vertical: 'middle' };
      ws.getCell(`B${r}`).font = { size: 12 };
      ws.getCell(`B${r}`).alignment = { horizontal: 'center', vertical: 'middle' };
      ws.getCell(`C${r}`).font = { size: 12 };
      ws.getCell(`C${r}`).alignment = { horizontal: 'center', vertical: 'middle' };
    }
    // Row 22 merged, empty, very light gray
    ws.getCell('A22').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
    // Row 23: Grand total
    ws.getCell('A23').font = { bold: true, size: 12 };
    ws.getCell('A23').alignment = { horizontal: 'right', vertical: 'middle' };
    ws.getCell('B23').font = { size: 12 };
    ws.getCell('B23').alignment = { horizontal: 'center', vertical: 'middle' };
    ws.getCell('C23').font = { bold: true, size: 12 };
    ws.getCell('C23').alignment = { horizontal: 'center', vertical: 'middle' };
    ws.getCell('C23').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFF00' } };
    ws.getRow(23).height = 22;

    // Borders for all cells in the used range
    for (let r = 1; r <= 23; r++) {
      for (let c = 1; c <= 3; c++) {
        // Remove borders for merged spacer rows (A10:C11 and A16:C17)
        if ((r === 10 || r === 11 || r === 16 || r === 17) && c >= 1 && c <= 3) continue;
        ws.getCell(r, c).border = {
          top: { style: 'medium' },
          left: { style: 'medium' },
          bottom: { style: 'medium' },
          right: { style: 'medium' }
        };
      }
    }
    // Add right border to C10 and C16 for merged spacers
    ws.getCell('C10').border = { right: { style: 'medium' } };
    ws.getCell('C16').border = { right: { style: 'medium' } };

    // --- DETAILED BREAKDOWN SHEET ---
    const wsDetail = workbook.addWorksheet('Daily Breakdown');
    wsDetail.views = [{ showGridLines: false }];
    wsDetail.columns = [
      { header: '', width: 16 }, // Resource
      { header: '', width: 14 }, // Date
      { header: '', width: 14 }, // Day
      { header: '', width: 14 }, // Type
      { header: 'Labour', width: 14 }, // RegLab
      { header: '', width: 14 }, // OT Lab
      { header: '', width: 14 }, // PremLab
      { header: '', width: 14 }, // TotLab
      { header: 'Travel', width: 14 }, // RegTrav
      { header: '', width: 14 }, // OT Trav
      { header: '', width: 14 }, // PremTrav
      { header: '', width: 14 }, // TotTrav
      { header: 'Labour', width: 14 }, // Labour
      { header: 'Travel', width: 14 }, // Travel
      { header: 'Hotel', width: 14 }, // Hotel
      { header: 'Per Diem', width: 14 }, // Per Diem
      { header: 'Mileage', width: 14 }, // Mileage
      { header: 'Rental Car', width: 14 }, // Rental Car
      { header: 'Airfare', width: 14 }, // Airfare
      { header: '', width: 20 }  // Total Day Cost (column T)
    ];
    // Title row
    wsDetail.mergeCells(1, 1, 1, 20);
    wsDetail.getCell('A1').value = 'Daily Breakdown';
    wsDetail.getCell('A1').font = { bold: true, size: 16 };
    wsDetail.getCell('A1').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.getCell('A1').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFD9E1F2' } };
    wsDetail.getRow(1).height = 26;
    // Multi-level header rows
    // First header row: group headers
    wsDetail.mergeCells('A2:A3'); wsDetail.getCell('A2').value = 'Resource';
    wsDetail.mergeCells('B2:D2'); wsDetail.getCell('B2').value = '';
    wsDetail.mergeCells('E2:H2'); wsDetail.getCell('E2').value = 'Labour Hours';
    wsDetail.mergeCells('I2:L2'); wsDetail.getCell('I2').value = 'Travel Hours';
    wsDetail.getCell('M2').value = 'Daily Cost';
    wsDetail.mergeCells('M2:N2');
    wsDetail.getCell('M2').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.getCell('O2').value = 'Expenses (Cost +10%, not including per diem)';
    wsDetail.mergeCells('O2:S2');
    wsDetail.getCell('O2').alignment = { horizontal: 'center', vertical: 'middle' };
    // --- ENSURE T2:T3 MERGE IS LAST ---
    wsDetail.mergeCells('T2:T3');
    wsDetail.getCell('T2').value = 'Total Day Cost';
    wsDetail.getCell('T2').alignment = { horizontal: 'center', vertical: 'middle' };
    // Second header row: subheaders
    const headerRow2 = wsDetail.getRow(3);
    headerRow2.values = [
      '', 'Date', 'Day', 'Type',
      'Regular', 'Overtime', 'Premium', 'Total',
      'Regular', 'Overtime', 'Premium', 'Total',
      'Labour', 'Travel', 'Hotel', 'Per Diem', 'Mileage', 'Rental Car', 'Airfare', ''
    ];
    // Style both header rows
    for (let c = 1; c <= 20; c++) {
      wsDetail.getCell(2, c).font = { bold: true, size: 12 };
      wsDetail.getCell(2, c).alignment = { horizontal: 'center', vertical: 'middle' };
      wsDetail.getCell(2, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
      wsDetail.getCell(3, c).font = { bold: true, size: 12 };
      wsDetail.getCell(3, c).alignment = { horizontal: 'center', vertical: 'middle' };
      wsDetail.getCell(3, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    }
    wsDetail.getRow(2).height = 22;
    wsDetail.getRow(3).height = 22;

    // Collect all day details from all resources
    const allDayDetails: Array<{
      resourceName: string;
      day: any;
      resourceRateSheet: any;
    }> = [];

    // Use the project's rate sheet for all resources
    const projectRateSheet = rateSheets.find(s => s.name === selectedSheet) || rateSheets[0];
    
    trackingData.resources.forEach(resource => {
      resource.days.forEach(day => {
        allDayDetails.push({
          resourceName: resource.resourceName,
          day,
          resourceRateSheet: projectRateSheet
        });
      });
    });

    // Sort by date, then by resource name
    allDayDetails.sort((a, b) => {
      if (a.day.date && b.day.date) {
        const dateCompare = dayjs(a.day.date).diff(dayjs(b.day.date));
        if (dateCompare !== 0) return dateCompare;
      }
      return a.resourceName.localeCompare(b.resourceName);
    });

    // Data rows
    let currentRow = 4;
    let currentDateKey = '';
    const dayRows: any[] = [];

    allDayDetails.forEach(({ resourceName, day, resourceRateSheet }) => {
      // Add date separator if this is a new date
      if (day.date !== currentDateKey) {
        if (currentDateKey !== '') {
          // Add blank separator row between different dates
          dayRows.push(Array(20).fill(''));
          currentRow++;
        }
        currentDateKey = day.date;
      }

      // Calculate costs using the resource's rate sheet
      const dayLabourCost = 
        (day.planned.regularLabour || 0) * resourceRateSheet.regularLabourRate +
        (day.planned.overtimeLabour || 0) * resourceRateSheet.overtimeLabourRate +
        (day.planned.premiumLabour || 0) * resourceRateSheet.premiumLabourRate;
      const dayTravelCost = 
        (day.planned.regularTravel || 0) * resourceRateSheet.regularTravelRate +
        (day.planned.overtimeTravel || 0) * resourceRateSheet.overtimeTravelRate +
        (day.planned.premiumTravel || 0) * resourceRateSheet.premiumTravelRate;

      const dayRow = [
        resourceName,
        day.date || '',
        daysOfWeek[day.dayOfWeek] || '',
        getDayTypeDisplay(day.type, day.isHoldover),
        day.planned.regularLabour || 0,
        day.planned.overtimeLabour || 0,
        day.planned.premiumLabour || 0,
        (day.planned.regularLabour || 0) + (day.planned.overtimeLabour || 0) + (day.planned.premiumLabour || 0),
        day.planned.regularTravel || 0,
        day.planned.overtimeTravel || 0,
        day.planned.premiumTravel || 0,
        (day.planned.regularTravel || 0) + (day.planned.overtimeTravel || 0) + (day.planned.premiumTravel || 0),
        dayLabourCost,
        dayTravelCost,
        day.planned.hotel || 0,
        day.planned.perDiem || 0,
        day.planned.mileage || 0,
        day.planned.carRental || 0,
        day.planned.flight || 0,
        dayLabourCost + dayTravelCost + (day.planned.hotel || 0) + (day.planned.perDiem || 0) + 
        (day.planned.mileage || 0) + (day.planned.carRental || 0) + (day.planned.flight || 0)
      ];
      
      dayRows.push(dayRow);
      currentRow++;
    });

    wsDetail.addRows(dayRows);

    // Totals row
    const totalsRow = [
      'TOTAL', '', '', '',
      ...[4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20].map(idx => {
        // Sum each numeric column (skip text columns)
        if (idx >= 4) {
          let sum = 0;
          for (let r = 0; r < dayRows.length; r++) {
            const val = dayRows[r][idx];
            if (typeof val === 'number') sum += val;
          }
          return sum;
        }
        return '';
      })
    ];
    wsDetail.addRow(totalsRow);
    const totalsRowIdx = 4 + dayRows.length;
    for (let c = 5; c <= 20; c++) {
      wsDetail.getCell(totalsRowIdx, c).font = { bold: true, color: { argb: 'FF000000' } };
      wsDetail.getCell(totalsRowIdx, c).numFmt = c >= 13 ? '$#,##0.00' : '0.00';
      wsDetail.getCell(totalsRowIdx, c).alignment = { horizontal: 'right', vertical: 'middle' };
      wsDetail.getCell(totalsRowIdx, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    }
    wsDetail.getCell(totalsRowIdx, 1).font = { bold: true };
    wsDetail.getRow(totalsRowIdx).height = 22;

    // Format data rows and add resource separators
    let rowIndex = 4;
    for (let r = 0; r < dayRows.length; r++) {
      const row = wsDetail.getRow(rowIndex);
      const dayRow = dayRows[r];
      
      // Check if this is a separator row (all empty values)
      const isSeparatorRow = dayRow.every((val: any) => val === '');
      
      if (isSeparatorRow) {
        // Format separator row
        wsDetail.mergeCells(rowIndex, 1, rowIndex, 20);
        wsDetail.getCell(rowIndex, 1).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
      } else {
        // Format data row
        // Alternating fill
        if (rowIndex % 2 === 1) {
          row.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF8F8F8' } };
        }
        // Align text/number columns
        for (let c = 1; c <= 20; c++) {
          let cell = row.getCell(c);
          if ([1,2,3,4].includes(c)) cell.alignment = { horizontal: 'left', vertical: 'middle' };
          else cell.alignment = { horizontal: 'right', vertical: 'middle' };
          // Currency formatting for cost columns
          if ([13,14,15,16,17,18,19,20].includes(c)) cell.numFmt = '$#,##0.00';
          // Highlight cost columns
          if ([13,14,15,16,17,18,19,20].includes(c)) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFFCC' } };
          // Zero value cells in light grey
          const v = cell.value;
          if (v === 0 || v === '0' || v === 0.0 || v === '0.00' || v === '$0.00') {
            cell.font = { ...cell.font, color: { argb: 'FFB0B0B0' } };
          }
        }
      }
      rowIndex++;
    }

    // Borders for all cells
    for (let r = 1; r <= totalsRowIdx; r++) {
      for (let c = 1; c <= 20; c++) {
        wsDetail.getCell(r, c).border = {
          top: { style: 'thin' },
          left: { style: 'thin' },
          bottom: { style: 'thin' },
          right: { style: 'thin' }
        };
      }
    }

    // Save file
    const buf = await workbook.xlsx.writeBuffer();
    
    // Generate filename: "Estimate - [Customer] [Project Description] - [Current Date]"
    const currentDate = dayjs().format('YYYY-MM-DD');
    const projectText = projectDescription.trim() || 'Support';
    const filename = `Estimate - ${customerToUse} ${projectText} - ${currentDate}.xlsx`;
    
    saveAs(new Blob([buf], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), filename);
  };

  // Helper to get friendly type label
  const friendlyType = (day: DayDetail, manualOverrides: Map<number, { dayType: string }>) => {
    const override = manualOverrides.get(day.dayNumber);
    if (override && override.dayType === 'Holdover') return 'Holdover';
    switch (day.type) {
      case 'WorkDay': return 'Work Day';
      case 'TravelTo': return 'Travel To';
      case 'TravelFrom': return 'Travel From';
      case 'None': return 'None';
      default: return day.type;
    }
  };

  // Helper to get day type display for Excel export
  const getDayTypeDisplay = (type: string, isHoldover: boolean) => {
    if (isHoldover) return 'Holdover';
    switch (type) {
      case 'WorkDay': return 'Work Day';
      case 'TravelTo': return 'Travel To';
      case 'TravelFrom': return 'Travel From';
      case 'None': return 'None';
      default: return type;
    }
  };

  // Manual override state
  const [dayEditorOpen, setDayEditorOpen] = useState(false);
  const [editingDay, setEditingDay] = useState<number | null>(null);

  // Day editor handlers
  const handleDayClick = (day: typeof filteredDayDetails[number]) => {
    setEditingDay(day.dayNumber);
    setDayEditorOpen(true);
  };

  const handleDaySave = (override: ManualDayOverride | null) => {
    if (editingDay) {
      const newOverrides = new Map(currentResource.manualOverrides);
      if (override) {
        newOverrides.set(editingDay, override);
      } else {
        newOverrides.delete(editingDay);
      }
      updateCurrentResourceField('manualOverrides', newOverrides);
    }
  };

  const getDefaultDayType = (day: typeof filteredDayDetails[number]): DayType => {
    if (day.type === 'TravelTo' || day.type === 'TravelFrom') return DayType.Travel;
    if (day.type === 'None') return DayType.Nil;
    return DayType.Work;
  };

  // Robust local state for linked travel fields
  // REMOVE localTravelTime and localTravelDistance state

  const handleTravelTimeChange = (val: number) => {
    const roundedHours = Math.max(0.25, roundToNearest(val, 0.25));
    const miles = Math.max(15, roundToNearest(roundedHours * 60, 15));
    updateCurrentResource({
      dailyTravelTime: roundedHours,
      dailyTravelDistance: miles
    });
  };

  const handleTravelDistanceChange = (val: number) => {
    const roundedMiles = Math.max(15, roundToNearest(val, 15));
    const hours = Math.max(0.25, roundToNearest(roundedMiles / 60, 0.25));
    updateCurrentResource({
      dailyTravelDistance: roundedMiles,
      dailyTravelTime: hours
    });
  };

  // Calculations for summary and totals
  const totalLabourHours = filteredDayDetails.reduce((sum, d) => sum + (d.totalLabourHours ?? 0), 0);
  const totalTravelHours = filteredDayDetails.reduce((sum, d) => sum + (d.totalTravelHours ?? 0), 0);
  const totalLabourCost = filteredDayDetails.reduce((sum, d) => sum + (d.labourCost ?? 0), 0);
  const totalTravelCost = filteredDayDetails.reduce((sum, d) => sum + (d.travelCost ?? 0), 0);
  const totalExpenses = filteredDayDetails.reduce((sum, d) =>
    sum +
      ((d.hotelCost ?? 0) * 1.1) +
      ((d.mileageCost ?? 0) * 1.1) +
      ((d.rentalCarCost ?? 0) * 1.1) +
      ((d.airfareCost ?? 0) * 1.1) +
      (d.perDiem ?? 0),
    0
  );
  const grandTotal = filteredDayDetails.reduce((sum, d) =>
    sum +
      (d.labourCost ?? 0) +
      (d.travelCost ?? 0) +
      ((d.hotelCost ?? 0) * 1.1) +
      ((d.mileageCost ?? 0) * 1.1) +
      ((d.rentalCarCost ?? 0) * 1.1) +
      ((d.airfareCost ?? 0) * 1.1) +
      (d.perDiem ?? 0),
    0
  );
  const totalDays = filteredDayDetails.length;

  return (
    <Box p={1} sx={{ background: darkMode ? '#23262b' : '#fff', minHeight: '100vh', color: panelText, boxSizing: 'border-box' }}>
      {/* Success/Error Messages */}
      {saveSuccess && (
        <Box sx={{ 
          mb: 2, 
          p: 2, 
          bgcolor: '#4caf50', 
          color: '#fff', 
          borderRadius: 1,
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center'
        }}>
          <Typography>{saveSuccess}</Typography>
          <IconButton 
            size="small" 
            onClick={() => setSaveSuccess(null)}
            sx={{ color: '#fff' }}
          >
            ×
          </IconButton>
        </Box>
      )}
      
      {saveError && (
        <Box sx={{ 
          mb: 2, 
          p: 2, 
          bgcolor: '#f44336', 
          color: '#fff', 
          borderRadius: 1,
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center'
        }}>
          <Typography>{saveError}</Typography>
          <IconButton 
            size="small" 
            onClick={() => setSaveError(null)}
            sx={{ color: '#fff' }}
          >
            ×
          </IconButton>
        </Box>
      )}

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
        <Typography variant="h6" sx={{ color: panelText }}>
          Time & Expense Calculator
        </Typography>
        {onBackToWelcome && (
          <Button
            variant="outlined"
            size="small"
            onClick={onBackToWelcome}
            sx={{ color: panelText, borderColor: panelBorder }}
          >
            Back to Welcome
          </Button>
        )}
      </Box>
      
      {/* Resource Tabs */}
      <Box sx={{ mb: 1 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
          <Tabs 
            value={currentResourceIndex} 
            onChange={(_, newValue) => setCurrentResourceIndex(newValue)}
            variant="scrollable"
            scrollButtons="auto"
            sx={{ 
              flex: 1,
              '& .MuiTab-root': {
                minWidth: 'auto',
                px: 2,
                py: 1,
                fontSize: '0.875rem'
              }
            }}
          >
            {resources.map((resource, index) => (
              <Tab 
                key={resource.id}
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography
                      variant="body2"
                      sx={{
                        fontSize: '0.875rem',
                        color: 'inherit',
                        fontWeight: 'medium'
                      }}
                    >
                      {resource.name}
                    </Typography>
                    {resources.length > 1 && (
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          deleteResource(index);
                        }}
                        disabled={resources.length <= 1}
                        sx={{ 
                          p: 0.5, 
                          ml: 0.5,
                          '&:hover': { bgcolor: 'error.main', color: 'white' }
                        }}
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    )}
                  </Box>
                }
              />
            ))}
          </Tabs>
          <Button
            size="small"
            variant="outlined"
            startIcon={<PersonAddIcon />}
            onClick={() => {
              setNewResourceName(`Technician ${resources.length + 1}`);
              setCopyFromResourceIndex(currentResourceIndex);
              setCopyOnlyRateSheet(false);
              setAddResourceDialogOpen(true);
            }}
            sx={{ ml: 1 }}
          >
            Add Resource
          </Button>
        </Box>
      </Box>
      
      <Grid container spacing={1} alignItems="flex-start">
        {/* Left Column: Rates, Travel Options, Expenses */}
        <Grid item xs={4}>
          <Grid container direction="column" spacing={1}>
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText }}>
                <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Rates</Typography>
                <Grid container spacing={1.5} alignItems="center">
                  <Grid item xs={12}>
                    <Select
                      size="small"
                      fullWidth
                      value={selectedSheet}
                      onChange={e => setSelectedSheet(e.target.value)}
                    >
                      {rateSheets.map(sheet => (
                        <MenuItem key={sheet.name} value={sheet.name}>{sheet.name}</MenuItem>
                      ))}
                    </Select>
                  </Grid>
                  <Grid item xs={6}>
                    <TextField 
                      size="small" 
                      label="Discount" 
                      type="number" 
                      fullWidth 
                      value={currentResource.discountPercent} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('discountPercent', value);
                      }} 
                      InputProps={{ endAdornment: <InputAdornment position="end">%</InputAdornment> }} 
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <FormControlLabel 
                      control={
                        <Checkbox 
                          size="small" 
                          checked={currentResource.isEmergency} 
                          onChange={e => updateCurrentResourceField('isEmergency', e.target.checked)} 
                        />
                      } 
                      label={<Typography variant="caption">Emergency</Typography>} 
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Box sx={{ mt: 1 }}>
                      <Grid container spacing={0.5}>
                        <Grid item xs={12}>
                          <Box display="flex" alignItems="center" sx={{ mb: 1 }}>
                            <span style={{ minWidth: 140 }}>Regular Labour</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.regularLabourRate ?? 0, currentSheet?.premiumLabourRate ?? 0)} / hr</span>
                            <span style={{ minWidth: 60 }}></span>
                            <span style={{ minWidth: 140 }}>Regular Travel</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.regularTravelRate ?? 0, currentSheet?.premiumTravelRate ?? 0)} / hr</span>
                          </Box>
                          <Box display="flex" alignItems="center" sx={{ mb: 1 }}>
                            <span style={{ minWidth: 140 }}>Overtime Labour</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.overtimeLabourRate ?? (currentSheet?.regularLabourRate ?? 0) * 1.5, currentSheet?.premiumLabourRate ?? 0)} / hr</span>
                            <span style={{ minWidth: 60 }}></span>
                            <span style={{ minWidth: 140 }}>Overtime Travel</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.overtimeTravelRate ?? (currentSheet?.regularTravelRate ?? 0) * 1.5, currentSheet?.premiumTravelRate ?? 0)} / hr</span>
                          </Box>
                          <Box display="flex" alignItems="center">
                            <span style={{ minWidth: 140 }}>Premium Labour</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.premiumLabourRate ?? (currentSheet?.regularLabourRate ?? 0) * 2, currentSheet?.premiumLabourRate ?? 0)} / hr</span>
                            <span style={{ minWidth: 60 }}></span>
                            <span style={{ minWidth: 140 }}>Premium Travel</span>
                            <span style={{ minWidth: 100 }}>${getDiscountedRate(currentSheet?.premiumTravelRate ?? (currentSheet?.regularTravelRate ?? 0) * 2, currentSheet?.premiumTravelRate ?? 0)} / hr</span>
                          </Box>
                        </Grid>
                      </Grid>
                    </Box>
                  </Grid>
                </Grid>
              </Box>
            </Grid>

            {/* Travel Options */}
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText }}>
                <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Travel Options</Typography>
                <Grid container spacing={1.5} alignItems="center">
                  <Grid item xs={6}>
                    <FormControlLabel 
                      control={
                        <Checkbox 
                          size="small" 
                          checked={currentResource.separateTravelTo} 
                          onChange={e => updateCurrentResourceField('separateTravelTo', e.target.checked)} 
                        />
                      } 
                      label={<Typography variant="caption">Separate Travel Day To</Typography>} 
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <FormControlLabel 
                      control={
                        <Checkbox 
                          size="small" 
                          checked={currentResource.separateTravelFrom} 
                          onChange={e => updateCurrentResourceField('separateTravelFrom', e.target.checked)} 
                        />
                      } 
                      label={<Typography variant="caption">Separate Travel Day From</Typography>} 
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField
                      select
                      size="small"
                      fullWidth
                      label="Travel Method to Site Area"
                      value={currentResource.travelMethod}
                      onChange={e => updateCurrentResourceField('travelMethod', e.target.value)}
                    >
                      <MenuItem value="Driving">Driving</MenuItem>
                      <MenuItem value="Flight">Flight</MenuItem>
                    </TextField>
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Driving Distance (First and Last Days Only)" 
                      type="number" 
                      fullWidth 
                      value={currentResource.travelDistance} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('travelDistance', value);
                      }} 
                      InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Total Travel Time to Site Area (Including Flight)" 
                      type="number" 
                      fullWidth 
                      value={currentResource.travelTime}
                      onChange={e => {
                        const value = Math.max(0.25, roundToNearest(Number(e.target.value), 0.25));
                        updateCurrentResourceField('travelTime', value);
                      }}
                      inputProps={{ min: 0.25, step: 0.25 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Daily Driving Distance (One Way)" 
                      type="number" 
                      fullWidth 
                      value={currentResource.dailyTravelDistance}
                      onChange={e => handleTravelDistanceChange(Number(e.target.value))}
                      inputProps={{ min: 15, step: 15 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Daily Travel Time (One Way)" 
                      type="number" 
                      fullWidth 
                      value={currentResource.dailyTravelTime}
                      onChange={e => handleTravelTimeChange(Number(e.target.value))}
                      inputProps={{ min: 0.25, step: 0.25 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                    />
                  </Grid>
                </Grid>
              </Box>
            </Grid>

            {/* Expenses */}
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ minHeight: 207, background: panelBg, borderColor: panelBorder, color: panelText }}>
                <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Expenses</Typography>
                <Grid container spacing={1.5} alignItems="center">
                  <Grid item xs={6} display="flex" alignItems="center">
                    <FormControlLabel 
                      control={
                        <Checkbox 
                          size="small" 
                          checked={currentResource.hotelRequired} 
                          onChange={e => updateCurrentResourceField('hotelRequired', e.target.checked)} 
                        />
                      } 
                      label={<Typography variant="caption">Hotel Required</Typography>} 
                      sx={{ mr: 1 }} 
                    />
                    <TextField 
                      size="small" 
                      label="Hotel" 
                      type="number" 
                      value={currentResource.hotelCost} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('hotelCost', value);
                      }} 
                      fullWidth 
                      disabled={!currentResource.hotelRequired} 
                      InputProps={{ 
                        startAdornment: <InputAdornment position="start">$</InputAdornment>, 
                        endAdornment: <InputAdornment position="end">per night</InputAdornment> 
                      }} 
                    />
                  </Grid>
                  <Grid item xs={6} display="flex" alignItems="center">
                    <FormControlLabel 
                      control={
                        <Checkbox 
                          size="small" 
                          checked={currentResource.rentalCarRequired} 
                          onChange={e => updateCurrentResourceField('rentalCarRequired', e.target.checked)} 
                        />
                      } 
                      label={<Typography variant="caption">Rental Car Required</Typography>} 
                      sx={{ mr: 1 }} 
                    />
                    <TextField 
                      size="small" 
                      label="Rental Car" 
                      type="number" 
                      value={currentResource.rentalCarRate} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('rentalCarRate', value);
                      }} 
                      fullWidth 
                      disabled={!currentResource.rentalCarRequired} 
                      InputProps={{ 
                        startAdornment: <InputAdornment position="start">$</InputAdornment>, 
                        endAdornment: <InputAdornment position="end">per day</InputAdornment> 
                      }} 
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Flight Cost (One Way)" 
                      type="number" 
                      value={currentResource.flightCost} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('flightCost', value);
                      }} 
                      fullWidth 
                      InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment> }} 
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Mileage" 
                      type="number" 
                      value={currentResource.mileageRate} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('mileageRate', value);
                      }} 
                      fullWidth 
                      InputProps={{ 
                        startAdornment: <InputAdornment position="start">$</InputAdornment>, 
                        endAdornment: <InputAdornment position="end">per mile/km</InputAdornment> 
                      }} 
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField 
                      size="small" 
                      label="Per Diem" 
                      type="number" 
                      value={currentResource.perDiemRate} 
                      onChange={e => {
                        const value = Math.max(0, Number(e.target.value));
                        updateCurrentResourceField('perDiemRate', value);
                      }} 
                      fullWidth 
                      InputProps={{ 
                        startAdornment: <InputAdornment position="start">$</InputAdornment>, 
                        endAdornment: <InputAdornment position="end">per day</InputAdornment> 
                      }} 
                    />
                  </Grid>
                </Grid>
              </Box>
            </Grid>

            {/* Button Grid */}
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
                <Grid container spacing={1} sx={{ width: '100%' }}>
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={() => {
                        setEditDialogOpen(true);
                        if (!selectedSheet && rateSheets.length > 0) {
                          setSelectedSheet(rateSheets[0].name);
                          setEditingSheet({ ...rateSheets[0] });
                          setIsNew(false);
                        }
                      }} 
                      startIcon={<EditIcon />} 
                      fullWidth
                    >
                      Edit Rate Sheets
                    </Button>
                  </Grid>
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={() => updateCurrentResourceField('manualOverrides', new Map())} 
                      disabled={currentResource.manualOverrides.size === 0} 
                      fullWidth
                    >
                      Reset All Overrides
                    </Button>
                  </Grid>
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={handleExportToExcel} 
                      fullWidth
                    >
                      Export to Excel
                    </Button>
                  </Grid>
                  <Grid item xs={4}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={handleSaveClick} 
                      startIcon={<SaveIcon />} 
                      fullWidth
                    >
                      Save
                    </Button>
                  </Grid>
                  <Grid item xs={4}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={handleCloneClick} 
                      startIcon={<FileCopyIcon />} 
                      fullWidth
                    >
                      Clone
                    </Button>
                  </Grid>
                  <Grid item xs={4}>
                    <Button 
                      size="small" 
                      variant="outlined" 
                      onClick={() => setSavedEstimatesDialogOpen(true)} 
                      startIcon={<FolderIcon />} 
                      fullWidth
                    >
                      Load File
                    </Button>
                  </Grid>
                  <Grid item xs={6}>
                    <Button
                      variant="outlined"
                      color="primary"
                      fullWidth
                      sx={{ mt: 1 }}
                      onClick={handleTrackClick}
                    >
                      Track
                    </Button>
                  </Grid>
                  <Grid item xs={6}>
                    <Button
                      variant="outlined"
                      color="error"
                      fullWidth
                      sx={{ mt: 1 }}
                      onClick={() => setResetAllDialogOpen(true)}
                    >
                      Reset All
                    </Button>
                  </Grid>
                </Grid>
              </Box>
            </Grid>
          </Grid>
        </Grid>

        {/* Right Column: Days Config, Project Info, Schedule, Results */}
        <Grid item xs={8}>
          <Grid container spacing={1} direction="column" sx={{ height: '100%' }}>
            {/* Top Row: Days Config + Project Info */}
            <Grid item>
              <Grid container spacing={1}>
                <Grid item xs={6}>
                  <Box border={1} borderRadius={1} p={1} sx={{ height: '100%', background: panelBg, borderColor: panelBorder, color: panelText }}>
                    <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Days Configuration</Typography>
                    <Grid container spacing={0.5} alignItems="center">
                      <Grid item xs={6}>
                        <TextField
                          size="small"
                          label="Total Days On Site"
                          type="number"
                          fullWidth
                          value={currentResource.daysOnSite}
                          onChange={handleDaysOnSiteChange}
                          inputProps={{ min: 0 }}
                        />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField 
                          size="small" 
                          label="Hours per Day" 
                          type="number" 
                          fullWidth 
                          value={currentResource.hoursPerDay} 
                          onChange={e => {
                            const value = Math.max(0, Number(e.target.value));
                            updateCurrentResourceField('hoursPerDay', value);
                          }} 
                          inputProps={{ min: 0 }} 
                        />
                      </Grid>
                      <Grid container spacing={1} alignItems="center">
                        <Grid item xs={6}>
                          <Typography variant="caption" sx={{ mb: 0.5 }}>Start Day On Site</Typography>
                          <Select
                            size="small"
                            fullWidth
                            value={currentResource.startDay}
                            onChange={e => updateCurrentResourceField('startDay', e.target.value)}
                            disabled={!!startDate}
                          >
                            {daysOfWeek.map(day => <MenuItem key={day} value={day}>{day}</MenuItem>)}
                          </Select>
                        </Grid>
                        <Grid item xs={6}>
                          <Typography variant="caption" sx={{ mb: 0.5 }}>Start Time On Site</Typography>
                          <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <TimePicker
                              value={startTimeOnSite}
                              onChange={setStartTimeOnSite}
                              minutesStep={30}
                              ampm
                              slotProps={{ textField: { size: 'small', fullWidth: true, label: '' } }}
                              format="hh:mm A"
                            />
                          </LocalizationProvider>
                        </Grid>
                      </Grid>
                      <Grid item xs={12}>
                        <FormControlLabel
                          control={
                            <Checkbox 
                              size="small" 
                              checked={currentResource.includeSaturdays} 
                              onChange={e => updateCurrentResourceField('includeSaturdays', e.target.checked)} 
                            />
                          }
                          label={<Typography variant="caption">Include Saturdays</Typography>}
                        />
                        <FormControlLabel
                          control={
                            <Checkbox 
                              size="small" 
                              checked={currentResource.includeSundays} 
                              onChange={e => updateCurrentResourceField('includeSundays', e.target.checked)} 
                            />
                          }
                          label={<Typography variant="caption">Include Sundays</Typography>}
                          sx={{ ml: 2 }}
                        />
                      </Grid>
                    </Grid>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box border={1} borderRadius={1} p={1} sx={{ height: '100%', background: panelBg, borderColor: panelBorder, color: panelText }}>
                    <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Project Info</Typography>
                    <Grid container spacing={0.5} alignItems="center">
                      <Grid item xs={6}>
                        <TextField 
                          size="small" 
                          label="Project Number" 
                          fullWidth 
                          value={projectNumber} 
                          onChange={e => setProjectNumber(e.target.value)} 
                        />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField 
                          size="small" 
                          label="Customer" 
                          fullWidth 
                          value={customer} 
                          onChange={e => setCustomer(e.target.value)} 
                        />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField 
                          size="small" 
                          label="Project Description" 
                          fullWidth 
                          value={projectDescription} 
                          onChange={e => setProjectDescription(e.target.value)} 
                        />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField 
                          size="small" 
                          label="Technician" 
                          fullWidth 
                          value={technician} 
                          onChange={e => {
                            setTechnician(e.target.value);
                            updateCurrentResourceField('technician', e.target.value);
                            
                            // Update the resource name if technician name is provided
                            if (e.target.value.trim()) {
                              const newResources = [...resources];
                              newResources[currentResourceIndex].name = e.target.value.trim();
                              setResources(newResources);
                            } else {
                              // If technician name is cleared, revert to default name
                              const newResources = [...resources];
                              newResources[currentResourceIndex].name = `Technician ${currentResourceIndex + 1}`;
                              setResources(newResources);
                            }
                          }} 
                        />
                      </Grid>
                      <LocalizationProvider dateAdapter={AdapterDayjs}>
                        <Grid item xs={6}>
                          <DatePicker
                            label="Start Date"
                            value={startDate}
                            onChange={handleStartDateChange}
                            slotProps={{ textField: { size: 'small', fullWidth: true, placeholder: 'Unspecified' } }}
                            format="YYYY-MM-DD"
                          />
                        </Grid>
                        
                        <Grid item xs={6}>
                          <DatePicker
                            label="End Date"
                            value={endDate}
                            onChange={handleEndDateChange}
                            slotProps={{ 
                              textField: { 
                                size: 'small', 
                                fullWidth: true, 
                                placeholder: 'Unspecified', 
                                error: !!startDate && !!endDate && endDate.isBefore(startDate, 'day'), 
                                helperText: !!startDate && !!endDate && endDate.isBefore(startDate, 'day') ? "End date can't be before start date" : '' 
                              } 
                            }}
                            format="YYYY-MM-DD"
                          />
                        </Grid>
                        <Grid item xs={4}>
                          <Button size="small" fullWidth variant="outlined" onClick={handleClearDates}>Clear Dates</Button>
                        </Grid>
                      </LocalizationProvider>
                    </Grid>
                  </Box>
                </Grid>
              </Grid>
            </Grid>

            {/* Schedule Section */}
            <Grid item>
              <Box sx={{ display: 'flex', width: '100%' }}>
                {/* Schedule Panel */}
                <Box border={1} borderRadius={1} p={1} sx={{ height: 420, width: 733, background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column' }}>
                  <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Schedule</Typography>
                  <Box sx={{ width: '100%', mx: 'auto', flex: 1, display: 'flex', flexDirection: 'column', height: 'calc(100% - 32px)' }}>
                    {/* Day-of-week header row */}
                    <Box sx={{ display: 'flex', width: '100%', height: 32, minHeight: 32, maxHeight: 32, mb: 0.5, position: 'relative' }}>
                      {daysOfWeek.map((day, idx) => (
                        <Box 
                          key={day} 
                          sx={{ 
                            width: scheduleBoxSize, 
                            height: 32, 
                            display: 'flex', 
                            alignItems: 'center', 
                            justifyContent: 'center', 
                            color: scheduleInactiveText, 
                            fontWeight: 600, 
                            borderRight: idx !== 6 ? `1px solid ${panelBorder}` : 0, 
                            borderBottom: `1px solid ${panelBorder}` 
                          }}
                        >
                          {day}
                        </Box>
                      ))}
                      {/* Month label at top right */}
                      {startDate && (
                        <Typography 
                          variant="subtitle2" 
                          sx={{ 
                            position: 'absolute', 
                            top: -32, 
                            right: 8, 
                            fontSize: '1.1rem', 
                            color: scheduleInactiveText, 
                            fontWeight: 600 
                          }}
                        >
                          {startDate.format('MMMM')}
                        </Typography>
                      )}
                    </Box>

                    {/* Schedule grid */}
                    <Box sx={{ flex: 1, overflowY: 'auto', width: '100%' }}>
                      <Grid container spacing={0} sx={{ margin: 0, width: '100%' }}>
                        {calendarWeeks.map((week, weekIdx) => (
                          <React.Fragment key={weekIdx}>
                            {week.map((day, dayIdx) => {
                              const isBlank = !day;
                              return (
                                <Grid 
                                  item 
                                  key={weekIdx + '-' + dayIdx} 
                                  sx={{ 
                                    padding: 0, 
                                    margin: 0, 
                                    minWidth: 0, 
                                    minHeight: 0, 
                                    width: scheduleBoxSize, 
                                    height: scheduleBoxSize, 
                                    display: 'flex', 
                                    justifyContent: 'center', 
                                    alignItems: 'center' 
                                  }}
                                >
                                  <Box
                                    border={0}
                                    onClick={() => day && handleDayClick(day)}
                                    sx={{
                                      width: scheduleBoxSize,
                                      height: scheduleBoxSize,
                                      minWidth: scheduleBoxSize,
                                      minHeight: scheduleBoxSize,
                                      maxWidth: scheduleBoxSize,
                                      maxHeight: scheduleBoxSize,
                                      display: 'flex',
                                      flexDirection: 'column',
                                      alignItems: 'center',
                                      justifyContent: 'center',
                                      bgcolor: isBlank
                                        ? scheduleInactive
                                        : (day.type === CalculationDayType.None
                                            ? '#23262b'
                                            : (day.type === 'TravelTo' || day.type === 'TravelFrom' ? travelDayBg :
                                              currentResource.manualOverrides.has(day.dayNumber) && currentResource.manualOverrides.get(day.dayNumber)?.dayType === DayType.Holdover ? holdoverDayBg :
                                              scheduleActive)),
                                      color: isBlank
                                        ? scheduleInactiveText
                                        : (day.type === CalculationDayType.None
                                            ? '#fff'
                                            : (day.type === 'TravelTo' || day.type === 'TravelFrom' ? '#fff' : scheduleActiveText)),
                                      borderRight: (dayIdx !== 6) ? `1px solid ${panelBorder}` : 0,
                                      borderBottom: (weekIdx !== calendarWeeks.length - 1) ? `1px solid ${panelBorder}` : 0,
                                      margin: 0,
                                      p: 0,
                                      cursor: day ? 'pointer' : 'default',
                                      position: 'relative',
                                      '&:hover': day ? {
                                        opacity: 0.8,
                                        transform: 'scale(1.02)',
                                        transition: 'all 0.2s ease'
                                      } : {}
                                    }}
                                  >
                                    {/* Day of month in top right */}
                                    {day && startDate && (
                                      <Typography 
                                        variant="caption" 
                                        sx={{ 
                                          position: 'absolute', 
                                          top: 2, 
                                          right: 4, 
                                          fontSize: '0.75rem', 
                                          color: '#fff', 
                                          opacity: 0.7 
                                        }}
                                      >
                                        {(() => {
                                          // Calculate the actual calendar date for this day
                                          let actualDate;
                                          if (currentResource.separateTravelTo) {
                                            if (day.dayNumber === 1) {
                                              // Day 1 is travel day - one day before start date
                                              actualDate = startDate.subtract(1, 'day');
                                            } else {
                                              // Day 2+ are work days - offset by (dayNumber - 2) from start date
                                              actualDate = startDate.add(day.dayNumber - 2, 'day');
                                            }
                                          } else {
                                            // No separate travel day - offset by (dayNumber - 1) from start date
                                            actualDate = startDate.add(day.dayNumber - 1, 'day');
                                          }
                                          return actualDate.date();
                                        })()}
                                      </Typography>
                                    )}
                                    {day ? (
                                      <>
                                        <Typography variant="caption" sx={{ textAlign: 'left', width: '100%', pl: 0.5 }}>
                                          Day {day.dayNumber}
                                        </Typography>
                                        {/* Show start time only for work days */}
                                        {day.type === 'WorkDay' && startTimeOnSite && (
                                          <Typography sx={{ mt: 0.2, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>
                                            {(() => {
                                              const override = currentResource.manualOverrides.get(day.dayNumber);
                                              if (override && override.startTime) {
                                                return dayjs(override.startTime, 'HH:mm').format('hh:mm A');
                                              }
                                              return startTimeOnSite ? startTimeOnSite.format('hh:mm A') : '';
                                            })()}
                                          </Typography>
                                        )}
                                        {/* Only show labour/travel if not a No Activity day */}
                                        {!(currentResource.manualOverrides.has(day.dayNumber) && currentResource.manualOverrides.get(day.dayNumber)?.dayType === DayType.Nil) && <>
                                          <Typography sx={{ mt: 0.5, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>
                                            Labour: {day.totalLabourHours?.toFixed(1) ?? '0.0'} hrs.
                                          </Typography>
                                          <Typography sx={{ mt: 0.2, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>
                                            Travel: {day.totalTravelHours?.toFixed(1) ?? '0.0'} hrs.
                                          </Typography>
                                        </>}
                                        {currentResource.manualOverrides.has(day.dayNumber) && currentResource.manualOverrides.get(day.dayNumber)?.dayType !== DayType.Nil && (
                                          <Box
                                            sx={{
                                              position: 'absolute',
                                              top: 2,
                                              right: 2,
                                              width: 8,
                                              height: 8,
                                              borderRadius: '50%',
                                              bgcolor: '#ff6b6b',
                                              border: '1px solid #fff'
                                            }}
                                          />
                                        )}
                                      </>
                                    ) : null}
                                  </Box>
                                </Grid>
                              );
                            })}
                          </React.Fragment>
                        ))}
                      </Grid>
                    </Box>
                  </Box>
                </Box>

                {/* Summary Panel */}
                <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column', justifyContent: 'flex-start', height: 420, flex: 1, ml: 1 }}>
                  <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Summary</Typography>
                  
                  {/* Current Resource Summary */}
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="caption" sx={{ color: panelText, mb: 1, display: 'block' }}>
                      {resources[currentResourceIndex]?.name || 'Current Resource'}
                    </Typography>
                    <TableContainer>
                      <Table size="small" sx={{ minWidth: 300 }}>
                        <TableHead>
                          <TableRow>
                            <TableCell align="center" sx={{ fontWeight: 600, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>Labour</TableCell>
                            <TableCell align="center" sx={{ fontWeight: 600, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>Travel</TableCell>
                            <TableCell align="center" sx={{ fontWeight: 600, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>Expenses</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          <TableRow>
                            <TableCell align="center">{totalLabourHours} hrs.</TableCell>
                            <TableCell align="center">{totalTravelHours} hrs.</TableCell>
                            <TableCell align="center">–</TableCell>
                          </TableRow>
                          <TableRow>
                            <TableCell align="center">${totalLabourCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                            <TableCell align="center">${totalTravelCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                            <TableCell align="center">${totalExpenses.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          </TableRow>
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Box>
                  
                  {/* Total Days and Note */}
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, position: 'relative' }}>
                    <Typography variant="h6" sx={{ fontWeight: 700, mr: 2 }}>
                      Total Days: <span style={{ fontSize: '2rem', fontWeight: 700 }}>{totalDays}</span>
                    </Typography>
                    <Box sx={{ flex: 1 }} />
                    <Typography variant="caption" sx={{ position: 'absolute', top: -10, right: 0, color: panelText }}>
                      (Cost + 10%, not incl. per Diem)
                    </Typography>
                  </Box>
                  
                  {/* Grand Total for All Resources and Current Resource */}
                  {resources.length > 1 ? (
                    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'row', alignItems: 'stretch', width: '100%', gap: 2 }}>
                      {/* Current Resource Total */}
                      <Box sx={{ flex: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#222' : '#222', mr: 1 }}>
                        <Typography variant="subtitle2" sx={{ color: '#fff', fontWeight: 600 }}>Current Resource Total</Typography>
                        <Typography variant="h6" sx={{ color: darkMode ? '#4fc3f7' : '#00bfff', fontWeight: 700 }}>
                          ${grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Typography>
                      </Box>
                      {/* All Resources Total */}
                      <Box sx={{ flex: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#1a1a1a' : '#f0f0f0', ml: 1 }}>
                        <Typography variant="subtitle1" sx={{ color: panelText, fontWeight: 600, mb: 1 }}>All Resources Total</Typography>
                        <Typography variant="h4" sx={{ color: darkMode ? '#4fc3f7' : '#1976d2', fontWeight: 700 }}>
                          ${resources.reduce((sum, resource) => {
                            // Use the project's rate sheet for all resources
      const projectRateSheet = rateSheets.find(s => s.name === selectedSheet) || rateSheets[0];
                                                          const resourceCustomSheet = {
                                ...projectRateSheet,
                              hotelCost: resource.hotelCost,
                              rentalCarRate: resource.rentalCarRate,
                              flightCost: resource.flightCost,
                              mileageRate: resource.mileageRate,
                              perDiemRate: resource.perDiemRate
                            };
                            const resourceCalcResult = calculateEstimate({
                              daysOnSite: resource.daysOnSite,
                              hoursPerDay: resource.hoursPerDay,
                              startDayOfWeek: daysOfWeek.indexOf(resource.startDay),
                              holdoverDayEnabled: resource.holdoverDayEnabled,
                              holdoverDayOfWeek: daysOfWeek.indexOf(resource.holdoverDayOfWeek),
                              separateTravelTo: resource.separateTravelTo,
                              separateTravelFrom: resource.separateTravelFrom,
                              travelMethod: resource.travelMethod,
                              travelDistance: resource.travelDistance,
                              travelTime: resource.travelTime,
                              dailyTravelDistance: resource.dailyTravelDistance,
                              dailyTravelTime: resource.dailyTravelTime,
                              rateSheet: resourceCustomSheet,
                              discountPercent: resource.discountPercent,
                              isEmergency: resource.isEmergency,
                              hotelRequired: resource.hotelRequired,
                              rentalCarRequired: resource.rentalCarRequired,
                              otherExpenses: resource.otherExpenses,
                              manualOverrides: resource.manualOverrides,
                              includeSaturdays: resource.includeSaturdays,
                              includeSundays: resource.includeSundays
                            });
                            const resourceTotal = resourceCalcResult.dayDetails.reduce((sum, d) =>
                              sum +
                                (d.labourCost ?? 0) +
                                (d.travelCost ?? 0) +
                                ((d.hotelCost ?? 0) * 1.1) +
                                ((d.mileageCost ?? 0) * 1.1) +
                                ((d.rentalCarCost ?? 0) * 1.1) +
                                ((d.airfareCost ?? 0) * 1.1) +
                                (d.perDiem ?? 0),
                              0
                            );
                            return sum + resourceTotal;
                          }, 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Typography>
                      </Box>
                    </Box>
                  ) : (
                    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                      <Box sx={{ flex: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 4, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#222' : '#222', minHeight: 120, justifyContent: 'center' }}>
                        <Typography variant="subtitle2" sx={{ color: '#fff', fontWeight: 600, mb: 1 }}>Grand Total</Typography>
                        <Typography variant="h4" sx={{ color: darkMode ? '#4fc3f7' : '#00bfff', fontWeight: 700 }}>
                          ${grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Typography>
                      </Box>
                    </Box>
                  )}
                </Box>
              </Box>
            </Grid>

            {/* Results Table */}
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText }}>
                <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Results</Typography>
                <TableContainer component={Paper} sx={{ background: panelBg }}>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Day</TableCell>
                        <TableCell>Labour</TableCell>
                        <TableCell>Travel</TableCell>
                        <TableCell>Mileage</TableCell>
                        <TableCell>Hotel</TableCell>
                        <TableCell>Rental</TableCell>
                        <TableCell>Flight</TableCell>
                        <TableCell>Per Diem</TableCell>
                        <TableCell>Total</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {filteredDayDetails.map((day, i) => (
                        <TableRow key={i}>
                          <TableCell>Day {day.dayNumber}</TableCell>
                          <TableCell sx={{ color: (day.labourCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.labourCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.travelCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.travelCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.mileageCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.mileageCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.hotelCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.hotelCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.rentalCarCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.rentalCarCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.airfareCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.airfareCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.perDiem ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.perDiem ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                          <TableCell sx={{ color: (day.totalDayCost ?? 0) === 0 ? '#888' : 'inherit' }}>
                            ${(day.totalDayCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Box>
            </Grid>
          </Grid>
        </Grid>
      </Grid>

      {/* Rate Sheet Editor Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Rate Sheet Setup
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined, minWidth: 700 }}>
          <Grid container spacing={2}>
            {/* Left: List of Rate Sheets */}
            <Grid item xs={4}>
              <Paper variant="outlined" sx={{ height: 340, overflowY: 'auto', bgcolor: darkMode ? '#23262b' : '#fafafa' }}>
                {rateSheets.map((sheet, idx) => (
                  <Box 
                    key={sheet.name} 
                    sx={{
                      px: 2, 
                      py: 1, 
                      cursor: 'pointer',
                      bgcolor: selectedSheet === sheet.name ? (darkMode ? '#1976d2' : '#e3f2fd') : 'inherit',
                      color: selectedSheet === sheet.name ? '#fff' : (darkMode ? '#fff' : '#000'),
                      borderBottom: '1px solid',
                      borderColor: darkMode ? '#444' : '#ddd',
                      fontWeight: selectedSheet === sheet.name ? 600 : 400
                    }}
                    onClick={() => {
                      setSelectedSheet(sheet.name);
                      setEditingSheet({ ...sheet });
                      setIsNew(false);
                    }}
                  >
                    {sheet.name}
                  </Box>
                ))}
              </Paper>
            </Grid>
            {/* Right: Details Panel */}
            <Grid item xs={8}>
              <Box component="form" autoComplete="off">
                <Grid container spacing={1}>
                  <Grid item xs={12}>
                    <TextField 
                      label="Name" 
                      value={editingSheet?.name || ''} 
                      onChange={e => handleEditField('name', e.target.value)} 
                      fullWidth 
                      autoFocus 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Regular Labour Rate" 
                      type="number" 
                      value={editingSheet?.regularLabourRate || ''} 
                      onChange={e => handleEditField('regularLabourRate', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Overtime Labour Rate" 
                      type="number" 
                      value={editingSheet?.overtimeLabourRate || ''} 
                      fullWidth 
                      InputProps={{ readOnly: true }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Premium Labour Rate" 
                      type="number" 
                      value={editingSheet?.premiumLabourRate || ''} 
                      fullWidth 
                      InputProps={{ readOnly: true }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Regular Travel Rate" 
                      type="number" 
                      value={editingSheet?.regularTravelRate || ''} 
                      onChange={e => handleEditField('regularTravelRate', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Overtime Travel Rate" 
                      type="number" 
                      value={editingSheet?.overtimeTravelRate || ''} 
                      fullWidth 
                      InputProps={{ readOnly: true }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Premium Travel Rate" 
                      type="number" 
                      value={editingSheet?.premiumTravelRate || ''} 
                      fullWidth 
                      InputProps={{ readOnly: true }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Hotel Cost" 
                      type="number" 
                      value={editingSheet?.hotelCost || ''} 
                      onChange={e => handleEditField('hotelCost', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Per Diem Rate" 
                      type="number" 
                      value={editingSheet?.perDiemRate || ''} 
                      onChange={e => handleEditField('perDiemRate', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Mileage Rate" 
                      type="number" 
                      value={editingSheet?.mileageRate || ''} 
                      onChange={e => handleEditField('mileageRate', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Rental Car Rate" 
                      type="number" 
                      value={editingSheet?.rentalCarRate || ''} 
                      onChange={e => handleEditField('rentalCarRate', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField 
                      label="Flight Cost" 
                      type="number" 
                      value={editingSheet?.flightCost || ''} 
                      onChange={e => handleEditField('flightCost', parseFloat(e.target.value))} 
                      fullWidth 
                      InputProps={{ inputProps: { step: 0.01 } }} 
                    />
                  </Grid>
                </Grid>
              </Box>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined, justifyContent: 'flex-start', px: 3 }}>
          <Button onClick={() => handleEdit()} color="primary" variant="outlined">New</Button>
          <Button onClick={handleSave} color="primary" variant="contained">Save</Button>
          <Button onClick={() => handleDelete(editingSheet?.name || selectedSheet)} color="error" variant="outlined" disabled={rateSheets.length <= 1}>Delete</Button>
          <Button onClick={() => setEditDialogOpen(false)} color="inherit" variant="outlined">Close</Button>
        </DialogActions>
      </Dialog>

      {/* Day Editor Dialog */}
      {editingDay && (
        <DayEditorDialog
          open={dayEditorOpen}
          onClose={() => {
            setDayEditorOpen(false);
            setEditingDay(null);
          }}
          onSave={handleDaySave}
          dayNumber={editingDay}
          currentOverride={currentResource.manualOverrides.get(editingDay) || null}
          defaultDayType={getDefaultDayType(filteredDayDetails.find(d => d.dayNumber === editingDay)!)}
          defaultLabourHours={(() => {
            const day = filteredDayDetails.find(d => d.dayNumber === editingDay);
            const override = currentResource.manualOverrides.get(editingDay);
            if ((override && override.dayType === DayType.Travel) || (!override && day && (day.type === 'TravelTo' || day.type === 'TravelFrom'))) {
              return 0;
            }
            return day?.totalLabourHours || 0;
          })()}
          defaultTravelHours={(() => {
            const day = filteredDayDetails.find(d => d.dayNumber === editingDay);
            const override = currentResource.manualOverrides.get(editingDay);
            if ((override && override.dayType === DayType.Travel) || (!override && day && (day.type === 'TravelTo' || day.type === 'TravelFrom'))) {
              return currentResource.travelTime;
            }
            return day?.totalTravelHours || 0;
          })()}
          defaultStartTime={(() => {
            const override = currentResource.manualOverrides.get(editingDay);
            if (override && override.startTime) {
              return dayjs(override.startTime, 'HH:mm');
            }
            return startTimeOnSite;
          })()}
        />
      )}

      {/* Save Estimate Dialog */}
      <Dialog open={saveDialogOpen} onClose={() => setSaveDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Save Estimate
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <TextField
            autoFocus
            margin="dense"
            label="Estimate Name"
            type="text"
            fullWidth
            variant="outlined"
            value={estimateName}
            onChange={(e) => setEstimateName(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                handleSaveConfirm();
              }
            }}
          />
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Button onClick={() => setSaveDialogOpen(false)} color="inherit" variant="outlined">
            Cancel
          </Button>
          <Button onClick={handleSaveConfirm} color="primary" variant="contained" disabled={!estimateName.trim()}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      {/* Clone Estimate Dialog */}
      <Dialog open={cloneDialogOpen} onClose={() => setCloneDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Clone Estimate
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <TextField
            autoFocus
            margin="dense"
            label="Estimate Name"
            type="text"
            fullWidth
            variant="outlined"
            value={estimateName}
            onChange={(e) => setEstimateName(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                handleCloneConfirm();
              }
            }}
          />
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Button onClick={() => setCloneDialogOpen(false)} color="inherit" variant="outlined">
            Cancel
          </Button>
          <Button onClick={handleCloneConfirm} color="primary" variant="contained" disabled={!estimateName.trim()}>
            Clone
          </Button>
        </DialogActions>
      </Dialog>

      {/* Load Estimate Dialog */}
      <Dialog open={savedEstimatesDialogOpen} onClose={() => setSavedEstimatesDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Load Saved Estimate
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined, minHeight: 200 }}>
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <Typography variant="body1" color="textSecondary" sx={{ mb: 3 }}>
              Select a saved estimate file (.est) to load:
            </Typography>
            <Button
              variant="contained"
              component="label"
              sx={{ mb: 2 }}
            >
              Choose File
              <input
                type="file"
                accept=".est"
                style={{ display: 'none' }}
                onChange={handleEstimateFileLoad}
              />
            </Button>
            <Typography variant="body2" color="textSecondary">
              Files should be saved estimate files (.est format)
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Button onClick={() => setSavedEstimatesDialogOpen(false)} color="inherit" variant="outlined">
            Cancel
          </Button>
        </DialogActions>
      </Dialog>

      {/* Add Resource Dialog */}
      <Dialog open={addResourceDialogOpen} onClose={() => setAddResourceDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Add New Resource
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                autoFocus
                label="Resource Name"
                value={newResourceName}
                onChange={(e) => setNewResourceName(e.target.value)}
                fullWidth
                variant="outlined"
              />
            </Grid>
            {resources.length > 1 && (
              <>
                <Grid item xs={12}>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    Copy settings from:
                  </Typography>
                  <Select
                    value={copyFromResourceIndex}
                    onChange={(e) => setCopyFromResourceIndex(Number(e.target.value))}
                    fullWidth
                  >
                    {resources.map((resource, index) => (
                      <MenuItem key={resource.id} value={index}>
                        {resource.name}
                      </MenuItem>
                    ))}
                  </Select>
                </Grid>
                <Grid item xs={12}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={copyOnlyRateSheet}
                        onChange={(e) => setCopyOnlyRateSheet(e.target.checked)}
                      />
                    }
                    label="Copy only rate sheet and start date (project info always copied)"
                  />
                </Grid>
              </>
            )}
          </Grid>
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Button onClick={() => setAddResourceDialogOpen(false)} color="inherit" variant="outlined">
            Cancel
          </Button>
          <Button 
            onClick={addResource} 
            color="primary" 
            variant="contained" 
            disabled={!newResourceName.trim()}
          >
            Add Resource
          </Button>
        </DialogActions>
      </Dialog>

      {/* Reset All Confirmation Dialog */}
      <Dialog open={resetAllDialogOpen} onClose={() => setResetAllDialogOpen(false)}>
        <DialogTitle>Reset All Settings</DialogTitle>
        <DialogContent>
          <Typography>Are you sure you want to reset all fields to their default values? This cannot be undone.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetAllDialogOpen(false)} color="primary" variant="outlined">Cancel</Button>
          <Button onClick={handleResetAll} color="error" variant="contained">Reset All</Button>
        </DialogActions>
      </Dialog>

      {/* Results Window */}
      {resultsWindowOpen && (
        <ResultsWindow
          open={resultsWindowOpen}
          onClose={() => setResultsWindowOpen(false)}
          resources={resources}
          rateSheets={rateSheets}
          projectData={{
            projectNumber,
            customer,
            projectDescription,
            technician: currentResource.name || currentResource.technician || technician
          }}
          startDate={startDate}
          endDate={endDate}
          darkMode={darkMode}
          trackingData={trackingData}
        />
      )}

      {/* Hidden file input for loading tracking data */}
      <input
        type="file"
        ref={fileInputRef}
                        accept=".est"
        style={{ display: 'none' }}
        onChange={handleFileLoad}
      />
    </Box>
  );
};

export default QuickEstimator;