# Results Tracking System Specification

## Overview
The Results Tracking System is a new component that allows users to track actual vs planned values during commissioning projects. It provides a comprehensive interface for comparing estimated costs and hours against actual performance.

## Workflow

### 1. Planning Phase (QuickEstimator)
- User enters all project parameters in the existing QuickEstimator interface
- All calculations generate the "planned" values for the project
- User clicks new "Track" button to transition to tracking phase

### 2. Tracking Phase (Results Window)
- Results window opens with planned values copied to both "Planned" and "Actual" columns
- Initially, all deltas are zero (Actual = Planned)
- User can edit only the "Actual" columns during commissioning
- Deltas automatically calculate as Actual - Planned
- Color coding: Green for negative (under budget), Red for positive (over budget)

## Data Structure

### Summary Metrics (Top Section)
- **Quoted**: Manually editable, defaults to $0.00
- **Planned**: Sum of all planned values across all resources
- **Forecast**: Sum of all actual values across all resources  
- **Versus Planned**: Forecast - Planned
- **Versus Quoted**: Forecast - Quoted

### Resource Sections
Each resource displays:
- **Header**: "Resource: [Resource Name] ([Total Days on Site] display days)"
- **Date Columns**: Each work day with Planned/Actual/Delta sub-columns
- **Data Rows** (in order):

#### Labour Hours
- Regular Labour
- Overtime Labour
- Premium Labour
- **Subtotal Hours - Labour** (yellow background, bold)

#### Travel Hours
- Regular Travel
- Overtime Travel
- Premium Travel
- **Subtotal Hours - Travel** (yellow background, bold)

#### Subtotals (Monetary)
- **Subtotals - Charges** (yellow background, bold)
- **Subtotals - Expenses** (yellow background, bold)

#### Detailed Expenses (Monetary)
- Mileage
- Per Diem
- Flight
- Car Rental
- Hotel

#### Totals Row
- **Totals** (yellow background, bold) - sums all daily values

### Minimization Feature
When a resource is minimized, only show:
- Resource header
- Date/column headers (Planned/Actual/Delta)
- Totals row

## Technical Implementation

### Component Structure
- **ResultsWindow.tsx**: Main component
- **ResourceTracker.tsx**: Individual resource tracking component
- **TrackingDataManager.ts**: Data persistence and management

### Data Flow
1. QuickEstimator passes current state to ResultsWindow
2. ResultsWindow initializes with planned values in both planned and actual columns
3. User edits actual values
4. Deltas calculate automatically as Actual - Planned
5. Data saves to CSV file in downloads folder

### Delta Calculation Logic
- **Formula**: Delta = Actual - Planned
- **Positive Delta**: Actual > Planned (over budget) = Red text
- **Negative Delta**: Actual < Planned (under budget) = Green text
- **Zero Delta**: Actual = Planned (on budget) = Normal text
- **Real-time Updates**: Deltas recalculate immediately when actual values change

### File Storage
- **Format**: CSV file in user's downloads folder
- **Filename**: `Tracking_Data_[ProjectName]_[Date].csv`
- **Content**: All tracking data including planned, actual, and delta values
- **Persistence**: File remains after browser restart/computer restart

### Color Scheme
- **Background**: Matches QuickEstimator (dark/light mode compatible)
- **Yellow Highlights**: Subtotals and totals rows (`#FFEFFFEF`)
- **Green Text**: Negative deltas (under budget) (`#FF00FF00`)
- **Red Text**: Positive deltas (over budget) (`#FFFF0000`)
- **Headers**: Light blue (`#FFD9E1F2`)
- **Enhanced Colors**: Additional colors for better visual appeal and readability
- **Zero Values**: All zero values should display as "0" or "$0.00", never blank

### Delta Display Rules
- **Positive Deltas**: Red text color (over budget)
- **Negative Deltas**: Green text color (under budget)
- **Zero Deltas**: Normal text color
- **Format**: Hours show as "0", "1", "2", etc. (no decimals unless needed)
- **Currency Format**: Expenses show as "$0.00", "$123.45", etc.
- **No Blank Values**: All cells must show a value, never empty

### Integration Points
- **QuickEstimator**: Passes current resource data and calculations
- **estimatorEngine**: Provides calculation results for planned values
- **DataManager**: May be extended for additional data persistence

## User Interface Elements

### QuickEstimator Integration
- **New Button**: "Track" button added to bottom of QuickEstimator
- **Button Location**: Bottom section with other action buttons
- **Button Style**: Consistent with existing button styling

### Results Window Features
- **Window Title**: "Results - [ProjectName]"
- **Window Size**: Full width of monitor for maximum data visibility
- **Close Button**: Returns to QuickEstimator
- **Minimize Resources**: Collapsible resource sections
- **Editable Cells**: Only actual value columns
- **Auto-calculation**: Deltas update automatically
- **Color Feedback**: Immediate visual feedback on budget performance
- **Zero Value Display**: All zero values show as "0" or "$0.00", never blank

### Data Validation
- **Numeric Input**: Only numbers allowed in actual columns
- **Range Validation**: Prevent negative hours (if applicable)
- **Auto-save**: Save changes automatically to CSV

## Future Enhancements
- Export functionality (CSV/Excel)
- Multiple project tracking
- Historical data comparison
- Reporting and analytics
- Integration with external systems

## File Structure
```
src/
├── components/
│   ├── QuickEstimator.tsx (modified)
│   ├── ResultsWindow.tsx (new)
│   └── ResourceTracker.tsx (new)
├── utils/
│   ├── TrackingDataManager.ts (new)
│   └── estimatorEngine.ts (existing)
└── models/
    └── TrackingData.ts (new)
```

## Data Models

### TrackingData Interface
```typescript
interface TrackingData {
  projectId: string;
  projectName: string;
  quotedAmount: number;
  resources: ResourceTrackingData[];
  lastUpdated: Date;
}

interface ResourceTrackingData {
  resourceId: string;
  resourceName: string;
  days: DayTrackingData[];
  isMinimized: boolean;
}

interface DayTrackingData {
  dayNumber: number;
  date: string;
  planned: DayValues;
  actual: DayValues;
  delta: DayValues;
}

interface DayValues {
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
``` 