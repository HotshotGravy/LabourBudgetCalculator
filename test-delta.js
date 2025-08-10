// Simple test to verify delta calculation
function calculateDelta(actual, planned) {
  return {
    regularLabour: actual.regularLabour - planned.regularLabour,
    overtimeLabour: actual.overtimeLabour - planned.overtimeLabour,
    premiumLabour: actual.premiumLabour - planned.premiumLabour,
    regularTravel: actual.regularTravel - planned.regularTravel,
    overtimeTravel: actual.overtimeTravel - planned.overtimeTravel,
    premiumTravel: actual.premiumTravel - planned.premiumTravel,
    mileage: actual.mileage - planned.mileage,
    perDiem: actual.perDiem - planned.perDiem,
    flight: actual.flight - planned.flight,
    carRental: actual.carRental - planned.carRental,
    hotel: actual.hotel - planned.hotel
  };
}

function formatValue(value, isCurrency = false) {
  if (value === undefined || value === null) return isCurrency ? '$0.00' : '0';
  if (value === 0) return isCurrency ? '$0.00' : '0';
  const numValue = Number(value);
  if (isNaN(numValue)) return isCurrency ? '$0.00' : '0';
  return isCurrency ? `$${numValue.toFixed(2)}` : numValue.toString();
}

// Test cases
console.log('=== DELTA CALCULATION TEST ===');

// Test Case 1: Planned: 0, Actual: 3, Expected Delta: 3
const planned1 = { regularLabour: 0, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const actual1 = { regularLabour: 3, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const delta1 = calculateDelta(actual1, planned1);
console.log('Test 1 - Planned: 0, Actual: 3');
console.log('Delta:', delta1);
console.log('Formatted Delta:', formatValue(delta1.regularLabour));
console.log('---');

// Test Case 2: Planned: 8, Actual: 11, Expected Delta: 3
const planned2 = { regularLabour: 8, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const actual2 = { regularLabour: 11, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const delta2 = calculateDelta(actual2, planned2);
console.log('Test 2 - Planned: 8, Actual: 11');
console.log('Delta:', delta2);
console.log('Formatted Delta:', formatValue(delta2.regularLabour));
console.log('---');

// Test Case 3: Planned: 0.5, Actual: 3.5, Expected Delta: 3
const planned3 = { regularLabour: 0.5, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const actual3 = { regularLabour: 3.5, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 0, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const delta3 = calculateDelta(actual3, planned3);
console.log('Test 3 - Planned: 0.5, Actual: 3.5');
console.log('Delta:', delta3);
console.log('Formatted Delta:', formatValue(delta3.regularLabour));
console.log('---');

// Test Case 4: Currency test
const planned4 = { regularLabour: 0, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 100, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const actual4 = { regularLabour: 0, overtimeLabour: 0, premiumLabour: 0, regularTravel: 0, overtimeTravel: 0, premiumTravel: 0, mileage: 150, perDiem: 0, flight: 0, carRental: 0, hotel: 0 };
const delta4 = calculateDelta(actual4, planned4);
console.log('Test 4 - Currency - Planned: $100, Actual: $150');
console.log('Delta:', delta4);
console.log('Formatted Delta:', formatValue(delta4.mileage, true));
console.log('---'); 