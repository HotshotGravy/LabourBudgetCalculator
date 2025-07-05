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
    manualOverrides = new Map()
  } = inputs;

  // Calculate total days in schedule
  const totalDays = daysOnSite + (separateTravelTo ? 1 : 0) + (separateTravelFrom ? 1 : 0);
  const dayDetails: DayDetail[] = [];

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

  // Build schedule
  for (let i = 0; i < totalDays; i++) {
    const dayNumber = i + 1;
    const manualOverride = manualOverrides.get(dayNumber);
    
    // Determine day type
    let type: CalculationDayType = CalculationDayType.WorkDay;
    let dayIdx = i;
    let isTravelToDay = separateTravelTo && i === 0;
    let isTravelFromDay = separateTravelFrom && i === totalDays - 1;
    let isHoldover = false;
    let rawDayOfWeek = (startDayOfWeek + i - (separateTravelTo ? 1 : 0)) % 7;
    if (rawDayOfWeek < 0) rawDayOfWeek += 7;
    let dayOfWeek = rawDayOfWeek;
    
    // Check for manual override first
    if (manualOverride) {
      // Convert DayType to CalculationDayType
      switch (manualOverride.dayType) {
        case DayType.Work:
          type = CalculationDayType.WorkDay;
          break;
        case DayType.Travel:
          type = isTravelToDay ? CalculationDayType.TravelTo : CalculationDayType.TravelFrom;
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
      // Auto-calculate day type
      if (isTravelToDay) {
        type = CalculationDayType.TravelTo;
        let rawTravelDayOfWeek = (startDayOfWeek + i - 1) % 7;
        if (rawTravelDayOfWeek < 0) rawTravelDayOfWeek += 7;
        dayOfWeek = rawTravelDayOfWeek;
      } else if (isTravelFromDay) {
        type = CalculationDayType.TravelFrom;
      } else if (
        holdoverDayEnabled &&
        ((dayOfWeek + 7) % 7) === holdoverDayOfWeek
      ) {
        isHoldover = true;
      }
    }

    // Calculate hours
    let labourHours = 0;
    let travelHours = 0;
    
    if (manualOverride) {
      // Use manual override values, but enforce holdover rules
      if (manualOverride.dayType === DayType.Holdover) {
        // Holdover days are always 8 hours at regular rate, regardless of manual input
        labourHours = 8;
        travelHours = 0;
      } else {
        labourHours = manualOverride.labourHours;
        travelHours = manualOverride.travelHours;
      }
    } else {
      // Auto-calculate hours
      if (type === CalculationDayType.WorkDay) {
        labourHours = hoursPerDay;
        travelHours = dailyTravelTime * 2;
        if (isHoldover) {
          // Holdover day: 8 hours at regular
          labourHours = 8;
          travelHours = 0;
        }
      } else if (type === CalculationDayType.TravelTo || type === CalculationDayType.TravelFrom) {
        labourHours = 0;
        travelHours = travelTime;
      }
    }

    // For Nil (No Activity) days, set labour/travel to zero but optionally include expenses
    if (type === CalculationDayType.None) {
      // Expenses
      let mileageCost = 0, hotelCost = 0, rentalCarCost = 0, flightCost = 0, perDiem = 0;
      if (!manualOverride || manualOverride.includeExpenses !== false) {
        const isFirstDay = i === 0;
        const isLastDay = i === totalDays - 1;
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

    // Categorize hours
    const breakdown = HourCategorizerUtil.categorizeDailyHours(
      labourHours,
      travelHours,
      dayOfWeek,
      isEmergency,
      isHoldover
    );

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
    const isLastDay = i === totalDays - 1;
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