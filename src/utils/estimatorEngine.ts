import { CalculationResult, DayDetail, CalculationDayType } from '../models/CalculationResult';
import { RateSheet } from '../models/RateSheet';
import { HourCategorizerUtil } from './HourCategorizerUtil';
import { ExpenseCalculator } from './ExpenseCalculator';
import { DailyHourBreakdown } from '../models/DailyHourBreakdown';
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

export function calculateEstimate(inputs: EstimatorInputs): CalculationResult {
  console.log('ESTIMATOR ENGINE RUNNING v2');
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

  // Discount and emergency logic
  const discountMultiplier = 1 - (discountPercent / 100);
  const getRate = (base: number, emergencyBase: number) => isEmergency ? emergencyBase * discountMultiplier : base * discountMultiplier;

  // Prepare rates
  const regularLabourRate = getRate(rateSheet.regularLabourRate, rateSheet.premiumLabourRate);
  const overtimeLabourRate = getRate(rateSheet.overtimeLabourRate, rateSheet.premiumLabourRate);
  const premiumLabourRate = rateSheet.premiumLabourRate * discountMultiplier;
  const regularTravelRate = getRate(rateSheet.regularTravelRate, rateSheet.premiumTravelRate);
  const overtimeTravelRate = getRate(rateSheet.overtimeTravelRate, rateSheet.premiumTravelRate);
  const premiumTravelRate = rateSheet.premiumTravelRate * discountMultiplier;

  // Track totals
  let totalLabourCost = 0;
  let totalTravelCost = 0;
  let totalExpenses = 0;
  let totalLabourHours = 0;
  let totalTravelHours = 0;

  // Add this line to fix linter errors:
  const dayDetails: DayDetail[] = [];

  // --- NEW SCHEDULE BUILDING LOGIC ---
  // We want to assign the requested number of on-site (work) days, skipping ineligible days,
  // and extend the schedule as needed until the requested number of work days is reached.

  const rawDays: Array<{
    dayNumber: number;
    dayOfWeek: number;
    isTravelToDay: boolean;
    isTravelFromDay: boolean;
    manualOverride?: ManualDayOverride;
    type: CalculationDayType;
    isHoldover: boolean;
  }> = [];

  let currentDayOfWeek = startDayOfWeek;
  let assignedOnSite = 0;
  let dayIndex = 0;
  let totalDays = 0;
  let travelToAdded = false;
  let travelFromAdded = false;
  let pendingWorkDays = daysOnSite;

  // Add travel to day if needed
  if (separateTravelTo) {
    // The travel day should be the day before the selected start day
    const travelToDayOfWeek = (startDayOfWeek + 6) % 7;
    rawDays.push({
      dayNumber: rawDays.length + 1,
      dayOfWeek: travelToDayOfWeek,
      isTravelToDay: true,
      isTravelFromDay: false,
      manualOverride: manualOverrides.get(rawDays.length + 1),
      type: CalculationDayType.TravelTo,
      isHoldover: false
    });
    // Do NOT increment currentDayOfWeek here; the first on-site day should use startDayOfWeek
    travelToAdded = true;
  }

  // Main loop: keep adding days until we've assigned the requested number of on-site days
  // The first on-site day should always use startDayOfWeek
  currentDayOfWeek = startDayOfWeek;
  assignedOnSite = 0;
  while (assignedOnSite < daysOnSite) {
    const dayNumber = rawDays.length + 1;
    const manualOverride = manualOverrides.get(dayNumber);
    let type: CalculationDayType = CalculationDayType.WorkDay;
    let isHoldover = false;
    let isTravelToDay = false;
    let isTravelFromDay = false;
    let dayOfWeek = currentDayOfWeek;

    // Check for manual override
    if (manualOverride) {
      switch (manualOverride.dayType) {
        case DayType.Work:
          type = CalculationDayType.WorkDay;
          break;
        case DayType.Travel:
          // If we haven't added travel from yet and we're at the end, mark as travel from
          type = CalculationDayType.TravelFrom;
          isTravelFromDay = true;
          break;
        case DayType.Holdover:
          type = CalculationDayType.WorkDay;
          isHoldover = true;
          break;
        case DayType.Nil:
          type = CalculationDayType.None;
          break;
      }
    } else {
      // Holdover logic
      if (
        holdoverDayEnabled &&
        ((dayOfWeek + 7) % 7) === holdoverDayOfWeek
      ) {
        isHoldover = true;
      }
      // Exclude weekends if needed
      if ((dayOfWeek === 6 && !includeSaturdays) || (dayOfWeek === 0 && !includeSundays)) {
        type = CalculationDayType.None;
      }
    }

    // Only count as on-site if eligible
    const isEligible =
      type === CalculationDayType.WorkDay &&
      !isHoldover &&
      (!manualOverride || manualOverride.dayType !== DayType.Nil);
    if (isEligible) {
      assignedOnSite++;
    }

    rawDays.push({
      dayNumber,
      dayOfWeek,
      isTravelToDay,
      isTravelFromDay,
      manualOverride,
      type,
      isHoldover
    });
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
    // currentDayOfWeek = (currentDayOfWeek + 1) % 7; // Not needed unless more days follow
    travelFromAdded = true;
  }

  // --- REST OF THE LOGIC UNCHANGED ---
  // Build a list of all eligible day indexes
  const eligibleIndexes: number[] = [];
  for (let i = 0; i < rawDays.length; i++) {
    const d = rawDays[i];
    const isSaturday = d.dayOfWeek === 6;
    const isSunday = d.dayOfWeek === 0;
    const isEligible =
      d.type === CalculationDayType.WorkDay &&
      !d.isHoldover &&
      (!d.manualOverride || d.manualOverride.dayType !== DayType.Nil) &&
      ((includeSaturdays || !isSaturday) && (includeSundays || !isSunday));
    if (isEligible) {
      eligibleIndexes.push(i);
    }
  }
  // Assign on-site status to the first daysOnSite eligible days
  const onSiteSet = new Set(eligibleIndexes.slice(0, daysOnSite));

  // Now build the final dayDetails array with correct on-site assignment
  for (let i = 0; i < rawDays.length; i++) {
    const d = rawDays[i];
    let type = d.type;
    let isHoldover = d.isHoldover;
    let manualOverride = d.manualOverride;
    let dayOfWeek = d.dayOfWeek;
    let dayNumber = d.dayNumber;
    let isOnSite = onSiteSet.has(i);
    // If not on-site, set to no-activity unless it's travel or holdover
    if (!isOnSite && type === CalculationDayType.WorkDay && !isHoldover) {
      type = CalculationDayType.None;
    }
    // Calculate hours
    let labourHours = 0;
    let travelHours = 0;
    if (manualOverride) {
      if (manualOverride.dayType === DayType.Holdover) {
        labourHours = 8;
        travelHours = 0;
      } else {
        labourHours = manualOverride.labourHours;
        travelHours = manualOverride.travelHours;
      }
    } else {
      if (type === CalculationDayType.WorkDay) {
        labourHours = isHoldover ? 8 : hoursPerDay;
        travelHours = isHoldover ? 0 : dailyTravelTime * 2;
      } else if (type === CalculationDayType.TravelTo || type === CalculationDayType.TravelFrom) {
        labourHours = 0;
        travelHours = travelTime;
      }
    }
    // For Nil (No Activity) days, set labour/travel to zero but optionally include expenses
    if (type === CalculationDayType.None) {
      let mileageCost = 0, hotelCost = 0, rentalCarCost = 0, flightCost = 0, perDiem = 0;
      if (!manualOverride || manualOverride.includeExpenses !== false) {
        const isFirstDay = i === 0;
        const isLastDay = i === rawDays.length - 1;
        mileageCost = ExpenseCalculator.calculateMileageCost(
          rateSheet.mileageRate,
          travelDistance,
          dailyTravelDistance,
          isFirstDay || isLastDay,
          rentalCarRequired,
          travelMethod
        );
        hotelCost = ExpenseCalculator.calculateHotelCost(
          rateSheet.hotelCost,
          hotelRequired,
          isLastDay
        );
        rentalCarCost = ExpenseCalculator.calculateRentalCarCost(
          rateSheet.rentalCarRate,
          rentalCarRequired
        );
        flightCost = ExpenseCalculator.calculateFlightCost(
          rateSheet.flightCost,
          travelMethod,
          false, // not a travel day
          isFirstDay,
          isLastDay,
          separateTravelTo,
          separateTravelFrom
        );
        perDiem = rateSheet.perDiemRate;
      }
      const totalDayCost = mileageCost + hotelCost + rentalCarCost + flightCost + perDiem;
      totalExpenses += mileageCost + hotelCost + rentalCarCost + flightCost + perDiem;
      dayDetails.push({
        dayNumber,
        dayOfWeek,
        type,
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
        hotelCost,
        perDiem,
        mileageCost,
        rentalCarCost,
        airfareCost: flightCost,
        totalDayCost
      });
      continue;
    }
    // --- OT BEFORE 7AM/AFTER 5PM LOGIC ---
    let breakdown;
    if (
      otBefore7After5 &&
      type === CalculationDayType.WorkDay &&
      !isHoldover &&
      dayOfWeek >= 1 && dayOfWeek <= 5 // Monday=1, ..., Friday=5
    ) {
      // Determine start time (per-day override or global)
      let startTimeStr = (manualOverride && manualOverride.startTime) ? manualOverride.startTime : (startTimeOnSite ? startTimeOnSite.format ? startTimeOnSite.format('HH:mm') : startTimeOnSite : '08:00');
      let startHour = parseInt(startTimeStr.split(':')[0], 10);
      let startMin = parseInt(startTimeStr.split(':')[1], 10);
      let startMinutes = startHour * 60 + startMin;
      let endMinutes = startMinutes + Math.round(labourHours * 60);
      let otMinutes = 0;
      let regMinutes = 0;
      
      // Overtime before 7:00 AM
      if (startMinutes < 420) { // 420 = 7*60
        let otEnd = Math.min(420, endMinutes);
        otMinutes += otEnd - startMinutes;
      }
      
      // Overtime after 5:00 PM
      if (endMinutes > 1020) { // 1020 = 17*60
        let otStart = Math.max(1020, startMinutes);
        otMinutes += endMinutes - otStart;
      }
      
      // Regular hours are the remaining hours (between 7am and 5pm)
      regMinutes = Math.max(0, endMinutes - startMinutes - otMinutes);
      
      let regHours = regMinutes / 60;
      let otHours = otMinutes / 60;
      
      breakdown = {
        regularLabourHours: regHours,
        overtimeLabourHours: otHours,
        premiumLabourHours: 0,
        regularTravelHours: 0,
        overtimeTravelHours: 0,
        premiumTravelHours: 0
      };
    } else {
      breakdown = HourCategorizerUtil.categorizeDailyHours(
        labourHours,
        travelHours,
        dayOfWeek,
        isEmergency,
        isHoldover
      );
    }
    // Calculate costs
    const labourCost =
      (breakdown.regularLabourHours * regularLabourRate) +
      (breakdown.overtimeLabourHours * overtimeLabourRate) +
      (breakdown.premiumLabourHours * premiumLabourRate);
    const travelCost =
      (breakdown.regularTravelHours * regularTravelRate) +
      (breakdown.overtimeTravelHours * overtimeTravelRate) +
      (breakdown.premiumTravelHours * premiumTravelRate);
    // Expenses
    const isFirstDay = i === 0;
    const isLastDay = i === rawDays.length - 1;
    const mileageCost = ExpenseCalculator.calculateMileageCost(
      rateSheet.mileageRate,
      travelDistance,
      dailyTravelDistance,
      isFirstDay || isLastDay,
      rentalCarRequired,
      travelMethod
    );
    const hotelCost = ExpenseCalculator.calculateHotelCost(
      rateSheet.hotelCost,
      hotelRequired,
      isLastDay
    );
    const rentalCarCost = ExpenseCalculator.calculateRentalCarCost(
      rateSheet.rentalCarRate,
      rentalCarRequired
    );
    const flightCost = ExpenseCalculator.calculateFlightCost(
      rateSheet.flightCost,
      travelMethod,
      type === CalculationDayType.TravelTo || type === CalculationDayType.TravelFrom,
      isFirstDay,
      isLastDay,
      separateTravelTo,
      separateTravelFrom
    );
    const perDiem = ExpenseCalculator.calculatePerDiemCost(
      rateSheet.perDiemRate,
      labourHours,
      travelHours
    );
    const totalDayCost =
      labourCost +
      travelCost +
      mileageCost +
      hotelCost +
      rentalCarCost +
      flightCost +
      perDiem;
    // Aggregate totals
    totalLabourCost += labourCost;
    totalTravelCost += travelCost;
    totalExpenses += mileageCost + hotelCost + rentalCarCost + flightCost + perDiem;
    totalLabourHours += breakdown.regularLabourHours + breakdown.overtimeLabourHours + breakdown.premiumLabourHours;
    totalTravelHours += breakdown.regularTravelHours + breakdown.overtimeTravelHours + breakdown.premiumTravelHours;
    dayDetails.push({
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
      hotelCost,
      perDiem,
      mileageCost,
      rentalCarCost,
      airfareCost: flightCost,
      totalDayCost
    });
  }

  // Add markup to expenses (10%) except mileage and per diem
  const markup = 0.10;
  let markupExpenses = 0;
  dayDetails.forEach(day => {
    markupExpenses += (day.hotelCost + day.rentalCarCost + day.airfareCost) * markup;
  });
  totalExpenses += markupExpenses + otherExpenses;

  const grandTotal = totalLabourCost + totalTravelCost + totalExpenses;

  return {
    totalLabourCost,
    totalTravelCost,
    totalExpenses,
    grandTotal,
    totalDays,
    totalLabourHours,
    totalTravelHours,
    dayDetails
  };
} 