import { DailyHourBreakdown, DailyHourBreakdownClass } from '../models/DailyHourBreakdown';

export class HourCategorizerUtil {
  static categorizeDailyHours(
    totalLabourHours: number,
    totalTravelHours: number,
    dayOfWeek: number, // 0 = Sunday, 1 = Monday, etc.
    isEmergency: boolean,
    isHoldoverDay: boolean
  ): DailyHourBreakdown {
    const breakdown = new DailyHourBreakdownClass();

    if (totalLabourHours < 0) totalLabourHours = 0;
    if (totalTravelHours < 0) totalTravelHours = 0;

    if (isEmergency) {
      breakdown.premiumLabourHours = totalLabourHours;
      breakdown.premiumTravelHours = totalTravelHours;
    } else if (isHoldoverDay) {
      // Holdover day logic: All hours are typically regular.
      breakdown.regularLabourHours = totalLabourHours;
      breakdown.regularTravelHours = totalTravelHours;
    } else {
      switch (dayOfWeek) {
        case 0: // Sunday
          breakdown.premiumLabourHours = totalLabourHours;
          breakdown.premiumTravelHours = totalTravelHours;
          break;

        case 6: // Saturday
          breakdown.overtimeLabourHours = totalLabourHours;
          breakdown.overtimeTravelHours = totalTravelHours;
          break;

        default: // Weekdays: Monday to Friday
          const combinedHours = totalLabourHours + totalTravelHours;
          const regularHoursLimit = 8.0;

          if (combinedHours <= regularHoursLimit) {
            breakdown.regularLabourHours = totalLabourHours;
            breakdown.regularTravelHours = totalTravelHours;
          } else {
            // Prioritize labour for regular hours
            breakdown.regularLabourHours = Math.min(totalLabourHours, regularHoursLimit);
            const remainingRegularAllowance = regularHoursLimit - breakdown.regularLabourHours;
            breakdown.regularTravelHours = Math.min(totalTravelHours, remainingRegularAllowance);

            // Remaining hours are overtime
            breakdown.overtimeLabourHours = totalLabourHours - breakdown.regularLabourHours;
            breakdown.overtimeTravelHours = totalTravelHours - breakdown.regularTravelHours;
          }
          break;
      }
    }
    return breakdown;
  }
} 