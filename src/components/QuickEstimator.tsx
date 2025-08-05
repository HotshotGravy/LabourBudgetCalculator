import React, { useState, useEffect } from 'react';
import { Box, Grid, Typography, TextField, Select, MenuItem, Checkbox, FormControlLabel, Button, InputAdornment, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, List, ListItem, ListItemText, ListItemSecondaryAction, Tabs, Tab } from '@mui/material';
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
import dayjs, { Dayjs } from 'dayjs';
import { DatePicker, LocalizationProvider, TimePicker } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

const daysOfWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
const SCHEDULE_ROWS = 2;

interface QuickEstimatorProps {
  darkMode?: boolean;
}

interface ManualDayOverride {
  dayType: DayType;
  labourHours: number;
  travelHours: number;
  includeExpenses?: boolean;
  startTime?: string;
}

// New interfaces for multiple resources
interface ResourceData {
  id: string;
  name: string;
  // Form fields
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
  selectedSheet: string;
  manualOverrides: Map<number, ManualDayOverride>;
  otBefore7After5: boolean;
}

interface ProjectData {
  projectNumber: string;
  customer: string;
  projectDescription: string;
}

interface SavedEstimate {
  id: string;
  name: string;
  timestamp: number;
  projectData: ProjectData;
  resources: ResourceData[];
}

const QuickEstimator: React.FC<QuickEstimatorProps> = ({ darkMode }) => {
  // Calculate schedule box size based on available height (responsive)
  // For now, use fixed height for the schedule area for WinForms-like look
  const scheduleBoxSize = 99.225; // 90 * 1.05 * 1.05

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
  // Duplicate
  const handleDuplicate = (sheet: RateSheet) => {
    let copy = { ...sheet, name: sheet.name + ' Copy' };
    setRateSheets([...rateSheets, copy]);
    DataManager.saveRateSheets([...rateSheets, copy]);
  };
  // Change field in editor
  const handleEditField = (field: keyof RateSheet, value: any) => {
    if (!editingSheet) return;
    let updated = { ...editingSheet, [field]: value };
    if (field === 'regularLabourRate') {
      updated.overtimeLabourRate = value * 1.5;
      updated.premiumLabourRate = value * 2;
    }
    if (field === 'regularTravelRate') {
      updated.overtimeTravelRate = value * 1.5;
      updated.premiumTravelRate = value * 2;
    }
    setEditingSheet(updated);
  };

  // New states for the estimator
  const [daysOnSite, setDaysOnSite] = useState(1);
  const [hoursPerDay, setHoursPerDay] = useState(8);
  const [startDay, setStartDay] = useState('Monday');
  const [holdoverDayEnabled, setHoldoverDayEnabled] = useState(false);
  const [holdoverDayOfWeek, setHoldoverDayOfWeek] = useState('Sunday');
  const [separateTravelTo, setSeparateTravelTo] = useState(false);
  const [separateTravelFrom, setSeparateTravelFrom] = useState(false);
  const [travelMethod, setTravelMethod] = useState('Driving');
  const [travelDistance, setTravelDistance] = useState(0);
  const [travelTime, setTravelTime] = useState(0);
  const [dailyTravelDistance, setDailyTravelDistance] = useState(0);
  const [dailyTravelTime, setDailyTravelTime] = useState(0);
  const [discountPercent, setDiscountPercent] = useState(0);
  const [isEmergency, setIsEmergency] = useState(false);
  const [hotelRequired, setHotelRequired] = useState(true);
  const [rentalCarRequired, setRentalCarRequired] = useState(false);
  const [otherExpenses, setOtherExpenses] = useState(0);

  // Map day string to index (ensure 'Monday' = 1, ..., 'Sunday' = 0)
  const startDayOfWeek = daysOfWeek.indexOf(startDay);
  const holdoverDayIdx = daysOfWeek.indexOf(holdoverDayOfWeek);

  // Manual override state
  const [manualOverrides, setManualOverrides] = useState<Map<number, ManualDayOverride>>(new Map());
  const [dayEditorOpen, setDayEditorOpen] = useState(false);
  const [editingDay, setEditingDay] = useState<number | null>(null);

  // New states for the Expenses section
  const [hotelCost, setHotelCost] = useState(currentSheet?.hotelCost ?? 0);
  const [rentalCarRate, setRentalCarRate] = useState(currentSheet?.rentalCarRate ?? 0);
  const [flightCost, setFlightCost] = useState(currentSheet?.flightCost ?? 0);
  const [mileageRate, setMileageRate] = useState(currentSheet?.mileageRate ?? 0);
  const [perDiemRate, setPerDiemRate] = useState(currentSheet?.perDiemRate ?? 0);

  // Add state for including Saturdays and Sundays
  const [includeSaturdays, setIncludeSaturdays] = useState(true);
  const [includeSundays, setIncludeSundays] = useState(true);
  // Add state for OT before 7am and after 5pm
  const [otBefore7After5, setOtBefore7After5] = useState(false);

  // Add state for start time on site (default 8:00 am)
  const [startTimeOnSite, setStartTimeOnSite] = useState<Dayjs | null>(dayjs().hour(8).minute(0));

  // When selectedSheet or currentSheet changes, update these values
  useEffect(() => {
    setHotelCost(currentSheet?.hotelCost ?? 0);
    setRentalCarRate(currentSheet?.rentalCarRate ?? 0);
    setFlightCost(currentSheet?.flightCost ?? 0);
    setMileageRate(currentSheet?.mileageRate ?? 0);
    setPerDiemRate(currentSheet?.perDiemRate ?? 0);
  }, [currentSheet]);





  // Update the schedule creation logic to set excluded days as No Activity
  // Travel days should override weekend exclusion settings

  // Helper function to get discounted rate
  const getDiscountedRate = (base: number, premium: number) => {
    let rate = isEmergency ? premium : base;
    let discounted = rate * (1 - discountPercent / 100);
    return discountPercent > 0 ? `${rate.toFixed(2)} → ${discounted.toFixed(2)}` : rate.toFixed(2);
  };



  // Define colors for different day types
  const travelDayBg = darkMode ? '#afa436' : '#ffe066';
  const holdoverDayBg = darkMode ? '#2e7d32' : '#4caf50';

  // Calendar grid building logic will be moved after filteredDayDetails calculation

  // State for Reset All confirmation dialog
  const [resetAllDialogOpen, setResetAllDialogOpen] = useState(false);

  // Save/Clone functionality state
  const [savedEstimates, setSavedEstimates] = useState<SavedEstimate[]>([]);
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [cloneDialogOpen, setCloneDialogOpen] = useState(false);
  const [savedEstimatesDialogOpen, setSavedEstimatesDialogOpen] = useState(false);
  const [estimateName, setEstimateName] = useState('');
  const [editingEstimateId, setEditingEstimateId] = useState<string | null>(null);

  // Load saved estimates on component mount
  useEffect(() => {
    const saved = localStorage.getItem('savedEstimates');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        // Convert manualOverrides back to Map objects for each resource
        const estimatesWithMaps = parsed.map((est: any) => ({
          ...est,
          resources: est.resources ? est.resources.map((resource: any) => ({
            ...resource,
            manualOverrides: new Map(Object.entries(resource.manualOverrides || {}))
          })) : []
        }));
        setSavedEstimates(estimatesWithMaps);
      } catch (error) {
        console.error('Error loading saved estimates:', error);
      }
    }
  }, []);

  // Save estimates to localStorage whenever they change
  useEffect(() => {
    const estimatesToSave = savedEstimates.map(est => ({
      ...est,
      resources: est.resources.map(resource => ({
        ...resource,
        manualOverrides: Object.fromEntries(resource.manualOverrides)
      }))
    }));
    localStorage.setItem('savedEstimates', JSON.stringify(estimatesToSave));
  }, [savedEstimates]);

  // Handler to reset all fields to default values
  const handleResetAll = () => {
    setSelectedSheet(rateSheets[0]?.name || '');
    setDaysOnSite(1);
    setHoursPerDay(8);
    setStartDay('Monday');
    setHoldoverDayEnabled(false);
    setHoldoverDayOfWeek('Sunday');
    setSeparateTravelTo(false);
    setSeparateTravelFrom(false);
    setTravelMethod('Driving');
    setTravelDistance(0);
    setTravelTime(0);
    setDailyTravelDistance(0);
    setDailyTravelTime(0);
    setDiscountPercent(0);
    setIsEmergency(false);
    setHotelRequired(true);
    setRentalCarRequired(false);
    setOtherExpenses(0);
    setHotelCost(rateSheets[0]?.hotelCost ?? 0);
    setRentalCarRate(rateSheets[0]?.rentalCarRate ?? 0);
    setFlightCost(rateSheets[0]?.flightCost ?? 0);
    setMileageRate(rateSheets[0]?.mileageRate ?? 0);
    setPerDiemRate(rateSheets[0]?.perDiemRate ?? 0);
    setManualOverrides(new Map());
    setProjectNumber('');
    setCustomer('');
    setProjectDescription('');
    setTechnician('');
    setStartDate(null);
    setEndDate(null);
    setIncludeSaturdays(true);
    setIncludeSundays(true);
    setOtBefore7After5(false);
    setResetAllDialogOpen(false);
  };

  // Helper functions for rounding and linking
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

  // Helper: save current estimate state
  const saveCurrentEstimate = (name: string, isClone: boolean = false) => {
    const projectData: ProjectData = {
      projectNumber,
      customer,
      projectDescription
    };

    const newEstimate: SavedEstimate = {
      id: Date.now().toString(),
      name,
      timestamp: Date.now(),
      projectData,
      resources: resources.map(resource => ({
        ...resource,
        manualOverrides: new Map(resource.manualOverrides)
      }))
    };

    setSavedEstimates(prev => [...prev, newEstimate]);
    return newEstimate;
  };

  // Helper: load estimate data
  const loadEstimateData = (estimate: SavedEstimate) => {
    // Load project data
    setProjectNumber(estimate.projectData.projectNumber);
    setCustomer(estimate.projectData.customer);
    setProjectDescription(estimate.projectData.projectDescription);
    
    // Load resources
    setResources(estimate.resources.map(resource => ({
      ...resource,
      manualOverrides: new Map(resource.manualOverrides)
    })));
    setCurrentResourceIndex(0);
  };

  // Helper: delete saved estimate
  const deleteSavedEstimate = (id: string) => {
    setSavedEstimates(prev => prev.filter(est => est.id !== id));
  };

  // Add helper functions for rounding and linking
  // Linked state for daily driving distance/time
  const handleDailyTravelDistanceChange = (val: number) => {
    // Round to nearest 15 miles, min 15
    const roundedMiles = Math.max(15, roundToNearest(val, 15));
    setDailyTravelDistance(roundedMiles);
    // 1 mile = 1 minute, so time in hours = miles / 60
    const minutes = roundedMiles;
    const hours = Math.max(0.25, roundToNearest(minutes / 60, 0.25));
    setDailyTravelTime(hours);
  };
  const handleDailyTravelTimeChange = (val: number) => {
    // Round to nearest 0.25 hours (15 min), min 0.25
    const roundedHours = Math.max(0.25, roundToNearest(val, 0.25));
    setDailyTravelTime(roundedHours);
    // 1 minute = 1 mile, so miles = hours * 60
    const miles = Math.max(15, roundToNearest(roundedHours * 60, 15));
    setDailyTravelDistance(miles);
  };

  // Add new state for startDate and endDate
  const [startDate, setStartDate] = useState<Dayjs | null>(null);
  const [endDate, setEndDate] = useState<Dayjs | null>(null);

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
      dailyTravelDistance: 0,
      dailyTravelTime: 0,
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
      selectedSheet: rateSheets[0]?.name || '',
      manualOverrides: new Map(),
      otBefore7After5: false,
    }
  ]);
  
  const [currentResourceIndex, setCurrentResourceIndex] = useState(0);
  const [addResourceDialogOpen, setAddResourceDialogOpen] = useState(false);
  const [newResourceName, setNewResourceName] = useState('');
  const [copyFromResourceIndex, setCopyFromResourceIndex] = useState(0);
  const [copyOnlyRateSheet, setCopyOnlyRateSheet] = useState(false);

  // Helper functions for resources
  const addResource = () => {
    const newResource: ResourceData = {
      id: Date.now().toString(),
      name: newResourceName,
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
      dailyTravelDistance: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].dailyTravelDistance,
      dailyTravelTime: copyOnlyRateSheet ? 0 : resources[copyFromResourceIndex].dailyTravelTime,
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
      selectedSheet: copyOnlyRateSheet ? selectedSheet : resources[copyFromResourceIndex].selectedSheet,
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

  const renameResource = (index: number, newName: string) => {
    const newResources = [...resources];
    newResources[index].name = newName;
    setResources(newResources);
  };

  // Sync form fields with current resource
  useEffect(() => {
    const currentResource = resources[currentResourceIndex];
    if (currentResource) {
      setDaysOnSite(currentResource.daysOnSite);
      setHoursPerDay(currentResource.hoursPerDay);
      setStartDay(currentResource.startDay);
      setHoldoverDayEnabled(currentResource.holdoverDayEnabled);
      setHoldoverDayOfWeek(currentResource.holdoverDayOfWeek);
      setSeparateTravelTo(currentResource.separateTravelTo);
      setSeparateTravelFrom(currentResource.separateTravelFrom);
      setTravelMethod(currentResource.travelMethod);
      setTravelDistance(currentResource.travelDistance);
      setTravelTime(currentResource.travelTime);
      setDailyTravelDistance(currentResource.dailyTravelDistance);
      setDailyTravelTime(currentResource.dailyTravelTime);
      setDiscountPercent(currentResource.discountPercent);
      setIsEmergency(currentResource.isEmergency);
      setHotelRequired(currentResource.hotelRequired);
      setRentalCarRequired(currentResource.rentalCarRequired);
      setOtherExpenses(currentResource.otherExpenses);
      setHotelCost(currentResource.hotelCost);
      setRentalCarRate(currentResource.rentalCarRate);
      setFlightCost(currentResource.flightCost);
      setMileageRate(currentResource.mileageRate);
      setPerDiemRate(currentResource.perDiemRate);
      setIncludeSaturdays(currentResource.includeSaturdays);
      setIncludeSundays(currentResource.includeSundays);
      setOtBefore7After5(currentResource.otBefore7After5);
      setTechnician(currentResource.technician);
      setStartDate(currentResource.startDate ? dayjs(currentResource.startDate) : null);
      setEndDate(currentResource.endDate ? dayjs(currentResource.endDate) : null);
      setSelectedSheet(currentResource.selectedSheet);
      setManualOverrides(new Map(currentResource.manualOverrides));
    }
  }, [currentResourceIndex, resources]);

  // Calculate
  const currentResource = resources[currentResourceIndex];
  const customSheet = {
    ...currentSheet,
    hotelCost,
    rentalCarRate,
    flightCost,
    mileageRate,
    perDiemRate
  };
  const calcResult = calculateEstimate({
    daysOnSite,
    hoursPerDay,
    startDayOfWeek,
    holdoverDayEnabled,
    holdoverDayOfWeek: holdoverDayIdx,
    separateTravelTo,
    separateTravelFrom,
    travelMethod,
    travelDistance,
    travelTime,
    dailyTravelDistance,
    dailyTravelTime,
    rateSheet: customSheet,
    discountPercent,
    isEmergency,
    hotelRequired,
    rentalCarRequired,
    otherExpenses,
    manualOverrides,
    includeSaturdays,
    includeSundays,
    otBefore7After5,
    startTimeOnSite
  });
  const filteredDayDetails = calcResult.dayDetails;

  // Debug: log dayOfWeek for each scheduled day
  console.log('Scheduled days:', filteredDayDetails.map(d => ({ dayNumber: d.dayNumber, dayOfWeek: d.dayOfWeek, label: daysOfWeek[d.dayOfWeek] })));

  // Build a true calendar grid: anchor the first scheduled day to its correct weekday column
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
  const weeksToShow = Math.max(calendarWeeks.length, 1);

  // Functions to update current resource
  const updateCurrentResource = (updates: Partial<ResourceData>) => {
    const newResources = [...resources];
    newResources[currentResourceIndex] = { ...newResources[currentResourceIndex], ...updates };
    setResources(newResources);
  };

  const updateCurrentResourceField = (field: keyof ResourceData, value: any) => {
    updateCurrentResource({ [field]: value });
  };

  // --- Date logic ---
  useEffect(() => {
    // If both dates are set, update daysOnSite
    if (startDate && endDate) {
      const diff = endDate.diff(startDate, 'day') + 1;
      if (diff > 0 && daysOnSite !== diff) {
        setDaysOnSite(diff);
        updateCurrentResourceField('daysOnSite', diff);
      }
    }
  }, [startDate, endDate]);

  useEffect(() => {
    // If startDate and daysOnSite are set, update endDate
    if (startDate && daysOnSite && (!endDate || !endDate.isSame(startDate.add(daysOnSite - 1, 'day'), 'day'))) {
      setEndDate(startDate.add(daysOnSite - 1, 'day'));
    }
  }, [startDate, daysOnSite]);

  useEffect(() => {
    // If endDate and daysOnSite are set, but no startDate, update startDate
    if (endDate && daysOnSite && !startDate) {
      setStartDate(endDate.subtract(daysOnSite - 1, 'day'));
    }
  }, [endDate, daysOnSite]);

  useEffect(() => {
    // If endDate is before startDate, clear endDate
    if (startDate && endDate && endDate.isBefore(startDate, 'day')) {
      setEndDate(null);
    }
  }, [startDate, endDate]);

  // When startDate is set, update startDay and disable dropdown
  useEffect(() => {
    if (startDate) {
      setStartDay(daysOfWeek[startDate.day()]);
    }
  }, [startDate]);

  // --- Handlers ---
  const handleStartDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value ? dayjs(e.target.value) : null;
    setStartDate(value);
    updateCurrentResourceField('startDate', value?.format('YYYY-MM-DD') || null);
    if (!value) setStartDay('Monday'); // or your default
  };
  const handleEndDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value ? dayjs(e.target.value) : null;
    setEndDate(value);
    updateCurrentResourceField('endDate', value?.format('YYYY-MM-DD') || null);
  };
  const handleDaysOnSiteChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = Math.max(1, Number(e.target.value));
    setDaysOnSite(value);
    updateCurrentResourceField('daysOnSite', value);
  };
  const handleClearStartDate = () => {
    setStartDate(null);
    setStartDay('Monday'); // or your default
    updateCurrentResourceField('startDate', null);
  };
  const handleClearEndDate = () => {
    setEndDate(null);
    updateCurrentResourceField('endDate', null);
  };

  // Save/Clone handlers
  const handleSaveClick = () => {
    setEstimateName(generateDefaultEstimateName());
    setEditingEstimateId(null);
    setSaveDialogOpen(true);
  };

  const handleCloneClick = () => {
    const defaultName = generateDefaultEstimateName();
    setEstimateName(defaultName ? `Copy of ${defaultName}` : 'Copy of Estimate');
    setEditingEstimateId(null);
    setCloneDialogOpen(true);
  };

  const handleSaveConfirm = () => {
    if (estimateName.trim()) {
      saveCurrentEstimate(estimateName.trim());
      setSaveDialogOpen(false);
      setEstimateName('');
    }
  };

  const handleCloneConfirm = () => {
    if (estimateName.trim()) {
      saveCurrentEstimate(estimateName.trim(), true);
      setCloneDialogOpen(false);
      setEstimateName('');
    }
  };

  const handleLoadEstimate = (estimate: SavedEstimate) => {
    loadEstimateData(estimate);
    setSavedEstimatesDialogOpen(false);
  };

  // --- EXCEL EXPORT ---
  const handleExportToExcel = async () => {
    // Prompt for customer if not specified
    let customerToUse = customer.trim();
    if (!customerToUse) {
      const customerPrompt = prompt('Please enter the customer name:');
      if (!customerPrompt || customerPrompt.trim() === '') {
        alert('Customer name is required for export.');
        return;
      }
      customerToUse = customerPrompt.trim();
    }
    // Build the summary sheet row by row, matching the user's requirements
    const summaryRows = [];
    // 1. Header
    summaryRows.push(['Time and Expense Estimate', '', '']); // 1
    // 2-5: Project info
    summaryRows.push(['Customer:', customer, '']); // 2
    summaryRows.push(['Description:', projectDescription, '']); // 3
    summaryRows.push(['Project Number:', projectNumber, '']); // 4
    summaryRows.push(['Technician:', technician, '']); // 5
    // 6: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 6
    // 7-9: Date info
    summaryRows.push(['Start Date On Site:', startDate ? startDate.format('YYYY-MM-DD') : '', '']); // 7
    summaryRows.push(['End Site Date:', endDate ? endDate.format('YYYY-MM-DD') : '', '']); // 8
    summaryRows.push(['Total Days:', totalDays.toString(), '']); // 9
    // 10-11: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 10
    summaryRows.push(['', '', '']); // 11
    // 12-15: Rates/options (label in A, value in B)
    summaryRows.push(['Rates:', currentSheet?.name || '', '']); // 12
    summaryRows.push(['Separate Travel Days:', (separateTravelTo && separateTravelFrom) ? 'Yes - To and From' : separateTravelTo ? 'Yes - To' : separateTravelFrom ? 'Yes - From' : 'No', '']); // 13
    summaryRows.push(['Travel Method to Site Area:', travelMethod, '']); // 14
    summaryRows.push(['Emergency Rates:', isEmergency ? 'Yes' : 'No', '']); // 15
    // 16-17: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 16
    summaryRows.push(['', '', '']); // 17
    // 18: Summary table header
    summaryRows.push(['', 'Hours', 'Cost']); // 18
    // 19-21: Summary table
    summaryRows.push(['Labour:', totalLabourHours.toString(), totalLabourCost.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 19
    summaryRows.push(['Travel:', totalTravelHours.toString(), totalTravelCost.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 20
    summaryRows.push(['Expenses (Cost + 10%, not incl. per diem):', 'N/A', totalExpenses.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 21
    // 22: merged, empty, very light gray
    summaryRows.push(['', '', '']); // 22
    // 23: Grand total
    summaryRows.push(['Grand Total:', (totalLabourHours + totalTravelHours).toString(), grandTotal.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 23

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
      { header: '', width: 20 }  // Total Day Cost (column S)
    ];
    // Title row
    wsDetail.mergeCells(1, 1, 1, 19);
    wsDetail.getCell('A1').value = 'Daily Breakdown';
    wsDetail.getCell('A1').font = { bold: true, size: 16 };
    wsDetail.getCell('A1').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.getCell('A1').fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFD9E1F2' } };
    wsDetail.getRow(1).height = 26;
    // Multi-level header rows
    // First header row: group headers
    wsDetail.mergeCells('A2:C2'); wsDetail.getCell('A2').value = '';
    wsDetail.mergeCells('D2:G2'); wsDetail.getCell('D2').value = 'Labour Hours';
    wsDetail.mergeCells('H2:K2'); wsDetail.getCell('H2').value = 'Travel Hours';
    wsDetail.getCell('L2').value = 'Daily Cost';
    wsDetail.mergeCells('L2:M2');
    wsDetail.getCell('L2').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.getCell('N2').value = 'Expenses (Cost +10%, not including per diem)';
    wsDetail.mergeCells('N2:R2');
    wsDetail.getCell('N2').alignment = { horizontal: 'center', vertical: 'middle' };
    // --- ENSURE S2:S3 MERGE IS LAST ---
    wsDetail.mergeCells('S2:S3');
    wsDetail.getCell('S2').value = 'Total Day Cost';
    wsDetail.getCell('S2').alignment = { horizontal: 'center', vertical: 'middle' };
    // Second header row: subheaders
    const headerRow2 = wsDetail.getRow(3);
    headerRow2.values = [
      'Date', 'Day', 'Type',
      'Regular', 'Overtime', 'Premium', 'Total',
      'Regular', 'Overtime', 'Premium', 'Total',
      'Labour', 'Travel', 'Hotel', 'Per Diem', 'Mileage', 'Rental Car', 'Airfare', ''
    ];
    // Style both header rows
    for (let c = 1; c <= 19; c++) {
      wsDetail.getCell(2, c).font = { bold: true, size: 12 };
      wsDetail.getCell(2, c).alignment = { horizontal: 'center', vertical: 'middle' };
      wsDetail.getCell(2, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
      wsDetail.getCell(3, c).font = { bold: true, size: 12 };
      wsDetail.getCell(3, c).alignment = { horizontal: 'center', vertical: 'middle' };
      wsDetail.getCell(3, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    }
    wsDetail.getRow(2).height = 22;
    wsDetail.getRow(3).height = 22;
    // Data rows
    const dayRows = filteredDayDetails.map(day => [
      startDate ? dayjs(startDate).add(day.dayNumber - 1, 'day').format('YYYY-MM-DD') : '',
      daysOfWeek[day.dayOfWeek],
      friendlyType(day, manualOverrides),
      day.regularLabourHours,
      day.overtimeLabourHours,
      day.premiumLabourHours,
      day.totalLabourHours,
      day.regularTravelHours,
      day.overtimeTravelHours,
      day.premiumTravelHours,
      day.totalTravelHours,
      day.labourCost,
      day.travelCost,
      (day.hotelCost ?? 0) * 1.1, // Marked up
      day.perDiem, // No markup
      (day.mileageCost ?? 0) * 1.1, // Marked up
      (day.rentalCarCost ?? 0) * 1.1, // Marked up
      (day.airfareCost ?? 0) * 1.1, // Marked up
      // Total day cost: sum of all above (labourCost + travelCost + marked-up expenses + per diem)
      (day.labourCost ?? 0) + (day.travelCost ?? 0) + ((day.hotelCost ?? 0) * 1.1) + ((day.mileageCost ?? 0) * 1.1) + ((day.rentalCarCost ?? 0) * 1.1) + ((day.airfareCost ?? 0) * 1.1) + (day.perDiem ?? 0)
    ]);
    wsDetail.addRows(dayRows);
    // Totals row
    const totalsRow = [
      'TOTAL', '', '',
      ...[3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18].map(idx => {
        // Sum each numeric column (skip text columns)
        if (idx >= 3) {
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
    for (let c = 4; c <= 19; c++) {
      wsDetail.getCell(totalsRowIdx, c).font = { bold: true, color: { argb: 'FF000000' } };
      wsDetail.getCell(totalsRowIdx, c).numFmt = c >= 12 ? '$#,##0.00' : '0.00';
      wsDetail.getCell(totalsRowIdx, c).alignment = { horizontal: 'right', vertical: 'middle' };
      wsDetail.getCell(totalsRowIdx, c).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFEFFFEF' } };
    }
    wsDetail.getCell(totalsRowIdx, 1).font = { bold: true };
    wsDetail.getRow(totalsRowIdx).height = 22;
    // Format data rows
    for (let r = 4; r < 4 + dayRows.length; r++) {
      const row = wsDetail.getRow(r);
      // Alternating fill
      if (r % 2 === 1) {
        row.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF8F8F8' } };
      }
      // Align text/number columns
      for (let c = 1; c <= 19; c++) {
        let cell = row.getCell(c);
        if ([1,2,3].includes(c)) cell.alignment = { horizontal: 'left', vertical: 'middle' };
        else cell.alignment = { horizontal: 'right', vertical: 'middle' };
        // Currency formatting for cost columns
        if ([12,13,14,15,16,17,18,19].includes(c)) cell.numFmt = '$#,##0.00';
        // Highlight cost columns
        if ([12,13,14,15,16,17,18,19].includes(c)) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFFCC' } };
        // Zero value cells in light grey
        const v = cell.value;
        if (v === 0 || v === '0' || v === 0.0 || v === '0.00' || v === '$0.00') {
          cell.font = { ...cell.font, color: { argb: 'FFB0B0B0' } };
        }
      }
    }
    // Borders for all cells
    for (let r = 1; r <= 3 + dayRows.length + 1; r++) {
      for (let c = 1; c <= 19; c++) {
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

  

  // Calculations for summary and totals using filteredDayDetails
  const totalLabourHours = filteredDayDetails.reduce((sum, d) => sum + (d.totalLabourHours ?? 0), 0);
  const totalTravelHours = filteredDayDetails.reduce((sum, d) => sum + (d.totalTravelHours ?? 0), 0);
  const totalLabourCost = filteredDayDetails.reduce((sum, d) => sum + (d.labourCost ?? 0), 0);
  const totalTravelCost = filteredDayDetails.reduce((sum, d) => sum + (d.travelCost ?? 0), 0);
  // Apply 10% markup to hotel, mileage, rental car, airfare; per diem is not marked up
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
  const daysWithLabour = filteredDayDetails.filter(d => d.totalLabourHours > 0).length;
  const holdoverDays = filteredDayDetails.filter(d => d.isHoldover).length;
  const totalDaysOnSite = daysWithLabour - holdoverDays;
  const totalDays = filteredDayDetails.length;

  // Note: Removed the useEffect that was causing feedback loops when updating daysOnSite

  // Day editor handlers
  const handleDayClick = (day: typeof filteredDayDetails[number]) => {
    setEditingDay(day.dayNumber);
    setDayEditorOpen(true);
  };

  const handleDaySave = (override: ManualDayOverride | null) => {
    if (editingDay) {
      const newOverrides = new Map(manualOverrides);
      if (override) {
        newOverrides.set(editingDay, override);
      } else {
        newOverrides.delete(editingDay);
      }
      setManualOverrides(newOverrides);
      updateCurrentResourceField('manualOverrides', newOverrides);
    }
  };

  const getDefaultDayType = (day: typeof filteredDayDetails[number]): DayType => {
    if (day.type === 'TravelTo' || day.type === 'TravelFrom') return DayType.Travel;
    if (day.type === 'None') return DayType.Nil;
    return DayType.Work; // Default for WorkDay
  };

  return (
    <Box p={1} sx={{ background: darkMode ? '#23262b' : '#fff', minHeight: '100vh', color: panelText, boxSizing: 'border-box' }}>
      <Typography variant="h6" sx={{ mb: 0.5, color: panelText }}>Time & Expense Calculator</Typography>
      
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
                    <Typography variant="body2">{resource.name}</Typography>
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
                {/* Rates content */}
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
                  <Grid item xs={6}><TextField size="small" label="Discount" type="number" fullWidth value={discountPercent} onChange={e => {
                    const value = Math.max(0, Number(e.target.value));
                    setDiscountPercent(value);
                    updateCurrentResourceField('discountPercent', value);
                  }} InputProps={{ endAdornment: <InputAdornment position="end">%</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={isEmergency} onChange={e => {
                    setIsEmergency(e.target.checked);
                    updateCurrentResourceField('isEmergency', e.target.checked);
                  }} />} label={<Typography variant="caption">Emergency</Typography>} /></Grid>
                  <Grid item xs={6}>
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
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText }}>
                {/* Travel Options content */}
                <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Travel Options</Typography>
                <Grid container spacing={1.5} alignItems="center">
                  <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={separateTravelTo} onChange={e => {
                    setSeparateTravelTo(e.target.checked);
                    updateCurrentResourceField('separateTravelTo', e.target.checked);
                  }} />} label={<Typography variant="caption">Separate Travel Day To</Typography>} /></Grid>
                  <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={separateTravelFrom} onChange={e => {
                    setSeparateTravelFrom(e.target.checked);
                    updateCurrentResourceField('separateTravelFrom', e.target.checked);
                  }} />} label={<Typography variant="caption">Separate Travel Day From</Typography>} /></Grid>
                  <Grid item xs={12}>
                    <TextField
                      select
                      size="small"
                      fullWidth
                      label="Travel Method to Site Area"
                      value={travelMethod}
                      onChange={e => {
                        setTravelMethod(e.target.value);
                        updateCurrentResourceField('travelMethod', e.target.value);
                      }}
                    >
                      <MenuItem value="Driving">Driving</MenuItem>
                      <MenuItem value="Flight">Flight</MenuItem>
                    </TextField>
                  </Grid>
                  <Grid item xs={12}>
                    <TextField size="small" label="Driving Distance (First and Last Days Only)" type="number" fullWidth value={travelDistance} onChange={e => {
                      const value = Math.max(0, Number(e.target.value));
                      setTravelDistance(value);
                      updateCurrentResourceField('travelDistance', value);
                    }} InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField size="small" label="Total Travel Time to Site Area (Including Flight)" type="number" fullWidth value={travelTime}
                      onChange={e => {
                        const value = Math.max(0.25, roundToNearest(Number(e.target.value), 0.25));
                        setTravelTime(value);
                        updateCurrentResourceField('travelTime', value);
                      }}
                      inputProps={{ min: 0.25, step: 0.25 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                      onBlur={e => {
                        const value = Math.max(0.25, roundToNearest(Number((e.target as HTMLInputElement).value), 0.25));
                        setTravelTime(value);
                        updateCurrentResourceField('travelTime', value);
                      }}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); 
                        const value = Math.max(0.25, roundToNearest(Number((e.target as HTMLInputElement).value), 0.25));
                        setTravelTime(value);
                        updateCurrentResourceField('travelTime', value);
                      } }}
                      onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField size="small" label="Daily Driving Distance (One Way)" type="number" fullWidth value={dailyTravelDistance} 
                      onChange={e => {
                        handleDailyTravelDistanceChange(Number(e.target.value));
                        updateCurrentResourceField('dailyTravelDistance', dailyTravelDistance);
                      }}
                      inputProps={{ min: 15, step: 15 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }}
                      onBlur={e => {
                        handleDailyTravelDistanceChange(Number((e.target as HTMLInputElement).value));
                        updateCurrentResourceField('dailyTravelDistance', dailyTravelDistance);
                      }}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); 
                        handleDailyTravelDistanceChange(Number((e.target as HTMLInputElement).value));
                        updateCurrentResourceField('dailyTravelDistance', dailyTravelDistance);
                      } }}
                      onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField size="small" label="Daily Travel Time (One Way)" type="number" fullWidth value={dailyTravelTime} 
                      onChange={e => {
                        handleDailyTravelTimeChange(Number(e.target.value));
                        updateCurrentResourceField('dailyTravelTime', dailyTravelTime);
                      }}
                      inputProps={{ min: 0.25, step: 0.25 }}
                      InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                      onBlur={e => {
                        handleDailyTravelTimeChange(Number((e.target as HTMLInputElement).value));
                        updateCurrentResourceField('dailyTravelTime', dailyTravelTime);
                      }}
                      onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); 
                        handleDailyTravelTimeChange(Number((e.target as HTMLInputElement).value));
                        updateCurrentResourceField('dailyTravelTime', dailyTravelTime);
                      } }}
                      onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                    />
                  </Grid>
                </Grid>
              </Box>
            </Grid>
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ minHeight: 207, background: panelBg, borderColor: panelBorder, color: panelText }}>
                {/* Expenses content */}
                <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Expenses</Typography>
                <Grid container spacing={1.5} alignItems="center">
                  <Grid item xs={6} display="flex" alignItems="center">
                    <FormControlLabel control={<Checkbox size="small" checked={hotelRequired} onChange={e => {
                      setHotelRequired(e.target.checked);
                      updateCurrentResourceField('hotelRequired', e.target.checked);
                    }} />} label={<Typography variant="caption">Hotel Required</Typography>} sx={{ mr: 1 }} />
                    <TextField size="small" label="Hotel" type="number" value={hotelCost} onChange={e => {
                      const value = Math.max(0, Number(e.target.value));
                      setHotelCost(value);
                      updateCurrentResourceField('hotelCost', value);
                    }} fullWidth disabled={!hotelRequired} InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per night</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={6} display="flex" alignItems="center">
                    <FormControlLabel control={<Checkbox size="small" checked={rentalCarRequired} onChange={e => {
                      setRentalCarRequired(e.target.checked);
                      updateCurrentResourceField('rentalCarRequired', e.target.checked);
                    }} />} label={<Typography variant="caption">Rental Car Required</Typography>} sx={{ mr: 1 }} />
                    <TextField size="small" label="Rental Car" type="number" value={rentalCarRate} onChange={e => {
                      const value = Math.max(0, Number(e.target.value));
                      setRentalCarRate(value);
                      updateCurrentResourceField('rentalCarRate', value);
                    }} fullWidth disabled={!rentalCarRequired} InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per day</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={12}><TextField size="small" label="Flight Cost (One Way)" type="number" value={flightCost} onChange={e => {
                    const value = Math.max(0, Number(e.target.value));
                    setFlightCost(value);
                    updateCurrentResourceField('flightCost', value);
                  }} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={12}><TextField size="small" label="Mileage" type="number" value={mileageRate} onChange={e => {
                    const value = Math.max(0, Number(e.target.value));
                    setMileageRate(value);
                    updateCurrentResourceField('mileageRate', value);
                  }} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per mile/km</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={12}><TextField size="small" label="Per Diem" type="number" value={perDiemRate} onChange={e => {
                    const value = Math.max(0, Number(e.target.value));
                    setPerDiemRate(value);
                    updateCurrentResourceField('perDiemRate', value);
                  }} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per day</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                </Grid>
              </Box>
            </Grid>
            <Grid item>
              <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
                <Grid container spacing={1} sx={{ width: '100%' }}>
                  {/* First row */}
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button size="small" variant="outlined" onClick={() => {
                      setEditDialogOpen(true);
                      if (!selectedSheet && rateSheets.length > 0) {
                        setSelectedSheet(rateSheets[0].name);
                        setEditingSheet({ ...rateSheets[0] });
                        setIsNew(false);
                      }
                    }} startIcon={<EditIcon />} fullWidth>
                      Edit Rate Sheets
                    </Button>
                  </Grid>
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button size="small" variant="outlined" onClick={() => setManualOverrides(new Map())} disabled={manualOverrides.size === 0} fullWidth>
                      Reset All Overrides
                    </Button>
                  </Grid>
                  <Grid item xs={4} sx={{ mt: 0.5 }}>
                    <Button size="small" variant="outlined" onClick={handleExportToExcel} fullWidth>
                      Export to Excel
                    </Button>
                  </Grid>
                  {/* Second row */}
                  <Grid item xs={4}>
                    <Button size="small" variant="outlined" onClick={handleSaveClick} startIcon={<SaveIcon />} fullWidth>
                      Save
                    </Button>
                  </Grid>
                  <Grid item xs={4}>
                    <Button size="small" variant="outlined" onClick={handleCloneClick} startIcon={<FileCopyIcon />} fullWidth>
                      Clone
                    </Button>
                  </Grid>
                  <Grid item xs={4}>
                    <Button size="small" variant="outlined" onClick={() => setSavedEstimatesDialogOpen(true)} startIcon={<FolderIcon />} fullWidth>
                      Load Saved
                    </Button>
                  </Grid>
                  <Grid item xs={12}>
                    <Button
                      variant="outlined"
                      color="error"
                      fullWidth
                      sx={{ mt: 1 }}
                      onClick={() => setResetAllDialogOpen(true)}
                    >
                      Reset All (Restore Defaults)
                    </Button>
                  </Grid>
                </Grid>
              </Box>
            </Grid>
          </Grid>
        </Grid>
        {/* Right Column: Days Config, Project Info, Schedule, Results, Totals */}
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
                          value={daysOnSite}
                          onChange={handleDaysOnSiteChange}
                          inputProps={{ min: 0 }}
                          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                          onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                        />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField size="small" label="Hours per Day" type="number" fullWidth value={hoursPerDay} onChange={e => {
                          const value = Math.max(0, Number(e.target.value));
                          setHoursPerDay(value);
                          updateCurrentResourceField('hoursPerDay', value);
                        }} inputProps={{ min: 0 }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                      </Grid>
                      <Grid container spacing={1} alignItems="center">
                        <Grid item xs={6}>
                          <Typography variant="caption" sx={{ mb: 0.5 }}>Start Day On Site</Typography>
                          <Select
                            size="small"
                            fullWidth
                            value={startDay}
                            onChange={e => {
                              setStartDay(e.target.value);
                              updateCurrentResourceField('startDay', e.target.value);
                            }}
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
                          control={<Checkbox size="small" checked={includeSaturdays} onChange={e => {
                            setIncludeSaturdays(e.target.checked);
                            updateCurrentResourceField('includeSaturdays', e.target.checked);
                          }} />}
                          label={<Typography variant="caption">Include Saturdays</Typography>}
                        />
                        <FormControlLabel
                          control={<Checkbox size="small" checked={includeSundays} onChange={e => {
                            setIncludeSundays(e.target.checked);
                            updateCurrentResourceField('includeSundays', e.target.checked);
                          }} />}
                          label={<Typography variant="caption">Include Sundays</Typography>}
                          sx={{ ml: 2 }}
                        />
                        <FormControlLabel
                          control={<Checkbox size="small" checked={otBefore7After5} onChange={e => {
                            setOtBefore7After5(e.target.checked);
                            updateCurrentResourceField('otBefore7After5', e.target.checked);
                          }} />}
                          label={<Typography variant="caption">OT before 7am and after 5pm</Typography>}
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
                      <Grid item xs={6}><TextField size="small" label="Project Number" fullWidth value={projectNumber} onChange={e => setProjectNumber(e.target.value)} /></Grid>
                      <Grid item xs={6}><TextField size="small" label="Customer" fullWidth value={customer} onChange={e => setCustomer(e.target.value)} /></Grid>
                      <Grid item xs={6}><TextField size="small" label="Project Description" fullWidth value={projectDescription} onChange={e => setProjectDescription(e.target.value)} /></Grid>
                      <Grid item xs={6}><TextField size="small" label="Technician" fullWidth value={technician} onChange={e => {
                        setTechnician(e.target.value);
                        updateCurrentResourceField('technician', e.target.value);
                      }} /></Grid>
                      <LocalizationProvider dateAdapter={AdapterDayjs}>
                        <Grid item xs={6}>
                          <DatePicker
                            label="Start Date"
                            value={startDate}
                            onChange={val => handleStartDateChange({ target: { value: val ? val.format('YYYY-MM-DD') : '' } } as any)}
                            slotProps={{ textField: { size: 'small', fullWidth: true, placeholder: 'Unspecified' } }}
                            format="YYYY-MM-DD"
                          />
                        </Grid>
                        <Grid item xs={2}>
                          <Button size="small" variant="outlined" onClick={handleClearStartDate}>Clear</Button>
                        </Grid>
                        <Grid item xs={6}>
                          <DatePicker
                            label="End Date"
                            value={endDate}
                            onChange={val => handleEndDateChange({ target: { value: val ? val.format('YYYY-MM-DD') : '' } } as any)}
                            slotProps={{ textField: { size: 'small', fullWidth: true, placeholder: 'Unspecified', error: !!startDate && !!endDate && endDate.isBefore(startDate, 'day'), helperText: !!startDate && !!endDate && endDate.isBefore(startDate, 'day') ? "End date can't be before start date" : '' } }}
                            format="YYYY-MM-DD"
                          />
                        </Grid>
                        <Grid item xs={2}>
                          <Button size="small" variant="outlined" onClick={handleClearEndDate}>Clear</Button>
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
                    {/* Day-of-week header row (fixed height) */}
                    <Box sx={{ display: 'flex', width: '100%', height: 32, minHeight: 32, maxHeight: 32, mb: 0.5, position: 'relative' }}>
                      {daysOfWeek.map((day, idx) => (
                        <Box key={day} sx={{ width: scheduleBoxSize, height: 32, display: 'flex', alignItems: 'center', justifyContent: 'center', color: scheduleInactiveText, fontWeight: 600, borderRight: idx !== 6 ? `1px solid ${panelBorder}` : 0, borderBottom: `1px solid ${panelBorder}` }}>{day}</Box>
                      ))}
                      {/* Month label at top right */}
                      {startDate && (
                        <Typography variant="subtitle2" sx={{ position: 'absolute', top: -32, right: 8, fontSize: '1.1rem', color: scheduleInactiveText, fontWeight: 600 }}>
                          {startDate.format('MMMM')}
                        </Typography>
                      )}
                    </Box>
                    {/* Schedule grid (scrollable) */}
                    <Box sx={{ flex: 1, overflowY: 'auto', width: '100%' }}>
                      <Grid container spacing={0} sx={{ margin: 0, width: '100%' }}>
                        {calendarWeeks.map((week, weekIdx) => (
                          <React.Fragment key={weekIdx}>
                            {week.map((day, dayIdx) => {
                              const isBlank = !day;
                              return (
                                <Grid item key={weekIdx + '-' + dayIdx} sx={{ padding: 0, margin: 0, minWidth: 0, minHeight: 0, width: scheduleBoxSize, height: scheduleBoxSize, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
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
                                            ? '#23262b' // gray for No Activity
                                            : (day.type === 'TravelTo' || day.type === 'TravelFrom' ? travelDayBg :
                                              manualOverrides.has(day.dayNumber) && manualOverrides.get(day.dayNumber)?.dayType === DayType.Holdover ? holdoverDayBg :
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
                                      <Typography variant="caption" sx={{ position: 'absolute', top: 2, right: 4, fontSize: '0.75rem', color: '#fff', opacity: 0.7 }}>
                                        {startDate.add(day.dayNumber - 1, 'day').date()}
                                      </Typography>
                                    )}
                                    {day ? (
                                      <>
                                        <Typography variant="caption" sx={{ textAlign: 'left', width: '100%', pl: 0.5 }}>Day {day.dayNumber}</Typography>
                                        {/* Show start time only for work days */}
                                        {day.type === 'WorkDay' && startTimeOnSite && (
                                          <Typography sx={{ mt: 0.2, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>
                                            {(() => {
                                              const override = manualOverrides.get(day.dayNumber);
                                              if (override && override.startTime) {
                                                return dayjs(override.startTime, 'HH:mm').format('hh:mm A');
                                              }
                                              return startTimeOnSite ? startTimeOnSite.format('hh:mm A') : '';
                                            })()}
                                          </Typography>
                                        )}
                                        {/* Only show labour/travel if not a No Activity day */}
                                        {!(manualOverrides.has(day.dayNumber) && manualOverrides.get(day.dayNumber)?.dayType === DayType.Nil) && <>
                                          <Typography sx={{ mt: 0.5, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>Labour: {day.totalLabourHours?.toFixed(1) ?? '0.0'} hrs.</Typography>
                                          <Typography sx={{ mt: 0.2, textAlign: 'left', width: '100%', pl: 0.5, fontSize: '0.7rem' }}>Travel: {day.totalTravelHours?.toFixed(1) ?? '0.0'} hrs.</Typography>
                                        </>}
                                        {manualOverrides.has(day.dayNumber) && manualOverrides.get(day.dayNumber)?.dayType !== DayType.Nil && (
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
                    <Typography variant="h6" sx={{ fontWeight: 700, mr: 2 }}>Total Days: <span style={{ fontSize: '2rem', fontWeight: 700 }}>{totalDays}</span></Typography>
                    <Box sx={{ flex: 1 }} />
                    <Typography variant="caption" sx={{ position: 'absolute', top: -10, right: 0, color: panelText }}>(Cost + 10%, not incl. per Diem)</Typography>
                  </Box>
                  
                  {/* Grand Total for All Resources and Current Resource - CONDITIONAL SIDE BY SIDE */}
                  {resources.length > 1 ? (
                    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'row', alignItems: 'stretch', width: '100%', gap: 2 }}>
                      {/* Current Resource Total (left, smaller font) */}
                      <Box sx={{ flex: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#222' : '#222', mr: 1 }}>
                        <Typography variant="subtitle2" sx={{ color: '#fff', fontWeight: 600 }}>Current Resource Total</Typography>
                        <Typography variant="h6" sx={{ color: darkMode ? '#4fc3f7' : '#00bfff', fontWeight: 700 }}>${grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Typography>
                      </Box>
                      {/* All Resources Total (right, bigger font) */}
                      <Box sx={{ flex: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#1a1a1a' : '#f0f0f0', ml: 1 }}>
                        <Typography variant="subtitle1" sx={{ color: panelText, fontWeight: 600, mb: 1 }}>All Resources Total</Typography>
                        <Typography variant="h4" sx={{ color: darkMode ? '#4fc3f7' : '#1976d2', fontWeight: 700 }}>
                          ${resources.reduce((sum, resource) => {
                            // Calculate total for this resource using the same logic as current resource
                            const resourceSheet = rateSheets.find(s => s.name === resource.selectedSheet) || rateSheets[0];
                            const resourceCustomSheet = {
                              ...resourceSheet,
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
                        <Typography variant="h4" sx={{ color: darkMode ? '#4fc3f7' : '#00bfff', fontWeight: 700 }}>${grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Typography>
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
                          <TableCell sx={{ color: (day.labourCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.labourCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.travelCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.travelCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.mileageCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.mileageCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.hotelCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.hotelCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.rentalCarCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.rentalCarCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.airfareCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.airfareCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.perDiem ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.perDiem ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                          <TableCell sx={{ color: (day.totalDayCost ?? 0) === 0 ? '#888' : 'inherit' }}>${(day.totalDayCost ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
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
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>Rate Sheet Setup</DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined, minWidth: 700 }}>
    <Grid container spacing={2}>
            {/* Left: List of Rate Sheets */}
            <Grid item xs={4}>
              <Paper variant="outlined" sx={{ height: 340, overflowY: 'auto', bgcolor: darkMode ? '#23262b' : '#fafafa' }}>
                {rateSheets.map((sheet, idx) => (
                  <Box key={sheet.name} sx={{
                    px: 2, py: 1, cursor: 'pointer',
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
                  <Grid item xs={12}><TextField label="Name" value={editingSheet?.name || ''} onChange={e => handleEditField('name', e.target.value)} fullWidth autoFocus onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={4}>
                    <TextField label="Regular Labour Rate" type="number" value={editingSheet?.regularLabourRate || ''} onChange={e => handleEditField('regularLabourRate', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField label="Overtime Labour Rate" type="number" value={editingSheet?.overtimeLabourRate || ''} fullWidth InputProps={{ readOnly: true }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField label="Premium Labour Rate" type="number" value={editingSheet?.premiumLabourRate || ''} fullWidth InputProps={{ readOnly: true }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField label="Regular Travel Rate" type="number" value={editingSheet?.regularTravelRate || ''} onChange={e => handleEditField('regularTravelRate', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField label="Overtime Travel Rate" type="number" value={editingSheet?.overtimeTravelRate || ''} fullWidth InputProps={{ readOnly: true }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}>
                    <TextField label="Premium Travel Rate" type="number" value={editingSheet?.premiumTravelRate || ''} fullWidth InputProps={{ readOnly: true }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                  </Grid>
                  <Grid item xs={4}><TextField label="Hotel Cost" type="number" value={editingSheet?.hotelCost || ''} onChange={e => handleEditField('hotelCost', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={4}><TextField label="Per Diem Rate" type="number" value={editingSheet?.perDiemRate || ''} onChange={e => handleEditField('perDiemRate', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={4}><TextField label="Mileage Rate" type="number" value={editingSheet?.mileageRate || ''} onChange={e => handleEditField('mileageRate', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={4}><TextField label="Rental Car Rate" type="number" value={editingSheet?.rentalCarRate || ''} onChange={e => handleEditField('rentalCarRate', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
                  <Grid item xs={4}><TextField label="Flight Cost" type="number" value={editingSheet?.flightCost || ''} onChange={e => handleEditField('flightCost', parseFloat(e.target.value))} fullWidth InputProps={{ inputProps: { step: 0.01 } }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
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
          currentOverride={manualOverrides.get(editingDay) || null}
          defaultDayType={getDefaultDayType(filteredDayDetails.find(d => d.dayNumber === editingDay)!)}
          defaultLabourHours={(() => {
            const day = filteredDayDetails.find(d => d.dayNumber === editingDay);
            const override = manualOverrides.get(editingDay);
            if ((override && override.dayType === DayType.Travel) || (!override && day && (day.type === 'TravelTo' || day.type === 'TravelFrom'))) {
              return 0;
            }
            return day?.totalLabourHours || 0;
          })()}
          defaultTravelHours={(() => {
            const day = filteredDayDetails.find(d => d.dayNumber === editingDay);
            const override = manualOverrides.get(editingDay);
            if ((override && override.dayType === DayType.Travel) || (!override && day && (day.type === 'TravelTo' || day.type === 'TravelFrom'))) {
              return travelTime;
            }
            return day?.totalTravelHours || 0;
          })()}
          defaultStartTime={(() => {
            const override = manualOverrides.get(editingDay);
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

      {/* Saved Estimates Dialog */}
      <Dialog open={savedEstimatesDialogOpen} onClose={() => setSavedEstimatesDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle sx={{ bgcolor: darkMode ? '#23262b' : undefined, color: darkMode ? '#fff' : undefined }}>
          Load Saved Estimate
        </DialogTitle>
        <DialogContent sx={{ bgcolor: darkMode ? '#23262b' : undefined, minHeight: 300 }}>
          {savedEstimates.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <Typography variant="body1" color="textSecondary">
                No saved estimates found.
              </Typography>
            </Box>
          ) : (
            <List>
              {savedEstimates
                .sort((a, b) => b.timestamp - a.timestamp)
                .map((estimate) => (
                  <ListItem
                    key={estimate.id}
                    sx={{
                      border: '1px solid',
                      borderColor: darkMode ? '#444' : '#ddd',
                      borderRadius: 1,
                      mb: 1,
                      '&:hover': {
                        bgcolor: darkMode ? '#333' : '#f5f5f5'
                      }
                    }}
                  >
                    <ListItemText
                      primary={estimate.name}
                      secondary={`Saved on ${new Date(estimate.timestamp).toLocaleString()}`}
                    />
                    <ListItemSecondaryAction>
                      <Button
                        size="small"
                        variant="outlined"
                        onClick={() => handleLoadEstimate(estimate)}
                        sx={{ mr: 1 }}
                      >
                        Load
                      </Button>
                      <IconButton
                        size="small"
                        onClick={() => deleteSavedEstimate(estimate.id)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
            </List>
          )}
        </DialogContent>
        <DialogActions sx={{ bgcolor: darkMode ? '#23262b' : undefined }}>
          <Button onClick={() => setSavedEstimatesDialogOpen(false)} color="inherit" variant="outlined">
            Close
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
    </Box>
  );
};

export default QuickEstimator;