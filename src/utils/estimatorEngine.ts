import { CalculationResult, DayDetail, CalculationDayType } from '../models/CalculationResult';
import { RateSheet } from '../models/RateSheet';
import { HourCategorizerUtil } from './HourCategorizerUtil';
import { ExpenseCalculator } from './ExpenseCalculator';
import { DayType } from '../models/ResourceDayData';

interface ManualDayOverride {
  dayType: DayType;
  labourHours: number;
  travelHours: number;
  includeExpenses?: boolean;
  startTime?: string; // HH:mm format
}

interface EstimatorInputs {
  daysOnSite: number;
  hoursPerDay: number;
  startDayOfWeek: number; // 0=Sunday, 1=Monday, ...
  holdoverDayEnabled: boolean;
  holdoverDayOfWeek: number; // 0=Sunday, 1=Monday, ...
  separateTravelTo: boolean;
  separateTravelFrom: boolean;
  travelMethod: string;
  travelDistance: number;
  travelTime: number;
  dailyTravelDistance: number;
  dailyTravelTime: number;
  rateSheet: RateSheet;
  discountPercent: number;
  isEmergency: boolean;
  hotelRequired: boolean;
  rentalCarRequired: boolean;
  otherExpenses: number;
  manualOverrides?: Map<number, ManualDayOverride>; // dayNumber -> override
  includeSaturdays: boolean;
  includeSundays: boolean;
  otBefore7After5?: boolean;
  startTimeOnSite?: any; // Dayjs | null
}

interface RawDay {
  dayNumber: number;
  dayOfWeek: number;
  isTravelToDay: boolean;
  isTravelFromDay: boolean;
  manualOverride?: ManualDayOverride;
  type: CalculationDayType;
  isHoldover: boolean;
}

export function calculateEstimate(inputs: EstimatorInputs): CalculationResult {
  const {
    daysOnSite,
    hoursPerDay,
    startDayOfWeek,
    holdoverDayEnabled,
    holdoverDayOfWeek,
    separateTravelTo,
    separateTravelFrom,
    travelMethod,
    travelDistance,
    travelTime,
    dailyTravelDistance,
    dailyTravelTime,
    rateSheet,
    discountPercent,
    isEmergency,
    hotelRequired,
    rentalCarRequired,
    otherExpenses,
    manualOverrides = new Map(),
    includeSaturdays,
    includeSundays,
    otBefore7After5 = false,
    startTimeOnSite = null,
  } = inputs;

  // Calculate discounted rates
  const discountMultiplier = 1 - (discountPercent / 100);
  const getRate = (base: number, emergencyBase: number) => 
    isEmergency ? emergencyBase * discountMultiplier : base * discountMultiplier;

  const rates = {
    regularLabour: getRate(rateSheet.regularLabourRate, rateSheet.premiumLabourRate),
    overtimeLabour: getRate(rateSheet.overtimeLabourRate, rateSheet.premiumLabourRate),
    premiumLabour: rateSheet.premiumLabourRate * discountMultiplier,
    regularTravel: getRate(rateSheet.regularTravelRate, rateSheet.premiumTravelRate),
    overtimeTravel: getRate(rateSheet.overtimeTravelRate, rateSheet.premiumTravelRate),
    premiumTravel: rateSheet.premiumTravelRate * discountMultiplier,
  };

  // Ensure rate hierarchy is maintained
  if (rates.overtimeLabour <= rates.regularLabour) {
    rates.overtimeLabour = rates.regularLabour;
  }
  if (rates.premiumLabour <= rates.overtimeLabour) {
    rates.premiumLabour = rates.overtimeLabour;
  }
  if (rates.overtimeTravel <= rates.regularTravel) {
    rates.overtimeTravel = rates.regularTravel;
  }
  if (rates.premiumTravel <= rates.overtimeTravel) {
    rates.premiumTravel = rates.overtimeTravel;
  }

  // Initialize totals
  const totals = {
    labourCost: 0,
    travelCost: 0,
    expenses: 0,
    labourHours: 0,
    travelHours: 0,
  };

  const dayDetails: DayDetail[] = [];
  const rawDays = buildRawSchedule(inputs);
  const eligibleWorkDays = getEligibleWorkDays(rawDays, includeSaturdays, includeSundays);
  const onSiteSet = new Set(eligibleWorkDays.slice(0, daysOnSite));

  // Process each day
  for (let i = 0; i < rawDays.length; i++) {
    const rawDay = rawDays[i];
    const isOnSite = onSiteSet.has(i);
    const dayDetail = processDayDetail(rawDay, isOnSite, i, rawDays.length, inputs, rates);
    
    dayDetails.push(dayDetail);
    
    // Aggregate totals
    totals.labourCost += dayDetail.labourCost || 0;
    totals.travelCost += dayDetail.travelCost || 0;
    totals.expenses += (dayDetail.hotelCost || 0) + (dayDetail.perDiem || 0) + 
                     (dayDetail.mileageCost || 0) + (dayDetail.rentalCarCost || 0) + 
                     (dayDetail.airfareCost || 0);
    totals.labourHours += dayDetail.totalLabourHours || 0;
    totals.travelHours += dayDetail.totalTravelHours || 0;
  }

  // Add 10% markup to expenses (except per diem)
  const markup = 0.10;
  const markupExpenses = dayDetails.reduce((sum, day) => 
    sum + ((day.hotelCost || 0) + (day.rentalCarCost || 0) + (day.airfareCost || 0)) * markup, 0
  );
  
  totals.expenses += markupExpenses + otherExpenses;
  const grandTotal = totals.labourCost + totals.travelCost + totals.expenses;

  return {
    totalLabourCost: totals.labourCost,
    totalTravelCost: totals.travelCost,
    totalExpenses: totals.expenses,
    grandTotal,
    totalDays: rawDays.length,
    totalLabourHours: totals.labourHours,
    totalTravelHours: totals.travelHours,
    dayDetails
  };
}

function buildRawSchedule(inputs: EstimatorInputs): RawDay[] {
  const { 
    daysOnSite, 
    startDayOfWeek, 
    separateTravelTo, 
    separateTravelFrom, 
    manualOverrides = new Map(),
    holdoverDayEnabled,
    holdoverDayOfWeek,
    includeSaturdays,
    includeSundays 
  } = inputs;
  
  const rawDays: RawDay[] = [];

  // Add travel to day if needed
  if (separateTravelTo) {
    const travelToDayOfWeek = (startDayOfWeek + 6) % 7;
    rawDays.push({
      dayNumber: 1,
      dayOfWeek: travelToDayOfWeek,
      isTravelToDay: true,
      isTravelFromDay: false,
      manualOverride: manualOverrides.get(1),
      type: CalculationDayType.TravelTo,
      isHoldover: false
    });
  }

  // Add work days
  let currentDayOfWeek = startDayOfWeek;
  let assignedOnSite = 0;

  while (assignedOnSite < daysOnSite) {
    const dayNumber = rawDays.length + 1;
    const manualOverride = manualOverrides.get(dayNumber);
    
    const dayInfo = determineDayType(
      currentDayOfWeek, 
      manualOverride, 
      holdoverDayEnabled, 
      holdoverDayOfWeek, 
      includeSaturdays, 
      includeSundays
    );

    rawDays.push({
      dayNumber,
      dayOfWeek: currentDayOfWeek,
      isTravelToDay: false,
      isTravelFromDay: false,
      manualOverride,
      type: dayInfo.type,
      isHoldover: dayInfo.isHoldover
    });

    // Count eligible on-site days
    if (dayInfo.type === CalculationDayType.WorkDay && 
        !dayInfo.isHoldover && 
        (!manualOverride || manualOverride.dayType !== DayType.Nil)) {
      assignedOnSite++;
    }

    currentDayOfWeek = (currentDayOfWeek + 1) % 7;
  }

  // Add travel from day if needed
  if (separateTravelFrom) {
    rawDays.push({
      dayNumber: rawDays.length + 1,
      dayOfWeek: currentDayOfWeek,
      isTravelToDay: false,
      isTravelFromDay: true,
      manualOverride: manualOverrides.get(rawDays.length + 1),
      type: CalculationDayType.TravelFrom,
      isHoldover: false
    });
  }

  return rawDays;
}

function determineDayType(
  dayOfWeek: number,
  manualOverride: ManualDayOverride | undefined,
  holdoverDayEnabled: boolean,
  holdoverDayOfWeek: number,
  includeSaturdays: boolean,
  includeSundays: boolean
): { type: CalculationDayType; isHoldover: boolean } {
  
  if (manualOverride) {
    switch (manualOverride.dayType) {
      case DayType.Work:
        return { type: CalculationDayType.WorkDay, isHoldover: false };
      case DayType.Travel:
        return { type: CalculationDayType.TravelFrom, isHoldover: false };
      case DayType.Holdover:
        return { type: CalculationDayType.WorkDay, isHoldover: true };
      case DayType.Nil:
        return { type: CalculationDayType.None, isHoldover: false };
    }
  }

  // Check for holdover
  const isHoldover = holdoverDayEnabled && ((dayOfWeek + 7) % 7) === holdoverDayOfWeek;
  
  // Check for excluded weekends
  if ((dayOfWeek === 6 && !includeSaturdays) || (dayOfWeek === 0 && !includeSundays)) {
    return { type: CalculationDayType.None, isHoldover: false };
  }

  return { type: CalculationDayType.WorkDay, isHoldover };
}

function getEligibleWorkDays(
  rawDays: RawDay[], 
  includeSaturdays: boolean, 
  includeSundays: boolean
): number[] {
  const eligibleIndexes: number[] = [];
  
  for (let i = 0; i < rawDays.length; i++) {
    const day = rawDays[i];
    const isSaturday = day.dayOfWeek === 6;
    const isSunday = day.dayOfWeek === 0;
    
    const isEligible = day.type === CalculationDayType.WorkDay &&
                      !day.isHoldover &&
                      (!day.manualOverride || day.manualOverride.dayType !== DayType.Nil) &&
                      ((includeSaturdays || !isSaturday) && (includeSundays || !isSunday));
    
    if (isEligible) {
      eligibleIndexes.push(i);
    }
  }
  
  return eligibleIndexes;
}

function processDayDetail(
  rawDay: RawDay,
  isOnSite: boolean,
  dayIndex: number,
  totalDays: number,
  inputs: EstimatorInputs,
  rates: any
): DayDetail {
  const {
    hoursPerDay,
    dailyTravelTime,
    travelTime,
    otBefore7After5 = false,
    startTimeOnSite,
    rateSheet,
    hotelRequired,
    rentalCarRequired,
    travelMethod,
    travelDistance,
    dailyTravelDistance,
    separateTravelTo,
    separateTravelFrom
  } = inputs;

  let type = rawDay.type;
  const { isHoldover, manualOverride, dayOfWeek, dayNumber } = rawDay;

  // Adjust type if not on-site
  if (!isOnSite && type === CalculationDayType.WorkDay && !isHoldover) {
    type = CalculationDayType.None;
  }

  // Handle No Activity days
  if (type === CalculationDayType.None) {
    return createNoActivityDay(rawDay, dayIndex, totalDays, inputs);
  }

  // Calculate hours
  const { labourHours, travelHours } = calculateDayHours(
    type, manualOverride, isHoldover, hoursPerDay, dailyTravelTime, travelTime
  );

  // Get hour breakdown
  const breakdown = getHourBreakdown(
    labourHours, travelHours, dayOfWeek, inputs.isEmergency, isHoldover, 
    otBefore7After5, manualOverride, startTimeOnSite
  );

  // Calculate costs
  const labourCost = calculateLabourCost(breakdown, rates);
  const travelCost = calculateTravelCost(breakdown, rates);

  // Calculate expenses
  const isFirstDay = dayIndex === 0;
  const isLastDay = dayIndex === totalDays - 1;
  const expenses = calculateExpenses(
    type, isFirstDay, isLastDay, labourHours, travelHours, rateSheet,
    hotelRequired, rentalCarRequired, travelMethod, travelDistance,
    dailyTravelDistance, separateTravelTo, separateTravelFrom
  );

  const totalDayCost = labourCost + travelCost + expenses.total;

  return {
    dayNumber,
    dayOfWeek,
    type,
    isHoldover,
    regularLabourHours: breakdown.regularLabourHours,
    overtimeLabourHours: breakdown.overtimeLabourHours,
    premiumLabourHours: breakdown.premiumLabourHours,
    totalLabourHours: breakdown.regularLabourHours + breakdown.overtimeLabourHours + breakdown.premiumLabourHours,
    regularTravelHours: breakdown.regularTravelHours,
    overtimeTravelHours: breakdown.overtimeTravelHours,
    premiumTravelHours: breakdown.premiumTravelHours,
    totalTravelHours: breakdown.regularTravelHours + breakdown.overtimeTravelHours + breakdown.premiumTravelHours,
    labourCost,
    travelCost,
    hotelCost: expenses.hotel,
    perDiem: expenses.perDiem,
    mileageCost: expenses.mileage,
    rentalCarCost: expenses.rentalCar,
    airfareCost: expenses.flight,
    totalDayCost
  };
}

function createNoActivityDay(
  rawDay: RawDay,
  dayIndex: number,
  totalDays: number,
  inputs: EstimatorInputs
): DayDetail {
  const { dayNumber, dayOfWeek, manualOverride } = rawDay;
  let expenses = { hotel: 0, perDiem: 0, mileage: 0, rentalCar: 0, flight: 0, total: 0 };

  // Include expenses unless explicitly disabled
  if (!manualOverride || manualOverride.includeExpenses !== false) {
    const isFirstDay = dayIndex === 0;
    const isLastDay = dayIndex === totalDays - 1;
    expenses = calculateExpenses(
      CalculationDayType.None, isFirstDay, isLastDay, 0, 0, inputs.rateSheet,
      inputs.hotelRequired, inputs.rentalCarRequired, inputs.travelMethod,
      inputs.travelDistance, inputs.dailyTravelDistance, inputs.separateTravelTo, inputs.separateTravelFrom
    );
  }

  return {
    dayNumber,
    dayOfWeek,
    type: CalculationDayType.None,
    isHoldover: false,
    regularLabourHours: 0,
    overtimeLabourHours: 0,
    premiumLabourHours: 0,
    totalLabourHours: 0,
    regularTravelHours: 0,
    overtimeTravelHours: 0,
    premiumTravelHours: 0,
    totalTravelHours: 0,
    labourCost: 0,
    travelCost: 0,
    hotelCost: expenses.hotel,
    perDiem: expenses.perDiem,
    mileageCost: expenses.mileage,
    rentalCarCost: expenses.rentalCar,
    airfareCost: expenses.flight,
    totalDayCost: expenses.total
  };
}

function calculateDayHours(
  type: CalculationDayType,
  manualOverride: ManualDayOverride | undefined,
  isHoldover: boolean,
  hoursPerDay: number,
  dailyTravelTime: number,
  travelTime: number
): { labourHours: number; travelHours: number } {
  
  if (manualOverride) {
    if (manualOverride.dayType === DayType.Holdover) {
      return { labourHours: 8, travelHours: 0 };
    }
    return { labourHours: manualOverride.labourHours, travelHours: manualOverride.travelHours };
  }

  if (type === CalculationDayType.WorkDay) {
    return {
      labourHours: isHoldover ? 8 : hoursPerDay,
      travelHours: isHoldover ? 0 : dailyTravelTime * 2
    };
  }

  if (type === CalculationDayType.TravelTo || type === CalculationDayType.TravelFrom) {
    return { labourHours: 0, travelHours: travelTime };
  }

  return { labourHours: 0, travelHours: 0 };
}

function getHourBreakdown(
  labourHours: number,
  travelHours: number,
  dayOfWeek: number,
  isEmergency: boolean,
  isHoldover: boolean,
  otBefore7After5: boolean,
  manualOverride: ManualDayOverride | undefined,
  startTimeOnSite: any
) {
  // Handle OT before 7am/after 5pm logic for weekdays
  if (otBefore7After5 && dayOfWeek >= 1 && dayOfWeek <= 5 && !isHoldover) {
    // Check if there would actually be time-based overtime
    const timeBasedBreakdown = calculateTimeBasedOT(labourHours, travelHours, manualOverride, startTimeOnSite);
    
    // Only use time-based logic if there is actual time-based overtime
    if (timeBasedBreakdown.overtimeLabourHours > 0 || timeBasedBreakdown.overtimeTravelHours > 0) {
      return {
        regularLabourHours: timeBasedBreakdown.regularLabourHours,
        overtimeLabourHours: timeBasedBreakdown.overtimeLabourHours,
        premiumLabourHours: timeBasedBreakdown.premiumLabourHours,
        regularTravelHours: timeBasedBreakdown.regularTravelHours,
        overtimeTravelHours: timeBasedBreakdown.overtimeTravelHours,
        premiumTravelHours: timeBasedBreakdown.premiumTravelHours
      };
    }
  }

  return HourCategorizerUtil.categorizeDailyHours(
    labourHours, travelHours, dayOfWeek, isEmergency, isHoldover
  );
}

// ========================================
// ADD this helper function to estimatorEngine.ts
// ========================================

// ADD this new function after the getHourBreakdown function:

function categorizeTravelHours(
  hours: number,
  dayOfWeek: number,
  isEmergency: boolean
): { regular: number; overtime: number; premium: number } {
  if (hours <= 0) return { regular: 0, overtime: 0, premium: 0 };
  
  if (isEmergency) {
    return { regular: 0, overtime: 0, premium: hours };
  }
  
  if (dayOfWeek === 0) { // Sunday = Premium
    return { regular: 0, overtime: 0, premium: hours };
  }
  
  if (dayOfWeek === 6) { // Saturday = Overtime
    return { regular: 0, overtime: hours, premium: 0 };
  }
  
  // Weekdays = Regular (travel is typically not subject to daily OT limits)
  return { regular: hours, overtime: 0, premium: 0 };
}

function calculateTimeBasedOT(
  labourHours: number,
  travelHours: number,
  manualOverride: ManualDayOverride | undefined,
  startTimeOnSite: any
) {
  const startTimeStr = (manualOverride && manualOverride.startTime) ? 
    manualOverride.startTime : 
    (startTimeOnSite ? (startTimeOnSite.format ? startTimeOnSite.format('HH:mm') : startTimeOnSite) : '08:00');
  
  const [startHour, startMin] = startTimeStr.split(':').map(Number);
  const startMinutes = startHour * 60 + startMin;
  
  // Calculate travel start and end times
  const travelStartMinutes = startMinutes - Math.round((travelHours / 2) * 60);
  const labourEndMinutes = startMinutes + Math.round(labourHours * 60);
  const travelEndMinutes = labourEndMinutes + Math.round((travelHours / 2) * 60);
  
  let labourOtMinutes = 0;
  let travelOtMinutes = 0;
  
  // OT before 7:00 AM (420 minutes)
  if (travelStartMinutes < 420) {
    const otEnd = Math.min(420, travelEndMinutes);
    const totalOtBefore7 = otEnd - travelStartMinutes;
    
    // Distribute overtime proportionally between travel and labour
    if (totalOtBefore7 > 0) {
      const travelRatio = travelHours / (labourHours + travelHours);
      const labourRatio = labourHours / (labourHours + travelHours);
      
      travelOtMinutes += totalOtBefore7 * travelRatio;
      labourOtMinutes += totalOtBefore7 * labourRatio;
    }
  }
  
  // OT after 5:00 PM (1020 minutes)
  if (travelEndMinutes > 1020) {
    const otStart = Math.max(1020, travelStartMinutes);
    const totalOtAfter5 = travelEndMinutes - otStart;
    
    // Distribute overtime proportionally between travel and labour
    if (totalOtAfter5 > 0) {
      const travelRatio = travelHours / (labourHours + travelHours);
      const labourRatio = labourHours / (labourHours + travelHours);
      
      travelOtMinutes += totalOtAfter5 * travelRatio;
      labourOtMinutes += totalOtAfter5 * labourRatio;
    }
  }
  
  // Calculate regular hours after time-based overtime
  const labourRegMinutes = Math.max(0, Math.round(labourHours * 60) - labourOtMinutes);
  const travelRegMinutes = Math.max(0, Math.round(travelHours * 60) - travelOtMinutes);
  
  // Apply 8-hour threshold logic to remaining regular hours
  const totalRegularHours = (labourRegMinutes + travelRegMinutes) / 60;
  const regularHoursLimit = 8.0;
  
  if (totalRegularHours > regularHoursLimit) {
    // Calculate how much overtime to add due to threshold
    const thresholdOtHours = totalRegularHours - regularHoursLimit;
    const thresholdOtMinutes = thresholdOtHours * 60;
    
    // Distribute threshold overtime proportionally
    if (thresholdOtMinutes > 0 && (labourRegMinutes + travelRegMinutes) > 0) {
      const labourRatio = labourRegMinutes / (labourRegMinutes + travelRegMinutes);
      const travelRatio = travelRegMinutes / (labourRegMinutes + travelRegMinutes);
      
      const labourThresholdOt = thresholdOtMinutes * labourRatio;
      const travelThresholdOt = thresholdOtMinutes * travelRatio;
      
      // Update overtime and regular hours
      labourOtMinutes += labourThresholdOt;
      travelOtMinutes += travelThresholdOt;
      
      // Recalculate regular hours
      const finalLabourRegMinutes = Math.max(0, labourRegMinutes - labourThresholdOt);
      const finalTravelRegMinutes = Math.max(0, travelRegMinutes - travelThresholdOt);
      
      return {
        regularLabourHours: finalLabourRegMinutes / 60,
        overtimeLabourHours: labourOtMinutes / 60,
        premiumLabourHours: 0,
        regularTravelHours: finalTravelRegMinutes / 60,
        overtimeTravelHours: travelOtMinutes / 60,
        premiumTravelHours: 0
      };
    }
  }
  
  return {
    regularLabourHours: labourRegMinutes / 60,
    overtimeLabourHours: labourOtMinutes / 60,
    premiumLabourHours: 0,
    regularTravelHours: travelRegMinutes / 60,
    overtimeTravelHours: travelOtMinutes / 60,
    premiumTravelHours: 0
  };
}

function calculateLabourCost(breakdown: any, rates: any): number {
  return (breakdown.regularLabourHours * rates.regularLabour) +
         (breakdown.overtimeLabourHours * rates.overtimeLabour) +
         (breakdown.premiumLabourHours * rates.premiumLabour);
}

function calculateTravelCost(breakdown: any, rates: any): number {
  return (breakdown.regularTravelHours * rates.regularTravel) +
         (breakdown.overtimeTravelHours * rates.overtimeTravel) +
         (breakdown.premiumTravelHours * rates.premiumTravel);
}

function calculateExpenses(
  type: CalculationDayType,
  isFirstDay: boolean,
  isLastDay: boolean,
  labourHours: number,
  travelHours: number,
  rateSheet: RateSheet,
  hotelRequired: boolean,
  rentalCarRequired: boolean,
  travelMethod: string,
  travelDistance: number,
  dailyTravelDistance: number,
  separateTravelTo: boolean,
  separateTravelFrom: boolean
) {
  const mileage = ExpenseCalculator.calculateMileageCost(
    rateSheet.mileageRate, travelDistance, dailyTravelDistance,
    isFirstDay || isLastDay, rentalCarRequired, travelMethod
  );
  
  const hotel = ExpenseCalculator.calculateHotelCost(
    rateSheet.hotelCost, hotelRequired, isLastDay
  );
  
  const rentalCar = ExpenseCalculator.calculateRentalCarCost(
    rateSheet.rentalCarRate, rentalCarRequired
  );
  
  const flight = ExpenseCalculator.calculateFlightCost(
    rateSheet.flightCost, travelMethod,
    type === CalculationDayType.TravelTo || type === CalculationDayType.TravelFrom,
    isFirstDay, isLastDay, separateTravelTo, separateTravelFrom
  );
  
  const perDiem = ExpenseCalculator.calculatePerDiemCost(
    rateSheet.perDiemRate, labourHours, travelHours
  );
  
  return {
    mileage,
    hotel,
    rentalCar,
    flight,
    perDiem,
    total: mileage + hotel + rentalCar + flight + perDiem
  };
}