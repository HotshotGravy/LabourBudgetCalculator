import React, { useState, useEffect } from 'react';
import { Box, Grid, Typography, TextField, Select, MenuItem, Checkbox, FormControlLabel, Button, InputAdornment, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Dialog, DialogTitle, DialogContent, DialogActions, IconButton } from '@mui/material';
import { DataManager } from '../utils/DataManager';
import { RateSheet, RateSheetClass } from '../models/RateSheet';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import AddIcon from '@mui/icons-material/Add';
import { calculateEstimate } from '../utils/estimatorEngine';
import { DayEditorDialog } from './DayEditorDialog';
import { DayType } from '../models/ResourceDayData';
import { CalculationDayType, DayDetail, CalculationResult } from '../models/CalculationResult';
import dayjs, { Dayjs } from 'dayjs';
import { DatePicker, LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

const daysOfWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
const SCHEDULE_DAYS = 14;
const SCHEDULE_COLS = 7;
const SCHEDULE_ROWS = 2;

interface QuickEstimatorProps {
  darkMode?: boolean;
}

interface ManualDayOverride {
  dayType: DayType;
  labourHours: number;
  travelHours: number;
  includeExpenses?: boolean;
}

const QuickEstimator: React.FC<QuickEstimatorProps> = ({ darkMode }) => {
  // Calculate schedule box size based on available height (responsive)
  // For now, use fixed height for the schedule area for WinForms-like look
  const scheduleBoxSize = 99.225; // 90 * 1.05 * 1.05
  const scheduleHeight = scheduleBoxSize * SCHEDULE_ROWS + 16; // 16px for grid spacing

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

  // When selectedSheet or currentSheet changes, update these values
  useEffect(() => {
    setHotelCost(currentSheet?.hotelCost ?? 0);
    setRentalCarRate(currentSheet?.rentalCarRate ?? 0);
    setFlightCost(currentSheet?.flightCost ?? 0);
    setMileageRate(currentSheet?.mileageRate ?? 0);
    setPerDiemRate(currentSheet?.perDiemRate ?? 0);
  }, [currentSheet]);

  // Calculate
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
    manualOverrides
  });

  // Update the schedule creation logic to set excluded days as No Activity
  const filteredDayDetails = calcResult.dayDetails.map(day => {
    const isSaturday = daysOfWeek[day.dayOfWeek] === 'Saturday';
    const isSunday = daysOfWeek[day.dayOfWeek] === 'Sunday';
    if ((!includeSaturdays && isSaturday) || (!includeSundays && isSunday)) {
      return {
        ...day,
        type: CalculationDayType.None as CalculationDayType,
        totalLabourHours: 0,
        totalTravelHours: 0,
        regularLabourHours: 0,
        overtimeLabourHours: 0,
        premiumLabourHours: 0,
        regularTravelHours: 0,
        overtimeTravelHours: 0,
        premiumTravelHours: 0,
        labourCost: 0,
        travelCost: 0,
        hotelCost: 0,
        perDiem: 0,
        mileageCost: 0,
        rentalCarCost: 0,
        airfareCost: 0,
        totalDayCost: 0
      };
    }
    return day;
  });

  // Helper function to get discounted rate
  const getDiscountedRate = (base: number, premium: number) => {
    let rate = isEmergency ? premium : base;
    let discounted = rate * (1 - discountPercent / 100);
    return discountPercent > 0 ? `${rate.toFixed(2)} → ${discounted.toFixed(2)}` : rate.toFixed(2);
  };

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
    }
  };

  const getDefaultDayType = (day: typeof filteredDayDetails[number]): DayType => {
    if (day.type === 'TravelTo' || day.type === 'TravelFrom') return DayType.Travel;
    if (day.type === 'None') return DayType.Nil;
    return DayType.Work; // Default for WorkDay
  };

  // Define colors for different day types
  const travelDayBg = darkMode ? '#afa436' : '#ffe066';
  const holdoverDayBg = darkMode ? '#2e7d32' : '#4caf50';

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

  // State for Reset All confirmation dialog
  const [resetAllDialogOpen, setResetAllDialogOpen] = useState(false);

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
    setResetAllDialogOpen(false);
  };

  // Add helper functions for rounding and linking
  // Helper: round to nearest increment
  function roundToNearest(value: number, increment: number) {
    return Math.round(value / increment) * increment;
  }

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

  // --- Date logic ---
  useEffect(() => {
    // If both dates are set, update daysOnSite
    if (startDate && endDate) {
      const diff = endDate.diff(startDate, 'day') + 1;
      if (diff > 0 && daysOnSite !== diff) setDaysOnSite(diff);
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
    if (!value) setStartDay('Monday'); // or your default
  };
  const handleEndDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value ? dayjs(e.target.value) : null;
    setEndDate(value);
  };
  const handleDaysOnSiteChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = Math.max(1, Number(e.target.value));
    setDaysOnSite(value);
  };
  const handleClearStartDate = () => {
    setStartDate(null);
    setStartDay('Monday'); // or your default
  };
  const handleClearEndDate = () => setEndDate(null);

  // --- EXCEL EXPORT ---
  const handleExportToExcel = async () => {
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
    summaryRows.push(['Total Days On Site:', totalDays.toString(), '']); // 9
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
    summaryRows.push(['Expenses:', 'N/A', totalExpenses.toLocaleString(undefined, { style: 'currency', currency: 'USD' })]); // 21
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
      { header: '', width: 13 }, // Date
      { header: '', width: 12 }, // Day
      { header: '', width: 12 }, // Type
      { header: '', width: 8 }, // RegLab
      { header: '', width: 8 }, // OT Lab
      { header: '', width: 8 }, // PremLab
      { header: '', width: 8 }, // TotLab
      { header: '', width: 8 }, // RegTrav
      { header: '', width: 8 }, // OT Trav
      { header: '', width: 8 }, // PremTrav
      { header: '', width: 8 }, // TotTrav
      { header: '', width: 13 }, // Labour Cost
      { header: '', width: 13 }, // Travel Cost
      { header: '', width: 13 }, // Hotel Cost
      { header: '', width: 13 }, // Per Diem
      { header: '', width: 13 }, // Mileage Cost
      { header: '', width: 13 }, // Rental Car Cost
      { header: '', width: 13 }, // Airfare Cost
      { header: '', width: 15 }  // Total Day Cost
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
    wsDetail.mergeCells('D2:G2'); wsDetail.getCell('D2').value = 'Labour';
    wsDetail.mergeCells('H2:K2'); wsDetail.getCell('H2').value = 'Travel';
    wsDetail.mergeCells('L2:R2'); wsDetail.getCell('L2').value = 'Expenses';
    wsDetail.getCell('L2').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.mergeCells('S2:S3'); wsDetail.getCell('S2').value = 'Total Day Cost';
    wsDetail.getCell('S2').alignment = { horizontal: 'center', vertical: 'middle' };
    wsDetail.getCell('S2').font = { bold: true, size: 12 };
    // Second header row: subheaders
    const subHeaders = [
      '', '', '',
      'Reg.', 'OT', 'Prem.', 'Total',
      'Reg.', 'OT', 'Prem.', 'Total',
      'Labour Cost', 'Travel Cost', 'Hotel Cost', 'Per Diem', 'Mileage Cost', 'Rental Car Cost', 'Airfare Cost', ''
    ];
    const headerRow2 = wsDetail.getRow(3);
    headerRow2.values = [
      'Date', 'Day', 'Type',
      'Reg.', 'OT', 'Prem.', 'Total',
      'Reg.', 'OT', 'Prem.', 'Total',
      'Labour Cost', 'Travel Cost', 'Hotel Cost', 'Per Diem', 'Mileage Cost', 'Rental Car Cost', 'Airfare Cost', ''
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
      day.hotelCost,
      day.perDiem,
      day.mileageCost,
      day.rentalCarCost,
      day.airfareCost,
      day.totalDayCost
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
    // Autosize columns
    for (let c = 1; c <= 19; c++) {
      wsDetail.getColumn(c).width = undefined;
      wsDetail.getColumn(c).eachCell({ includeEmpty: true }, cell => {
        const text = cell.value ? cell.value.toString() : '';
        wsDetail.getColumn(c).width = Math.max(wsDetail.getColumn(c).width || 10, text.length + 2);
      });
    }
    // Save file
    const buf = await workbook.xlsx.writeBuffer();
    saveAs(new Blob([buf], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), `LabourBudgetCalculator_${projectNumber || 'Estimate'}.xlsx`);
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
  const totalExpenses = filteredDayDetails.reduce((sum, d) => sum + ((d.hotelCost ?? 0) + (d.rentalCarCost ?? 0) + (d.airfareCost ?? 0)) * 1.1 + (d.mileageCost ?? 0) + (d.perDiem ?? 0), 0);
  const grandTotal = filteredDayDetails.reduce((sum, d) => sum + (d.totalDayCost ?? 0), 0);
  const totalDays = filteredDayDetails.filter(d => d.type !== CalculationDayType.None).length;

  return (
    <Box p={1} sx={{ background: darkMode ? '#23262b' : '#fff', minHeight: '100vh', color: panelText, boxSizing: 'border-box' }}>
      <Typography variant="h6" sx={{ mb: 0.5, color: panelText }}>Time & Expense Calculator</Typography>
      <Grid container spacing={1} alignItems="flex-start">
        {/* Left Column: Rates, Travel Options, Expenses */}
        <Grid item xs={4}>
          <Box border={1} borderRadius={1} p={1} sx={{ mb: 1, background: panelBg, borderColor: panelBorder, color: panelText }}>
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
              <Grid item xs={6}><TextField size="small" label="Discount" type="number" fullWidth value={discountPercent} onChange={e => setDiscountPercent(Math.max(0, Number(e.target.value)))} InputProps={{ endAdornment: <InputAdornment position="end">%</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
              <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={isEmergency} onChange={e => setIsEmergency(e.target.checked)} />} label={<Typography variant="caption">Emergency</Typography>} /></Grid>
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
          <Box border={1} borderRadius={1} p={1} sx={{ mb: 1, background: panelBg, borderColor: panelBorder, color: panelText }}>
            <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Travel Options</Typography>
            <Grid container spacing={1.5} alignItems="center">
              <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={separateTravelTo} onChange={e => setSeparateTravelTo(e.target.checked)} />} label={<Typography variant="caption">Separate Travel Day To</Typography>} /></Grid>
              <Grid item xs={6}><FormControlLabel control={<Checkbox size="small" checked={separateTravelFrom} onChange={e => setSeparateTravelFrom(e.target.checked)} />} label={<Typography variant="caption">Separate Travel Day From</Typography>} /></Grid>
              <Grid item xs={12}>
                <TextField
                  select
                  size="small"
                  fullWidth
                  label="Travel Method to Site Area"
                  value={travelMethod}
                  onChange={e => setTravelMethod(e.target.value)}
                >
                  <MenuItem value="Driving">Driving</MenuItem>
                  <MenuItem value="Flight">Flight</MenuItem>
                </TextField>
              </Grid>
              <Grid item xs={12}>
                <TextField size="small" label="Driving Distance (First and Last Days Only)" type="number" fullWidth value={travelDistance} onChange={e => setTravelDistance(Math.max(0, Number(e.target.value)))} InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
              </Grid>
              <Grid item xs={12}>
                <TextField size="small" label="Total Travel Time to Site Area (Including Flight)" type="number" fullWidth value={travelTime}
                  onChange={e => setTravelTime(Math.max(0.25, roundToNearest(Number(e.target.value), 0.25)))}
                  inputProps={{ min: 0.25, step: 0.25 }}
                  InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                  onBlur={e => setTravelTime(Math.max(0.25, roundToNearest(Number((e.target as HTMLInputElement).value), 0.25)))}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); setTravelTime(Math.max(0.25, roundToNearest(Number((e.target as HTMLInputElement).value), 0.25))); } }}
                  onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField size="small" label="Daily Driving Distance (One Way)" type="number" fullWidth value={dailyTravelDistance} 
                  onChange={e => handleDailyTravelDistanceChange(Number(e.target.value))}
                  inputProps={{ min: 15, step: 15 }}
                  InputProps={{ endAdornment: <InputAdornment position="end">miles/km</InputAdornment> }}
                  onBlur={e => handleDailyTravelDistanceChange(Number((e.target as HTMLInputElement).value))}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); handleDailyTravelDistanceChange(Number((e.target as HTMLInputElement).value)); } }}
                  onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField size="small" label="Daily Travel Time (One Way)" type="number" fullWidth value={dailyTravelTime} 
                  onChange={e => handleDailyTravelTimeChange(Number(e.target.value))}
                  inputProps={{ min: 0.25, step: 0.25 }}
                  InputProps={{ endAdornment: <InputAdornment position="end">hours</InputAdornment> }}
                  onBlur={e => handleDailyTravelTimeChange(Number((e.target as HTMLInputElement).value))}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); handleDailyTravelTimeChange(Number((e.target as HTMLInputElement).value)); } }}
                  onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }}
                />
              </Grid>
            </Grid>
          </Box>
          <Box border={1} borderRadius={1} p={1} sx={{ mb: 1, minHeight: 207, background: panelBg, borderColor: panelBorder, color: panelText }}>
            <Typography variant="subtitle1" sx={{ mb: 1, color: panelText }}>Expenses</Typography>
            <Grid container spacing={1.5} alignItems="center">
              <Grid item xs={6} display="flex" alignItems="center">
                <FormControlLabel control={<Checkbox size="small" checked={hotelRequired} onChange={e => setHotelRequired(e.target.checked)} />} label={<Typography variant="caption">Hotel Required</Typography>} sx={{ mr: 1 }} />
                <TextField size="small" label="Hotel" type="number" value={hotelCost} onChange={e => setHotelCost(Math.max(0, Number(e.target.value)))} fullWidth disabled={!hotelRequired} InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per night</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
              </Grid>
              <Grid item xs={6} display="flex" alignItems="center">
                <FormControlLabel control={<Checkbox size="small" checked={rentalCarRequired} onChange={e => setRentalCarRequired(e.target.checked)} />} label={<Typography variant="caption">Rental Car Required</Typography>} sx={{ mr: 1 }} />
                <TextField size="small" label="Rental Car" type="number" value={rentalCarRate} onChange={e => setRentalCarRate(Math.max(0, Number(e.target.value)))} fullWidth disabled={!rentalCarRequired} InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per day</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
              </Grid>
              <Grid item xs={12}><TextField size="small" label="Flight Cost (One Way)" type="number" value={flightCost} onChange={e => setFlightCost(Math.max(0, Number(e.target.value)))} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
              <Grid item xs={12}><TextField size="small" label="Mileage" type="number" value={mileageRate} onChange={e => setMileageRate(Math.max(0, Number(e.target.value)))} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per mile/km</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
              <Grid item xs={12}><TextField size="small" label="Per Diem" type="number" value={perDiemRate} onChange={e => setPerDiemRate(Math.max(0, Number(e.target.value)))} fullWidth InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment>, endAdornment: <InputAdornment position="end">per day</InputAdornment> }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} /></Grid>
            </Grid>
          </Box>
          <Box border={1} borderRadius={1} p={1} sx={{ mb: 0, background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
            <Grid container spacing={1} alignItems="center" justifyContent="center">
              <Grid item>
                <Button size="small" variant="outlined" onClick={() => {
                  setEditDialogOpen(true);
                  if (!selectedSheet && rateSheets.length > 0) {
                    setSelectedSheet(rateSheets[0].name);
                    setEditingSheet({ ...rateSheets[0] });
                    setIsNew(false);
                  }
                }} startIcon={<EditIcon />}>
                  Edit Rate Sheets
                </Button>
              </Grid>
              <Grid item>
                <Button size="small" variant="outlined" onClick={() => setManualOverrides(new Map())} disabled={manualOverrides.size === 0}>Reset All Overrides</Button>
              </Grid>
              <Grid item>
                <Button size="small" variant="outlined" onClick={handleExportToExcel}>
                  Export to Excel
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
                        <TextField size="small" label="Total Days" type="number" fullWidth value={daysOnSite} onChange={handleDaysOnSiteChange} inputProps={{ min: 0 }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                      </Grid>
                      <Grid item xs={6}>
                        <TextField size="small" label="Hours per Day" type="number" fullWidth value={hoursPerDay} onChange={e => setHoursPerDay(Math.max(0, Number(e.target.value)))} inputProps={{ min: 0 }} onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} onKeyPress={e => { if (e.key === 'Enter') { e.preventDefault(); e.stopPropagation(); } }} />
                      </Grid>
                      <Grid item xs={12}>
                        <Typography variant="caption" sx={{ mb: 0.5 }}>Start Day On Site</Typography>
                        <Select
                          size="small"
                          fullWidth
                          value={startDay}
                          onChange={e => setStartDay(e.target.value)}
                          disabled={!!startDate}
                        >
                          {daysOfWeek.map(day => <MenuItem key={day} value={day}>{day}</MenuItem>)}
                        </Select>
                      </Grid>
                      <Grid item xs={12}>
                        <FormControlLabel
                          control={<Checkbox size="small" checked={includeSaturdays} onChange={e => setIncludeSaturdays(e.target.checked)} />}
                          label={<Typography variant="caption">Include Saturdays</Typography>}
                        />
                        <FormControlLabel
                          control={<Checkbox size="small" checked={includeSundays} onChange={e => setIncludeSundays(e.target.checked)} />}
                          label={<Typography variant="caption">Include Sundays</Typography>}
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
                      <Grid item xs={6}><TextField size="small" label="Technician" fullWidth value={technician} onChange={e => setTechnician(e.target.value)} /></Grid>
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
            <Grid item sx={{ flexGrow: 0, width: '100%', mb: 1 }}>
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
                <Box border={1} borderRadius={1} p={1} sx={{ background: panelBg, borderColor: panelBorder, color: panelText, display: 'flex', flexDirection: 'column', justifyContent: 'flex-start', height: 420, flex: 1, ml: 2 }}>
                  <Typography variant="subtitle1" sx={{ mb: 0.5, color: panelText }}>Summary</Typography>
                  {/* Summary Table */}
                  <Box sx={{ mb: 2 }}>
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
                  {/* Grand Total at the bottom */}
                  <Box sx={{ mt: 1, border: `1px solid ${panelBorder}`, borderRadius: 1, p: 2, display: 'flex', flexDirection: 'column', alignItems: 'center', bgcolor: darkMode ? '#222' : '#222' }}>
                    <Typography variant="subtitle1" sx={{ color: '#fff', fontWeight: 600 }}>Grand total</Typography>
                    <Typography variant="h4" sx={{ color: darkMode ? '#4fc3f7' : '#00bfff', fontWeight: 700 }}>${grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Typography>
                  </Box>
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
          defaultLabourHours={filteredDayDetails.find(d => d.dayNumber === editingDay)?.totalLabourHours || 0}
          defaultTravelHours={filteredDayDetails.find(d => d.dayNumber === editingDay)?.totalTravelHours || 0}
        />
      )}
    </Box>
  );
};

export default QuickEstimator; 