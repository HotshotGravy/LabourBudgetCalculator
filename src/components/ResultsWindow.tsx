import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  Box,
  Dialog,
  DialogTitle,
  DialogContent,
  Typography,
  Grid,
  Button,
  TextField,
  IconButton,
  Alert,
  Snackbar
} from '@mui/material';
import { ExpandMore, ExpandLess, KeyboardArrowUp, KeyboardArrowDown, Upload as UploadIcon } from '@mui/icons-material';
import ValidationWarningDialog from './ValidationWarningDialog';
import ImportSummaryDialog from './ImportSummaryDialog';
import { ExcelImportManager, ExcelRow, ValidationWarning } from '../utils/ExcelImportManager';
import { ResourceData } from './QuickEstimator';
import { RateSheet } from '../models/RateSheet';
import { ProjectData, TrackingData, DayValues } from '../models/TrackingData';
import { TrackingDataManager } from '../utils/TrackingDataManager';
import dayjs from 'dayjs';
import { NumericFormat } from 'react-number-format';

interface HourInputProps {
  value: number;
  onChange: (value: number) => void;
  darkMode?: boolean;
}

interface BulkEditDialogProps {
  open: boolean;
  onClose: () => void;
  fieldName: string;
  isCurrency: boolean;
  onConfirm: (value: number) => void;
  darkMode?: boolean;
}

interface SaveDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (filename: string) => void;
  defaultFilename: string;
  darkMode?: boolean;
  isSaveAs?: boolean;
}

interface CloseConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  onSave: () => void;
  hasBeenSaved?: boolean;
  filename?: string;
  darkMode?: boolean;
}

const CloseConfirmDialog: React.FC<CloseConfirmDialogProps> = ({
  open,
  onClose,
  onConfirm,
  onSave,
  hasBeenSaved,
  filename,
  darkMode = false
}) => {
  const panelBg = darkMode ? '#2c2f36' : '#fff';
  const panelBorder = darkMode ? '#444' : '#ccc';
  const panelText = darkMode ? '#fff' : '#000';

  const handleSaveClick = () => {
    onSave();
    onClose();
  };

  const handleCloseWithoutSaving = () => {
    onConfirm();
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          bgcolor: panelBg,
          color: panelText,
          border: `1px solid ${panelBorder}`
        }
      }}
    >
      <DialogTitle sx={{ bgcolor: panelBg, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>
        <Typography variant="h6">Confirm Close</Typography>
      </DialogTitle>
      <DialogContent sx={{ bgcolor: panelBg, color: panelText, p: 3 }}>
        <Typography variant="body1" sx={{ mb: 3 }}>
          Close without saving?
        </Typography>
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
          <Button
            variant="outlined"
            onClick={onClose}
            sx={{ color: panelText, borderColor: panelBorder }}
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleCloseWithoutSaving}
            sx={{ 
              bgcolor: darkMode ? '#4fc3f7' : '#1976d2',
              color: '#fff',
              '&:hover': {
                bgcolor: darkMode ? '#29b6f6' : '#1565c0'
              }
            }}
          >
            Yes
          </Button>
        </Box>
      </DialogContent>
    </Dialog>
  );
};

const SaveDialog: React.FC<SaveDialogProps> = ({
  open,
  onClose,
  onSave,
  defaultFilename,
  darkMode = false,
  isSaveAs = false
}) => {
  const [filename, setFilename] = useState(defaultFilename);
  const panelBg = darkMode ? '#2c2f36' : '#fff';
  const panelBorder = darkMode ? '#444' : '#ccc';
  const panelText = darkMode ? '#fff' : '#000';

  const handleSave = () => {
    // Check if filename already exists
    if (TrackingDataManager.checkFilenameExists(filename)) {
      const shouldOverwrite = window.confirm(
        `A file named "${filename}" already exists. Do you want to replace it?`
      );
      if (!shouldOverwrite) {
        return;
      }
    }
    onSave(filename);
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          bgcolor: panelBg,
          color: panelText,
          border: `1px solid ${panelBorder}`
        }
      }}
    >
      <DialogTitle sx={{ bgcolor: panelBg, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>
        <Typography variant="h6">{isSaveAs ? 'Save As' : 'Save Tracking Data'}</Typography>
      </DialogTitle>
      <DialogContent sx={{ bgcolor: panelBg, color: panelText, p: 3 }}>
        <Typography variant="body1" sx={{ mb: 2 }}>
          Choose a filename for your tracking data (.trk):
        </Typography>
        <TextField
          value={filename}
          onChange={(e) => setFilename(e.target.value)}
          fullWidth
          size="small"
          sx={{ mb: 2 }}
          inputProps={{ style: { color: panelText } }}
        />
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
          <Button
            variant="outlined"
            onClick={onClose}
            sx={{ color: panelText, borderColor: panelBorder }}
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleSave}
            sx={{ 
              bgcolor: darkMode ? '#4fc3f7' : '#1976d2',
              color: '#fff',
              '&:hover': {
                bgcolor: darkMode ? '#29b6f6' : '#1565c0'
              }
            }}
          >
            Save
          </Button>
        </Box>
      </DialogContent>
    </Dialog>
  );
};

const BulkEditDialog: React.FC<BulkEditDialogProps> = ({
  open,
  onClose,
  fieldName,
  isCurrency,
  onConfirm,
  darkMode = false
}) => {
  const [value, setValue] = useState<number>(0);
  const panelBg = darkMode ? '#2c2f36' : '#fff';
  const panelBorder = darkMode ? '#444' : '#ccc';
  const panelText = darkMode ? '#fff' : '#000';

  const handleConfirm = () => {
    onConfirm(value);
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          bgcolor: panelBg,
          color: panelText,
          border: `1px solid ${panelBorder}`
        }
      }}
    >
      <DialogTitle sx={{ bgcolor: panelBg, color: panelText, borderBottom: `1px solid ${panelBorder}` }}>
        <Typography variant="h6">Bulk Edit - {fieldName}</Typography>
      </DialogTitle>
      <DialogContent sx={{ bgcolor: panelBg, color: panelText, p: 3 }}>
        <Typography variant="body1" sx={{ mb: 2 }}>
          Set all {fieldName} values to:
        </Typography>
        {isCurrency ? (
          <NumericFormat
            value={value}
            onValueChange={(values) => setValue(values.floatValue || 0)}
            customInput={TextField}
            size="small"
            sx={{ width: '100%', mb: 2 }}
            decimalScale={2}
            allowNegative={false}
            thousandSeparator={true}
            prefix="$"
            fixedDecimalScale={true}
            inputProps={{ style: { textAlign: 'center' } }}
          />
        ) : (
          <NumericFormat
            value={value}
            onValueChange={(values) => setValue(values.floatValue || 0)}
            customInput={TextField}
            size="small"
            sx={{ width: '100%', mb: 2 }}
            decimalScale={1}
            allowNegative={false}
            thousandSeparator={false}
            inputProps={{ style: { textAlign: 'center' } }}
          />
        )}
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
          <Button
            variant="outlined"
            onClick={onClose}
            sx={{ color: panelText, borderColor: panelBorder }}
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleConfirm}
            sx={{ 
              bgcolor: darkMode ? '#4fc3f7' : '#1976d2',
              color: '#fff',
              '&:hover': {
                bgcolor: darkMode ? '#29b6f6' : '#1565c0'
              }
            }}
          >
            Apply to All
          </Button>
        </Box>
      </DialogContent>
    </Dialog>
  );
};

const HourInput: React.FC<HourInputProps> = ({ value, onChange, darkMode = false }) => {
  const handleIncrement = () => {
    onChange(value + 0.5);
  };

  const handleDecrement = () => {
    onChange(Math.max(0, value - 0.5));
  };

  return (
    <Box sx={{ 
      display: 'flex', 
      alignItems: 'center', 
      justifyContent: 'center',
      gap: 0.5,
      width: '100%'
    }}>
      <IconButton
        size="small"
        onClick={handleDecrement}
        sx={{ 
          color: '#fff',
          padding: '2px',
          minWidth: '20px',
          height: '20px'
        }}
      >
        <KeyboardArrowDown fontSize="small" />
      </IconButton>
      <NumericFormat
        value={value}
        onValueChange={(values) => onChange(values.floatValue || 0)}
        customInput={TextField}
        size="small"
        sx={{ 
          width: '60px',
          '& .MuiInputBase-input': {
            textAlign: 'center',
            padding: '4px 8px',
            fontSize: '14px',
            color: value === 0 ? '#888' : '#fff'
          }
        }}
        decimalScale={1}
        allowNegative={false}
        thousandSeparator={false}
        inputProps={{ style: { textAlign: 'center' } }}
      />
      <IconButton
        size="small"
        onClick={handleIncrement}
        sx={{ 
          color: '#fff',
          padding: '2px',
          minWidth: '20px',
          height: '20px'
        }}
      >
        <KeyboardArrowUp fontSize="small" />
      </IconButton>
    </Box>
  );
};

// Memoized sub-components for better performance
const MemoizedHourInput = React.memo<HourInputProps>(({ value, onChange, darkMode = false }) => {
  const handleIncrement = useCallback(() => {
    onChange(value + 0.5);
  }, [value, onChange]);

  const handleDecrement = useCallback(() => {
    onChange(Math.max(0, value - 0.5));
  }, [value, onChange]);

  return (
    <Box sx={{ 
      display: 'flex', 
      alignItems: 'center', 
      justifyContent: 'center',
      gap: 0.5,
      width: '100%'
    }}>
      <IconButton
        size="small"
        onClick={handleDecrement}
        sx={{ 
          color: '#fff',
          padding: '2px',
          minWidth: '20px',
          height: '20px'
        }}
      >
        <KeyboardArrowDown fontSize="small" />
      </IconButton>
      <NumericFormat
        value={value}
        onValueChange={(values) => onChange(values.floatValue || 0)}
        customInput={TextField}
        size="small"
        sx={{ 
          width: '60px',
          '& .MuiInputBase-input': {
            textAlign: 'center',
            padding: '4px 8px',
            fontSize: '14px',
            color: value === 0 ? '#888' : '#fff'
          }
        }}
        decimalScale={1}
        allowNegative={false}
        thousandSeparator={false}
        inputProps={{ style: { textAlign: 'center' } }}
      />
      <IconButton
        size="small"
        onClick={handleIncrement}
        sx={{ 
          color: '#fff',
          padding: '2px',
          minWidth: '20px',
          height: '20px'
        }}
      >
        <KeyboardArrowUp fontSize="small" />
      </IconButton>
    </Box>
  );
});

const MemoizedCurrencyInput = React.memo<{
  value: number;
  onChange: (value: number) => void;
  fieldName: string;
}>(({ value, onChange, fieldName }) => {
  return (
    <NumericFormat
      value={value ?? 0}
      onValueChange={(values) => onChange(values.floatValue || 0)}
      customInput={TextField}
      size="small"
      sx={{ 
        width: '100%',
        '& .MuiInputBase-input': {
          color: (value ?? 0) === 0 ? '#888' : '#fff'
        }
      }}
      decimalScale={2}
      allowNegative={false}
      thousandSeparator={true}
      prefix="$"
      fixedDecimalScale={true}
      inputProps={{ style: { textAlign: 'center' } }}
    />
  );
});

// Memoized cell component for better performance
const MemoizedDayCell = React.memo<{
  day: any;
  resourceIndex: number;
  fieldName: keyof DayValues;
  isCurrency: boolean;
  onValueChange: (resourceIndex: number, dayIndex: number, field: keyof DayValues, value: number) => void;
  darkMode: boolean;
  getDayIndex: (day: any) => number;
}>(({ day, resourceIndex, fieldName, isCurrency, onValueChange, darkMode, getDayIndex }) => {
  const handleChange = useCallback((value: number) => {
    onValueChange(resourceIndex, getDayIndex(day), fieldName, value);
  }, [resourceIndex, getDayIndex, day, fieldName, onValueChange]);

  if (isCurrency) {
    return (
      <MemoizedCurrencyInput
        value={day.actual[fieldName] ?? 0}
        onChange={handleChange}
        fieldName={fieldName}
      />
    );
  }

  return (
    <MemoizedHourInput
      value={day.actual[fieldName] ?? 0}
      onChange={handleChange}
      darkMode={darkMode}
    />
  );
});

// Memoized resource totals calculation
const useResourceTotals = (resource: any) => {
  return useMemo(() => {
    const totals = {
      planned: {
        regularLabour: 0,
        overtimeLabour: 0,
        premiumLabour: 0,
        regularTravel: 0,
        overtimeTravel: 0,
        premiumTravel: 0,
        mileage: 0,
        perDiem: 0,
        flight: 0,
        carRental: 0,
        hotel: 0
      },
      actual: {
        regularLabour: 0,
        overtimeLabour: 0,
        premiumLabour: 0,
        regularTravel: 0,
        overtimeTravel: 0,
        premiumTravel: 0,
        mileage: 0,
        perDiem: 0,
        flight: 0,
        carRental: 0,
        hotel: 0
      },
      delta: {
        regularLabour: 0,
        overtimeLabour: 0,
        premiumLabour: 0,
        regularTravel: 0,
        overtimeTravel: 0,
        premiumTravel: 0,
        mileage: 0,
        perDiem: 0,
        flight: 0,
        carRental: 0,
        hotel: 0
      }
    };

    resource.days.forEach((day: any) => {
      // Sum planned values
      totals.planned.regularLabour += day.planned.regularLabour || 0;
      totals.planned.overtimeLabour += day.planned.overtimeLabour || 0;
      totals.planned.premiumLabour += day.planned.premiumLabour || 0;
      totals.planned.regularTravel += day.planned.regularTravel || 0;
      totals.planned.overtimeTravel += day.planned.overtimeTravel || 0;
      totals.planned.premiumTravel += day.planned.premiumTravel || 0;
      totals.planned.mileage += day.planned.mileage || 0;
      totals.planned.perDiem += day.planned.perDiem || 0;
      totals.planned.flight += day.planned.flight || 0;
      totals.planned.carRental += day.planned.carRental || 0;
      totals.planned.hotel += day.planned.hotel || 0;

      // Sum actual values
      totals.actual.regularLabour += day.actual.regularLabour || 0;
      totals.actual.overtimeLabour += day.actual.overtimeLabour || 0;
      totals.actual.premiumLabour += day.actual.premiumLabour || 0;
      totals.actual.regularTravel += day.actual.regularTravel || 0;
      totals.actual.overtimeTravel += day.actual.overtimeTravel || 0;
      totals.actual.premiumTravel += day.actual.premiumTravel || 0;
      totals.actual.mileage += day.actual.mileage || 0;
      totals.actual.perDiem += day.actual.perDiem || 0;
      totals.actual.flight += day.actual.flight || 0;
      totals.actual.carRental += day.actual.carRental || 0;
      totals.actual.hotel += day.actual.hotel || 0;

      // Sum delta values
      totals.delta.regularLabour += day.delta.regularLabour || 0;
      totals.delta.overtimeLabour += day.delta.overtimeLabour || 0;
      totals.delta.premiumLabour += day.delta.premiumLabour || 0;
      totals.delta.regularTravel += day.delta.regularTravel || 0;
      totals.delta.overtimeTravel += day.delta.overtimeTravel || 0;
      totals.delta.premiumTravel += day.delta.premiumTravel || 0;
      totals.delta.mileage += day.delta.mileage || 0;
      totals.delta.perDiem += day.delta.perDiem || 0;
      totals.delta.flight += day.delta.flight || 0;
      totals.delta.carRental += day.delta.carRental || 0;
      totals.delta.hotel += day.delta.hotel || 0;
    });

    return totals;
  }, [resource.days]);
};

// Memoized rate calculations
const useRateCalculations = (rateSheets: RateSheet[]) => {
  return useMemo(() => {
    const rateSheet = rateSheets[0];
    return {
      regularLabourRate: rateSheet.regularLabourRate || 0,
      overtimeLabourRate: rateSheet.overtimeLabourRate || 0,
      premiumLabourRate: rateSheet.premiumLabourRate || 0,
      regularTravelRate: rateSheet.regularTravelRate || 0,
      overtimeTravelRate: rateSheet.overtimeTravelRate || 0,
      premiumTravelRate: rateSheet.premiumTravelRate || 0
    };
  }, [rateSheets]);
};

// Memoized format value function
const useFormatValue = () => {
  return useCallback((value: number | undefined, isCurrency: boolean = false): string => {
    if (value === undefined || value === null) {
      return isCurrency ? '$0.00' : '0';
    }
    if (value === 0) {
      return isCurrency ? '$0.00' : '0';
    }
    const numValue = Number(value);
    if (isNaN(numValue)) {
      return isCurrency ? '$0.00' : '0';
    }
    return isCurrency 
      ? `$${numValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
      : numValue.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }, []);
};

interface ResultsWindowProps {
  open: boolean;
  onClose: () => void;
  resources: ResourceData[];
  rateSheets: RateSheet[];
  projectData: ProjectData;
  startDate: dayjs.Dayjs | null;
  endDate: dayjs.Dayjs | null;
  darkMode?: boolean;
  trackingData?: TrackingData | null;
  loadedFilename?: string | null;
}

export const ResultsWindow: React.FC<ResultsWindowProps> = ({
  open,
  onClose,
  resources,
  rateSheets,
  projectData,
  startDate,
  endDate,
  darkMode = false,
  trackingData: initialTrackingData = null,
  loadedFilename = null
}) => {
  const [trackingData, setTrackingData] = useState<TrackingData | null>(null);
  const [summaryMetrics, setSummaryMetrics] = useState({
    planned: 0,
    forecast: 0,
    versusPlanned: 0,
    versusQuoted: 0
  });
  const [bulkEditDialogOpen, setBulkEditDialogOpen] = useState(false);
  const [bulkEditFieldName, setBulkEditFieldName] = useState('');
  const [bulkEditIsCurrency, setBulkEditIsCurrency] = useState(false);
  const [bulkEditOnConfirm, setBulkEditOnConfirm] = useState<((value: number) => void) | null>(null);
  const [saveDialogOpen, setSaveDialogOpen] = useState(false);
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false);
  const [currentFilename, setCurrentFilename] = useState<string | null>(null);
  const [defaultSaveFilename, setDefaultSaveFilename] = useState('tracking_data.trk');
  const [closeConfirmDialogOpen, setCloseConfirmDialogOpen] = useState(false);
  
  // Import-related state
  const [validationWarningDialogOpen, setValidationWarningDialogOpen] = useState(false);
  const [importSummaryDialogOpen, setImportSummaryDialogOpen] = useState(false);
  const [validationWarnings, setValidationWarnings] = useState<ValidationWarning[]>([]);
  const [importResult, setImportResult] = useState({
    processedRows: 0,
    skippedRows: 0,
    errors: [] as string[],
    validationWarnings: [] as string[]
  });
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error' | 'warning' | 'info'>('info');
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Memoized color theme for better performance
  const colorTheme = useMemo(() => ({
    panelBg: darkMode ? '#2c2f36' : '#fff',
    panelBorder: darkMode ? '#444' : '#ccc',
    panelText: darkMode ? '#fff' : '#000',
    greenText: '#00FF00',
    redText: '#FF0000',
    blueAccent: darkMode ? '#4fc3f7' : '#1976d2',
    purpleAccent: darkMode ? '#ab47bc' : '#7b1fa2',
    orangeAccent: darkMode ? '#ff9800' : '#f57c00',
    tealAccent: darkMode ? '#26a69a' : '#00796b',
    headerBg: darkMode ? '#33384d' : '#e0e0e0',
    hoursSubtotalBg: darkMode ? '#2a2a3a' : '#d0d8e0',
    chargesSubtotalBg: darkMode ? '#2a3a3a' : '#d0e0e8',
    totalsBg: darkMode ? '#1a2a3a' : '#b0c0d0'
  }), [darkMode]);

  // Memoized rate calculations
  const rateCalculations = useRateCalculations(rateSheets);
  
  // Memoized format value function
  const formatValue = useFormatValue();
  
  const updateSummaryMetrics = useCallback((data: TrackingData) => {
    const metrics = TrackingDataManager.calculateSummaryMetrics(data, rateSheets[0]);
    setSummaryMetrics(metrics);
  }, [rateSheets]);

  // Initialize tracking data when component opens
  useEffect(() => {
    if (open) {
      if (initialTrackingData) {
        // Use provided tracking data, but ensure all resources are minimized
        const minimizedTrackingData = {
          ...initialTrackingData,
          resources: initialTrackingData.resources.map(resource => ({
            ...resource,
            isMinimized: true
          }))
        };
        setTrackingData(minimizedTrackingData);
        updateSummaryMetrics(minimizedTrackingData);
        // If this is loaded data, we don't have a current filename
        setCurrentFilename(loadedFilename || null);
      } else if (!trackingData) {
        // Debug: Log the resources being passed to initializeTrackingData
        console.log('Resources being passed to initializeTrackingData:', 
          resources.map(r => ({ name: r.name, startDay: r.startDay, id: r.id }))
        );
        
        // Initialize new tracking data with all resources minimized
        const newTrackingData = TrackingDataManager.initializeTrackingData(
          resources,
          rateSheets,
          projectData,
          startDate,
          endDate
        );
        // Set all resources as minimized by default
        const minimizedTrackingData = {
          ...newTrackingData,
          resources: newTrackingData.resources.map(resource => ({
            ...resource,
            isMinimized: true
          }))
        };
        setTrackingData(minimizedTrackingData);
        updateSummaryMetrics(minimizedTrackingData);
        // New tracking data, no current filename
        setCurrentFilename(null);
      }
    }
  }, [open, resources, rateSheets, projectData, startDate, endDate, updateSummaryMetrics, initialTrackingData, loadedFilename]);

  // Debounced value change handler for better performance
  const handleActualValueChange = useCallback((
    resourceIndex: number,
    dayIndex: number,
    field: keyof DayValues,
    value: number
  ) => {
    if (!trackingData) return;
    // Always store with 2 decimal places for currency fields
    const isExpenseField = [
      'mileage', 'perDiem', 'flight', 'carRental', 'hotel'
    ].includes(field);
    const roundedValue = isExpenseField ? Number(Number(value).toFixed(2)) : value;
    const newTrackingData = { ...trackingData };
    newTrackingData.resources[resourceIndex].days[dayIndex].actual[field] = roundedValue;
    // Recompute deltas
    newTrackingData.resources[resourceIndex].days[dayIndex].delta = computeDelta(
      newTrackingData.resources[resourceIndex].days[dayIndex].actual,
      newTrackingData.resources[resourceIndex].days[dayIndex].planned
    );
    setTrackingData(newTrackingData);
    updateSummaryMetrics(newTrackingData);
  }, [trackingData, updateSummaryMetrics]);





  // Computed delta calculation function
  const computeDelta = (actual: DayValues, planned: DayValues): DayValues => {
    const delta = {
      regularLabour: (actual.regularLabour ?? 0) - (planned.regularLabour ?? 0),
      overtimeLabour: (actual.overtimeLabour ?? 0) - (planned.overtimeLabour ?? 0),
      premiumLabour: (actual.premiumLabour ?? 0) - (planned.premiumLabour ?? 0),
      regularTravel: (actual.regularTravel ?? 0) - (planned.regularTravel ?? 0),
      overtimeTravel: (actual.overtimeTravel ?? 0) - (planned.overtimeTravel ?? 0),
      premiumTravel: (actual.premiumTravel ?? 0) - (planned.premiumTravel ?? 0),
      mileage: (actual.mileage ?? 0) - (planned.mileage ?? 0),
      perDiem: (actual.perDiem ?? 0) - (planned.perDiem ?? 0),
      flight: (actual.flight ?? 0) - (planned.flight ?? 0),
      carRental: (actual.carRental ?? 0) - (planned.carRental ?? 0),
      hotel: (actual.hotel ?? 0) - (planned.hotel ?? 0)
    };
    
    return delta;
  };



  const handleQuotedAmountChange = (value: number) => {
    if (!trackingData) return;

    const newTrackingData = { ...trackingData, quotedAmount: value };
    setTrackingData(newTrackingData);
    updateSummaryMetrics(newTrackingData);
  };

  const toggleResourceMinimized = (resourceIndex: number) => {
    if (!trackingData) return;

    const newTrackingData = { ...trackingData };
    newTrackingData.resources[resourceIndex].isMinimized = !newTrackingData.resources[resourceIndex].isMinimized;
    setTrackingData(newTrackingData);
  };

  const handleExportCSV = () => {
    if (trackingData) {
      TrackingDataManager.saveToCSV(trackingData);
    }
  };

  // Import handlers
  const handleImportData = () => {
    fileInputRef.current?.click();
  };

  const processExcelFile = async (file: File) => {
    try {
      // Parse Excel file
      const excelRows = await ExcelImportManager.parseExcelFile(file);
      
      if (!trackingData) {
        showSnackbar('No tracking data available for import', 'error');
        return;
      }

      // Process the data
      const warnings: ValidationWarning[] = [];
      const updates = ExcelImportManager.mapExcelDataToUpdates(excelRows, trackingData, warnings);
      
      // Set row numbers for warnings
      warnings.forEach((warning, index) => {
        warning.row = index + 2; // +2 for header row and 1-based indexing
      });

      if (warnings.length > 0) {
        // Show validation warning dialog
        setValidationWarnings(warnings);
        setValidationWarningDialogOpen(true);
      } else {
        // Apply updates directly
        await applyImportUpdates(updates, excelRows.length, 0);
      }

    } catch (error) {
      showSnackbar(`Import failed: ${(error as Error).message}`, 'error');
    }
  };

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    // Reset file input
    event.target.value = '';

    await processExcelFile(file);
  };

  const handleValidationWarningConfirm = async (updatedWarnings: ValidationWarning[]) => {
    setValidationWarningDialogOpen(false);
    
    if (!trackingData) return;

    try {
      // Re-process with user choices
      const excelRows: ExcelRow[] = []; // This would need to be stored from the original import
      const warnings: ValidationWarning[] = [];
      const updates = ExcelImportManager.mapExcelDataToUpdates(excelRows, trackingData, warnings);
      
      // Apply user choices to updates
      // This is a simplified version - in practice, you'd need to re-process the original data
      // with the user's choices applied
      
      await applyImportUpdates(updates, excelRows.length, updatedWarnings.length);
      
    } catch (error) {
      showSnackbar(`Import failed: ${(error as Error).message}`, 'error');
    }
  };

  const applyImportUpdates = async (
    updates: { [resourceId: string]: { [date: string]: any } },
    totalRows: number,
    warningCount: number
  ) => {
    if (!trackingData) return;

    try {
      // Apply updates to tracking data
      const updatedTrackingData = ExcelImportManager.applyUpdatesToTrackingData(trackingData, updates);
      
      // Calculate processed vs skipped rows
      const processedRows = Object.values(updates).reduce((sum, resourceUpdates) => 
        sum + Object.keys(resourceUpdates).length, 0);
      const skippedRows = totalRows - processedRows;

      // Update state
      setTrackingData(updatedTrackingData);
      updateSummaryMetrics(updatedTrackingData);

      // Show summary
      setImportResult({
        processedRows,
        skippedRows,
        errors: [],
        validationWarnings: Array(warningCount).fill('Validation warning resolved')
      });
      setImportSummaryDialogOpen(true);

      showSnackbar(`Import completed: ${processedRows} rows processed, ${skippedRows} skipped`, 'success');

    } catch (error) {
      showSnackbar(`Import failed: ${(error as Error).message}`, 'error');
    }
  };

  const showSnackbar = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  const handleDragOver = (event: React.DragEvent) => {
    event.preventDefault();
    event.stopPropagation();
  };

  const handleDrop = async (event: React.DragEvent) => {
    event.preventDefault();
    event.stopPropagation();

    const files = Array.from(event.dataTransfer.files);
    const excelFile = files.find(file => 
      file.name.toLowerCase().endsWith('.xls') || 
      file.name.toLowerCase().endsWith('.xlsx')
    );

    if (!excelFile) {
      showSnackbar('Please drop an Excel file (.xls or .xlsx)', 'warning');
      return;
    }

    await processExcelFile(excelFile);
  };

  const handleBulkEdit = (fieldName: string, isCurrency: boolean) => {
    setBulkEditFieldName(fieldName);
    setBulkEditIsCurrency(isCurrency);
    setBulkEditOnConfirm(() => (value: number) => {
      if (trackingData) {
        const newTrackingData = { ...trackingData };
        const fieldNameMap: { [key: string]: keyof DayValues } = {
          'Regular Labour': 'regularLabour',
          'Overtime Labour': 'overtimeLabour',
          'Premium Labour': 'premiumLabour',
          'Regular Travel': 'regularTravel',
          'Overtime Travel': 'overtimeTravel',
          'Premium Travel': 'premiumTravel',
          'Mileage': 'mileage',
          'Per Diem': 'perDiem',
          'Flight': 'flight',
          'Car Rental': 'carRental',
          'Hotel': 'hotel'
        };
        
        const fieldKey = fieldNameMap[fieldName];
        if (fieldKey) {
          newTrackingData.resources.forEach(resource => {
            resource.days.forEach(day => {
              day.actual[fieldKey] = value;
              day.delta[fieldKey] = computeDelta(day.actual, day.planned)[fieldKey];
            });
          });
          setTrackingData(newTrackingData);
          updateSummaryMetrics(newTrackingData);
        }
      }
      setBulkEditDialogOpen(false);
    });
    setBulkEditDialogOpen(true);
  };

  const handleSaveTrackingData = async (filename: string) => {
    if (trackingData) {
      try {
        // Ensure filename has .trk extension
        const filenameWithExtension = filename.endsWith('.trk') ? filename : `${filename}.trk`;
        await TrackingDataManager.saveTrackingData(trackingData, projectData, filenameWithExtension);
        setCurrentFilename(filenameWithExtension);
        alert('Tracking data saved successfully!');
      } catch (error) {
        alert('Failed to save tracking data: ' + (error as Error).message);
      }
    }
  };

  const handleSave = async () => {
    if (currentFilename) {
      // Save to current file
      await handleSaveTrackingData(currentFilename);
    } else {
      // No current file, open save dialog
      setDefaultSaveFilename(getDefaultTrackingFilename());
      setSaveDialogOpen(true);
    }
  };

  const handleSaveAs = () => {
    setDefaultSaveFilename(getDefaultTrackingFilename());
    setSaveAsDialogOpen(true);
  };

  const getDefaultTrackingFilename = () => {
    const projectNumber = projectData.projectNumber?.trim() || 'Unknown';
    const customer = projectData.customer?.trim() || 'Unknown';
    const description = projectData.projectDescription?.trim() || 'Project';
    const today = dayjs().format('YYYY-MM-DD');
    return `${projectNumber} ${customer} ${description}-Tracking-${today}.trk`;
  };

  // Horizontal scroll handlers
  const handleWheel = (e: React.WheelEvent) => {
    // Allow horizontal scrolling with Shift + wheel
    if (e.shiftKey) {
      e.preventDefault();
      const target = e.currentTarget as HTMLElement;
      const scrollContainer = target.querySelector('[data-scroll-container]') as HTMLElement;
      if (scrollContainer) {
        scrollContainer.scrollLeft += e.deltaY;
      }
    }
  };

  // Arrow navigation state
  const [arrowStates, setArrowStates] = useState<{ [key: number]: { left: boolean; right: boolean } }>({});
  const [activeResourceIndex, setActiveResourceIndex] = useState<number | null>(null);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    console.log('Mouse move event triggered');
    
    // Find the resource section by looking up the DOM tree
    let target = e.target as HTMLElement;
    while (target && !target.hasAttribute('data-resource-section')) {
      target = target.parentElement as HTMLElement;
    }
    
    if (!target) {
      console.log('No resource section found');
      return;
    }
    
    console.log('Found resource section:', target);
    
    // Get the resource index from the current target
    const resourceIndex = parseInt(target.getAttribute('data-resource-index') || '0');
    console.log('Mouse move - resourceIndex:', resourceIndex);
    setActiveResourceIndex(resourceIndex);
    
    // Find the scroll container within this resource section
    const scrollContainer = target.querySelector('[data-scroll-container]') as HTMLElement;
    if (!scrollContainer) {
      console.log('No scroll container found');
      return;
    }
    
    const scrollRect = scrollContainer.getBoundingClientRect();
    const x = e.clientX - scrollRect.left;
    const width = scrollRect.width;
    
    console.log('Mouse position - x:', x, 'width:', width);
    
    // Show arrows when mouse is within 50px of scrollable content edges
    const edgeThreshold = 50;
    const showLeft = x < edgeThreshold;
    const showRight = x > width - edgeThreshold;
    
    console.log('Show left:', showLeft, 'Show right:', showRight);
    
    setArrowStates(prev => ({
      ...prev,
      [resourceIndex]: {
        left: showLeft,
        right: showRight
      }
    }));
  }, []);
  
  // Throttled mouse move handler for better performance
  const throttledMouseMove = useCallback(
    (() => {
      let timeoutId: NodeJS.Timeout | null = null;
      return (e: React.MouseEvent) => {
        if (timeoutId) return;
        timeoutId = setTimeout(() => {
          timeoutId = null;
          handleMouseMove(e);
        }, 16); // ~60fps
      };
    })(),
    [handleMouseMove]
  );

  const handleMouseLeave = () => {
    setArrowStates({});
  };

  const handleArrowClick = (direction: 'left' | 'right') => {
    console.log('Arrow clicked:', direction, 'activeResourceIndex:', activeResourceIndex);
    
    if (activeResourceIndex !== null) {
      const resourceSection = document.querySelector(`[data-resource-section][data-resource-index="${activeResourceIndex}"]`);
      console.log('Resource section found:', !!resourceSection);
      
      if (!resourceSection) return;
      
      const scrollContainer = resourceSection.querySelector('[data-scroll-container]') as HTMLElement;
      console.log('Scroll container found:', !!scrollContainer);
      
      if (!scrollContainer) return;
      
      const scrollAmount = 420; // 420px as requested
      const currentScroll = scrollContainer.scrollLeft;
      const maxScroll = scrollContainer.scrollWidth - scrollContainer.clientWidth;
      
      console.log('Current scroll:', currentScroll, 'Max scroll:', maxScroll);
      
      let targetScroll;
      if (direction === 'left') {
        targetScroll = Math.max(0, currentScroll - scrollAmount);
      } else {
        targetScroll = Math.min(maxScroll, currentScroll + scrollAmount);
      }
      
      console.log('Target scroll:', targetScroll);
      
      // Smooth scroll animation
      scrollContainer.scrollTo({
        left: targetScroll,
        behavior: 'smooth'
      });
    }
  };

  if (!trackingData) return null;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth={false}
      fullWidth
      PaperProps={{
        sx: {
          bgcolor: colorTheme.panelBg,
          color: colorTheme.panelText,
          minHeight: '95vh',
          width: '95vw',
          maxWidth: '95vw',
          display: 'flex',
          flexDirection: 'column'
        }
      }}
    >
      <DialogTitle sx={{ bgcolor: colorTheme.panelBg, color: colorTheme.panelText, borderBottom: `1px solid ${colorTheme.panelBorder}` }}>
        <Typography variant="h6">Tracking - {projectData.projectNumber} {projectData.customer} - {projectData.projectDescription}</Typography>
      </DialogTitle>

      {/* Sticky Summary Metrics Header */}
      <Box sx={{ 
        bgcolor: colorTheme.panelBg, 
        color: colorTheme.panelText, 
        p: 2, 
        borderBottom: `1px solid ${colorTheme.panelBorder}`,
        position: 'sticky',
        top: 0,
        zIndex: 20
      }}>
        <Grid container spacing={2}>
          <Grid item xs={2.4}>
            <Box sx={{ 
              bgcolor: colorTheme.panelBg, 
              border: `2px solid ${colorTheme.purpleAccent}`, 
              borderRadius: 1, 
              p: 2, 
              textAlign: 'center',
              height: '120px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            }}>
              <Typography variant="subtitle2" sx={{ color: '#fff', mb: 1 }}>Quoted</Typography>
              <NumericFormat
                value={trackingData.quotedAmount}
                onValueChange={(values) => handleQuotedAmountChange(values.floatValue || 0)}
                customInput={TextField}
                size="small"
                sx={{ width: '100%' }}
                decimalScale={2}
                allowNegative={false}
                thousandSeparator={true}
                prefix="$"
                fixedDecimalScale={true}
                inputProps={{ style: { textAlign: 'center' } }}
                InputProps={{
                  sx: { fontSize: '1.2rem', fontWeight: 'bold', color: '#fff' }
                }}
              />
            </Box>
          </Grid>
          <Grid item xs={2.4}>
            <Box sx={{ 
              bgcolor: colorTheme.panelBg, 
              border: `2px solid ${colorTheme.blueAccent}`, 
              borderRadius: 1, 
              p: 2, 
              textAlign: 'center',
              height: '120px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            }}>
              <Typography variant="subtitle2" sx={{ color: '#fff', mb: 1 }}>Planned</Typography>
              <Typography variant="h6" sx={{ color: '#fff', fontWeight: 'bold' }}>
                ${summaryMetrics.planned.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={2.4}>
            <Box sx={{ 
              bgcolor: colorTheme.panelBg, 
              border: `2px solid ${colorTheme.tealAccent}`, 
              borderRadius: 1, 
              p: 2, 
              textAlign: 'center',
              height: '120px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            }}>
              <Typography variant="subtitle2" sx={{ color: '#fff', mb: 1 }}>Forecast</Typography>
              <Typography variant="h6" sx={{ color: '#fff', fontWeight: 'bold' }}>
                ${summaryMetrics.forecast.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={2.4}>
            <Box sx={{ 
              bgcolor: colorTheme.panelBg, 
              border: `2px solid ${colorTheme.orangeAccent}`, 
              borderRadius: 1, 
              p: 2, 
              textAlign: 'center',
              height: '120px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            }}>
              <Typography variant="subtitle2" sx={{ color: '#fff', mb: 1 }}>Versus Planned</Typography>
              <Typography variant="h6" sx={{ 
                color: summaryMetrics.versusPlanned > 0 ? colorTheme.redText : colorTheme.greenText, 
                fontWeight: 'bold' 
              }}>
                ${summaryMetrics.versusPlanned.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={2.4}>
            <Box sx={{ 
              bgcolor: colorTheme.panelBg, 
              border: `2px solid #FFD700`, 
              borderRadius: 1, 
              p: 2, 
              textAlign: 'center',
              height: '120px',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            }}>
              <Typography variant="subtitle2" sx={{ color: '#fff', mb: 1 }}>Versus Quoted</Typography>
              <Typography variant="h6" sx={{ 
                color: summaryMetrics.versusQuoted > 0 ? colorTheme.redText : colorTheme.greenText, 
                fontWeight: 'bold' 
              }}>
                ${summaryMetrics.versusQuoted.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Box>

      {/* Scrollable Content */}
      <DialogContent 
        onWheel={handleWheel}
        onDragOver={handleDragOver}
        onDrop={handleDrop}
        sx={{ 
          bgcolor: colorTheme.panelBg, 
          color: colorTheme.panelText, 
          p: 2, 
          flex: 1,
          overflow: 'auto',
          position: 'relative'
        }}
      >

        {/* Resource Sections */}
        {trackingData.resources.map((resource, resourceIndex) => {
          // Calculate resource totals inline to avoid hook rule violation
                      const resourceTotals = (() => {
              const planned = {
                regularLabour: resource.days.reduce((sum, day) => sum + (day.planned.regularLabour || 0), 0),
                overtimeLabour: resource.days.reduce((sum, day) => sum + (day.planned.overtimeLabour || 0), 0),
                premiumLabour: resource.days.reduce((sum, day) => sum + (day.planned.premiumLabour || 0), 0),
                regularTravel: resource.days.reduce((sum, day) => sum + (day.planned.regularTravel || 0), 0),
                overtimeTravel: resource.days.reduce((sum, day) => sum + (day.planned.overtimeTravel || 0), 0),
                premiumTravel: resource.days.reduce((sum, day) => sum + (day.planned.premiumTravel || 0), 0),
                mileage: resource.days.reduce((sum, day) => sum + (day.planned.mileage || 0), 0),
                perDiem: resource.days.reduce((sum, day) => sum + (day.planned.perDiem || 0), 0),
                flight: resource.days.reduce((sum, day) => sum + (day.planned.flight || 0), 0),
                carRental: resource.days.reduce((sum, day) => sum + (day.planned.carRental || 0), 0),
                hotel: resource.days.reduce((sum, day) => sum + (day.planned.hotel || 0), 0)
              };
              const actual = {
                regularLabour: resource.days.reduce((sum, day) => sum + (day.actual.regularLabour || 0), 0),
                overtimeLabour: resource.days.reduce((sum, day) => sum + (day.actual.overtimeLabour || 0), 0),
                premiumLabour: resource.days.reduce((sum, day) => sum + (day.actual.premiumLabour || 0), 0),
                regularTravel: resource.days.reduce((sum, day) => sum + (day.actual.regularTravel || 0), 0),
                overtimeTravel: resource.days.reduce((sum, day) => sum + (day.actual.overtimeTravel || 0), 0),
                premiumTravel: resource.days.reduce((sum, day) => sum + (day.actual.premiumTravel || 0), 0),
                mileage: resource.days.reduce((sum, day) => sum + (day.actual.mileage || 0), 0),
                perDiem: resource.days.reduce((sum, day) => sum + (day.actual.perDiem || 0), 0),
                flight: resource.days.reduce((sum, day) => sum + (day.actual.flight || 0), 0),
                carRental: resource.days.reduce((sum, day) => sum + (day.actual.carRental || 0), 0),
                hotel: resource.days.reduce((sum, day) => sum + (day.actual.hotel || 0), 0)
              };
              const delta = {
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
              return { planned, actual, delta };
            })();
          
          return (
            <Box 
              key={resource.resourceId} 
              data-resource-section
              data-resource-index={resourceIndex}
              onMouseMove={throttledMouseMove}
              onMouseLeave={handleMouseLeave}
              sx={{ mb: 3 }}
            >
            {/* Resource Header */}
            <Box sx={{ 
              display: 'flex', 
              alignItems: 'center', 
              bgcolor: darkMode ? '#1a1a2e' : '#2c3e50', 
              border: `1px solid ${colorTheme.panelBorder}`, 
              borderRadius: '4px 4px 0 0',
              p: 0.5, 
              mb: 0 
            }}>
              <Box sx={{ display: 'flex', alignItems: 'center', flex: 1, gap: 1 }}>
                <Typography variant="subtitle2" sx={{ color: '#fff', fontWeight: 'bold', fontSize: '0.9rem' }}>
                  Resource: 
                </Typography>
                <TextField
                  size="small"
                  value={resource.resourceName}
                  onChange={(e) => {
                    const newTrackingData = { ...trackingData };
                    newTrackingData.resources[resourceIndex].resourceName = e.target.value;
                    setTrackingData(newTrackingData);
                  }}
                  sx={{
                    '& .MuiInputBase-root': {
                      fontSize: '0.9rem',
                      minHeight: 'auto',
                      '& input': {
                        padding: '2px 8px',
                        fontSize: '0.9rem',
                        color: '#fff',
                        fontWeight: 'bold'
                      },
                      '& fieldset': {
                        borderColor: 'transparent'
                      },
                      '&:hover fieldset': {
                        borderColor: '#fff'
                      },
                      '&.Mui-focused fieldset': {
                        borderColor: '#fff'
                      }
                    }
                  }}
                />
                <Typography variant="subtitle2" sx={{ color: '#fff', fontWeight: 'bold', fontSize: '0.9rem' }}>
                  Days on Site: {(() => {
                    // Count days where labour hours > 0
                    const daysOnSite = resource.days.filter(day => {
                      const totalLabourHours = (day.planned.regularLabour || 0) + (day.planned.overtimeLabour || 0) + (day.planned.premiumLabour || 0);
                      return totalLabourHours > 0;
                    }).length;
                    return daysOnSite;
                  })()}
                </Typography>
              </Box>
              <IconButton
                onClick={() => toggleResourceMinimized(resourceIndex)}
                sx={{ color: '#fff', padding: '4px' }}
                size="small"
              >
                {resource.isMinimized ? <ExpandMore /> : <ExpandLess />}
              </IconButton>
            </Box>

            {/* Resource Data Table with Frozen First Column */}
            <Box sx={{ 
              position: 'relative',
              overflow: 'hidden',
              border: `1px solid ${colorTheme.panelBorder}`,
              borderTop: 'none',
              borderRadius: '0 0 4px 4px'
            }}>
              {/* Navigation Arrows for this resource */}
              <Box
                onClick={() => handleArrowClick('left')}
                sx={{
                  position: 'absolute',
                  left: '230px', // 220px (frozen column) + 10px margin
                  top: '50%',
                  transform: 'translateY(-50%)',
                  zIndex: 1000,
                  bgcolor: 'rgba(0, 0, 0, 0.7)',
                  color: '#fff',
                  borderRadius: '50%',
                  width: 40,
                  height: 40,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: 'pointer',
                  fontSize: '20px',
                  fontWeight: 'bold',
                  transition: 'all 0.2s ease',
                  opacity: arrowStates[resourceIndex]?.left ? 1 : 0.3,
                  userSelect: 'none', // Prevent text selection
                  '&:hover': {
                    bgcolor: 'rgba(0, 0, 0, 0.9)',
                    transform: 'translateY(-50%) scale(1.1)'
                  }
                }}
              >
                ←
              </Box>
              
              <Box
                onClick={() => handleArrowClick('right')}
                sx={{
                  position: 'absolute',
                  right: '430px', // 420px (frozen totals column) + 10px margin
                  top: '50%',
                  transform: 'translateY(-50%)',
                  zIndex: 1000,
                  bgcolor: 'rgba(0, 0, 0, 0.7)',
                  color: '#fff',
                  borderRadius: '50%',
                  width: 40,
                  height: 40,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: 'pointer',
                  fontSize: '20px',
                  fontWeight: 'bold',
                  transition: 'all 0.2s ease',
                  opacity: arrowStates[resourceIndex]?.right ? 1 : 0.3,
                  userSelect: 'none', // Prevent text selection
                  '&:hover': {
                    bgcolor: 'rgba(0, 0, 0, 0.9)',
                    transform: 'translateY(-50%) scale(1.1)'
                  }
                }}
              >
                →
              </Box>
                {/* Frozen First Column */}
                <Box sx={{
                  position: 'absolute',
                  left: 0,
                  top: 0,
                  width: '220px',
                  zIndex: 10,
                  bgcolor: colorTheme.panelBg,
                  borderRight: `2px solid #888`
                }}>
                  {/* Days Header in Frozen Column */}
                  <Box sx={{
                    height: '40px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: colorTheme.headerBg,
                    borderBottom: `1px solid ${colorTheme.panelBorder}`,
                    fontWeight: 'bold',
                    color: colorTheme.panelText,
                    fontSize: '0.9rem'
                  }}>
                    Days
                  </Box>
                  
                  {/* Category Header */}
                  <Box sx={{
                    height: '48px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: colorTheme.headerBg,
                    borderBottom: `1px solid ${colorTheme.panelBorder}`,
                    fontWeight: 'bold',
                    color: colorTheme.panelText
                  }}>
                    Category
                  </Box>
                  
                  {/* Data Rows - Show all rows when expanded, only totals when minimized */}
                  {resource.isMinimized ? (
                    // Minimized view - only show totals row
                    <Box sx={{
                      height: '48px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'flex-start',
                      px: 2,
                      bgcolor: colorTheme.totalsBg,
                      borderBottom: `1px solid ${colorTheme.panelBorder}`,
                      fontWeight: 'bold',
                      color: '#fff'
                    }}>
                      Totals
                    </Box>
                  ) : (
                    // Full view - show all rows
                    [
                      { label: 'Regular Labour', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Overtime Labour', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Premium Labour', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Subtotal Hours - Labour', bgcolor: colorTheme.hoursSubtotalBg, specialBorder: 'thick' },
                      { label: 'Regular Travel', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Overtime Travel', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Premium Travel', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Subtotal Hours - Travel', bgcolor: colorTheme.hoursSubtotalBg, specialBorder: 'thick' },
                      { label: 'Total Hours Charges', bgcolor: colorTheme.chargesSubtotalBg, specialBorder: 'double' },
                      { label: 'Mileage', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Per Diem', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Flight', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Car Rental', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Hotel', bgcolor: 'transparent', specialBorder: false },
                      { label: 'Total Expenses', bgcolor: colorTheme.chargesSubtotalBg, specialBorder: 'double' },
                      { label: 'Totals', bgcolor: colorTheme.totalsBg, specialBorder: 'double' }
                    ].map((row, rowIndex) => {
                      // Determine if this row is editable
                      const isEditable = [
                        'Regular Labour', 'Overtime Labour', 'Premium Labour',
                        'Regular Travel', 'Overtime Travel', 'Premium Travel',
                        'Mileage', 'Per Diem', 'Flight', 'Car Rental', 'Hotel'
                      ].includes(row.label);
                      

                      
                      const isCurrency = ['Mileage', 'Per Diem', 'Flight', 'Car Rental', 'Hotel'].includes(row.label);
                      
                      return (
                        <Box 
                          key={rowIndex} 
                          onClick={isEditable ? () => handleBulkEdit(row.label, isCurrency) : undefined}
                          sx={{
                            height: '48px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'flex-start',
                            px: 2,
                            bgcolor: row.bgcolor,
                            borderBottom: row.specialBorder === 'thick' ? `6px double #888` : 
                                         row.specialBorder === 'double' ? `6px double #888` : 
                                         `1px solid ${colorTheme.panelBorder}`,
                            fontWeight: row.label.includes('Subtotal') || row.label.includes('Totals') ? 'bold' : 'normal',
                            color: '#fff',
                            cursor: isEditable ? 'pointer' : 'default',
                            '&:hover': isEditable ? {
                              bgcolor: darkMode ? '#3a3a4a' : '#f0f0f0',
                              color: darkMode ? '#4fc3f7' : '#1976d2',
                              transition: 'all 0.2s ease-in-out'
                            } : {},
                            transition: 'all 0.2s ease-in-out'
                          }}
                        >
                          {row.label}
                        </Box>
                      );
                    })
                  )}
                </Box>

                {/* Frozen Totals Column */}
                <Box sx={{
                  position: 'absolute',
                  right: 0,
                  top: 0,
                  width: '420px',
                  zIndex: 10,
                  bgcolor: colorTheme.panelBg,
                  borderLeft: `6px double #888`
                }}>
                  {/* Totals Header */}
                  <Box sx={{
                    height: '40px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: darkMode ? '#1a1a2e' : '#2c3e50', // Same as Resource header
                    borderBottom: `1px solid ${colorTheme.panelBorder}`,
                    fontWeight: 'bold',
                    color: '#fff',
                    fontSize: '0.9rem'
                  }}>
                    Totals
                  </Box>
                  
                  {/* Totals Header Row */}
                  <Box sx={{
                    height: '48px',
                    display: 'flex',
                    borderBottom: `1px solid ${colorTheme.panelBorder}`
                  }}>
                    <Box sx={{
                      width: '140px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: colorTheme.headerBg,
                      borderRight: `1px solid ${colorTheme.panelBorder}`,
                      fontWeight: 'bold',
                      color: colorTheme.panelText
                    }}>
                      Planned
                    </Box>
                    <Box sx={{
                      width: '140px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: colorTheme.headerBg,
                      borderRight: `1px solid ${colorTheme.panelBorder}`,
                      fontWeight: 'bold',
                      color: colorTheme.panelText
                    }}>
                      Actual
                    </Box>
                    <Box sx={{
                      width: '140px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: colorTheme.headerBg,
                      fontWeight: 'bold',
                      color: colorTheme.panelText
                    }}>
                      Delta
                    </Box>
                  </Box>
                  
                  {/* Totals Data Rows */}
                  {(() => {
                    if (resource.isMinimized) {
                      // Minimized view - only show totals row
                      const plannedCharges = (resourceTotals.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                        (resourceTotals.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                        (resourceTotals.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                        (resourceTotals.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                        (resourceTotals.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                        (resourceTotals.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                      const actualCharges = (resourceTotals.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                        (resourceTotals.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                        (resourceTotals.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                        (resourceTotals.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                        (resourceTotals.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                        (resourceTotals.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                      const plannedExpenses = (resourceTotals.planned.mileage || 0) + (resourceTotals.planned.perDiem || 0) + (resourceTotals.planned.flight || 0) + (resourceTotals.planned.carRental || 0) + (resourceTotals.planned.hotel || 0);
                      const actualExpenses = (resourceTotals.actual.mileage || 0) + (resourceTotals.actual.perDiem || 0) + (resourceTotals.actual.flight || 0) + (resourceTotals.actual.carRental || 0) + (resourceTotals.actual.hotel || 0);
                      const deltaValue = (actualCharges + actualExpenses) - (plannedCharges + plannedExpenses);
                      
                      return (
                        <Box sx={{
                          height: '48px',
                          display: 'flex',
                          borderBottom: `1px solid ${colorTheme.panelBorder}`
                        }}>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.totalsBg,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            color: (plannedCharges + plannedExpenses) === 0 ? '#888' : '#fff',
                            fontWeight: 'bold'
                          }}>
                            {formatValue(plannedCharges + plannedExpenses, true)}
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.totalsBg,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            color: (actualCharges + actualExpenses) === 0 ? '#888' : '#fff',
                            fontWeight: 'bold'
                          }}>
                            {formatValue(actualCharges + actualExpenses, true)}
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.totalsBg,
                            color: deltaValue === 0 ? '#888' : deltaValue > 0 ? colorTheme.redText : colorTheme.greenText,
                            fontWeight: 'bold'
                          }}>
                            {formatValue(deltaValue, true)}
                          </Box>
                        </Box>
                      );
                    } else {
                      // Full view - show all rows
                      const rows = [
                        // Regular Labour
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.regularLabour),
                          getActual: () => formatValue(resourceTotals.actual.regularLabour),
                          getDelta: () => formatValue(resourceTotals.delta.regularLabour),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Overtime Labour
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.overtimeLabour),
                          getActual: () => formatValue(resourceTotals.actual.overtimeLabour),
                          getDelta: () => formatValue(resourceTotals.delta.overtimeLabour),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Premium Labour
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.premiumLabour),
                          getActual: () => formatValue(resourceTotals.actual.premiumLabour),
                          getDelta: () => formatValue(resourceTotals.delta.premiumLabour),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Subtotal Hours - Labour
                        { 
                          getPlanned: () => {
                            const total = (resourceTotals.planned.regularLabour || 0) + (resourceTotals.planned.overtimeLabour || 0) + (resourceTotals.planned.premiumLabour || 0);
                            return formatValue(total);
                          },
                          getActual: () => {
                            const total = (resourceTotals.actual.regularLabour || 0) + (resourceTotals.actual.overtimeLabour || 0) + (resourceTotals.actual.premiumLabour || 0);
                            return formatValue(total);
                          },
                          getDelta: () => {
                            const plannedTotal = (resourceTotals.planned.regularLabour || 0) + (resourceTotals.planned.overtimeLabour || 0) + (resourceTotals.planned.premiumLabour || 0);
                            const actualTotal = (resourceTotals.actual.regularLabour || 0) + (resourceTotals.actual.overtimeLabour || 0) + (resourceTotals.actual.premiumLabour || 0);
                            return formatValue(actualTotal - plannedTotal);
                          },
                          bgcolor: colorTheme.hoursSubtotalBg,
                          specialBorder: 'thick'
                        },
                        // Regular Travel
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.regularTravel),
                          getActual: () => formatValue(resourceTotals.actual.regularTravel),
                          getDelta: () => formatValue(resourceTotals.delta.regularTravel),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Overtime Travel
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.overtimeTravel),
                          getActual: () => formatValue(resourceTotals.actual.overtimeTravel),
                          getDelta: () => formatValue(resourceTotals.delta.overtimeTravel),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Premium Travel
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.premiumTravel),
                          getActual: () => formatValue(resourceTotals.actual.premiumTravel),
                          getDelta: () => formatValue(resourceTotals.delta.premiumTravel),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Subtotal Hours - Travel
                        { 
                          getPlanned: () => {
                            const total = (resourceTotals.planned.regularTravel || 0) + (resourceTotals.planned.overtimeTravel || 0) + (resourceTotals.planned.premiumTravel || 0);
                            return formatValue(total);
                          },
                          getActual: () => {
                            const total = (resourceTotals.actual.regularTravel || 0) + (resourceTotals.actual.overtimeTravel || 0) + (resourceTotals.actual.premiumTravel || 0);
                            return formatValue(total);
                          },
                          getDelta: () => {
                            const plannedTotal = (resourceTotals.planned.regularTravel || 0) + (resourceTotals.planned.overtimeTravel || 0) + (resourceTotals.planned.premiumTravel || 0);
                            const actualTotal = (resourceTotals.actual.regularTravel || 0) + (resourceTotals.actual.overtimeTravel || 0) + (resourceTotals.actual.premiumTravel || 0);
                            return formatValue(actualTotal - plannedTotal);
                          },
                          bgcolor: colorTheme.hoursSubtotalBg,
                          specialBorder: 'thick'
                        },
                        // Total Hours Charges
                        { 
                          getPlanned: () => {
                            const regularLabour = (resourceTotals.planned.regularLabour || 0) * rateCalculations.regularLabourRate;
                            const overtimeLabour = (resourceTotals.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate;
                            const premiumLabour = (resourceTotals.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate;
                            const regularTravel = (resourceTotals.planned.regularTravel || 0) * rateCalculations.regularTravelRate;
                            const overtimeTravel = (resourceTotals.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate;
                            const premiumTravel = (resourceTotals.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            return formatValue(regularLabour + overtimeLabour + premiumLabour + regularTravel + overtimeTravel + premiumTravel, true);
                          },
                          getActual: () => {
                            const regularLabour = (resourceTotals.actual.regularLabour || 0) * rateCalculations.regularLabourRate;
                            const overtimeLabour = (resourceTotals.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate;
                            const premiumLabour = (resourceTotals.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate;
                            const regularTravel = (resourceTotals.actual.regularTravel || 0) * rateCalculations.regularTravelRate;
                            const overtimeTravel = (resourceTotals.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate;
                            const premiumTravel = (resourceTotals.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            return formatValue(regularLabour + overtimeLabour + premiumLabour + regularTravel + overtimeTravel + premiumTravel, true);
                          },
                          getDelta: () => {
                            const plannedCharges = (resourceTotals.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const actualCharges = (resourceTotals.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            return formatValue(actualCharges - plannedCharges, true);
                          },
                          bgcolor: colorTheme.chargesSubtotalBg,
                          specialBorder: 'double'
                        },
                        // Mileage
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.mileage, true),
                          getActual: () => formatValue(resourceTotals.actual.mileage, true),
                          getDelta: () => formatValue(resourceTotals.delta.mileage, true),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Per Diem
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.perDiem, true),
                          getActual: () => formatValue(resourceTotals.actual.perDiem, true),
                          getDelta: () => formatValue(resourceTotals.delta.perDiem, true),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Flight
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.flight, true),
                          getActual: () => formatValue(resourceTotals.actual.flight, true),
                          getDelta: () => formatValue(resourceTotals.delta.flight, true),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Car Rental
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.carRental, true),
                          getActual: () => formatValue(resourceTotals.actual.carRental, true),
                          getDelta: () => formatValue(resourceTotals.delta.carRental, true),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Hotel
                        { 
                          getPlanned: () => formatValue(resourceTotals.planned.hotel, true),
                          getActual: () => formatValue(resourceTotals.actual.hotel, true),
                          getDelta: () => formatValue(resourceTotals.delta.hotel, true),
                          bgcolor: 'transparent',
                          specialBorder: false
                        },
                        // Total Expenses
                        { 
                          getPlanned: () => {
                            const total = (resourceTotals.planned.mileage || 0) + (resourceTotals.planned.perDiem || 0) + (resourceTotals.planned.flight || 0) + (resourceTotals.planned.carRental || 0) + (resourceTotals.planned.hotel || 0);
                            return formatValue(total, true);
                          },
                          getActual: () => {
                            const total = (resourceTotals.actual.mileage || 0) + (resourceTotals.actual.perDiem || 0) + (resourceTotals.actual.flight || 0) + (resourceTotals.actual.carRental || 0) + (resourceTotals.actual.hotel || 0);
                            return formatValue(total, true);
                          },
                          getDelta: () => {
                            const plannedTotal = (resourceTotals.planned.mileage || 0) + (resourceTotals.planned.perDiem || 0) + (resourceTotals.planned.flight || 0) + (resourceTotals.planned.carRental || 0) + (resourceTotals.planned.hotel || 0);
                            const actualTotal = (resourceTotals.actual.mileage || 0) + (resourceTotals.actual.perDiem || 0) + (resourceTotals.actual.flight || 0) + (resourceTotals.actual.carRental || 0) + (resourceTotals.actual.hotel || 0);
                            return formatValue(actualTotal - plannedTotal, true);
                          },
                          bgcolor: colorTheme.chargesSubtotalBg,
                          specialBorder: 'double'
                        },
                        // Totals
                        { 
                          getPlanned: () => {
                            const plannedCharges = (resourceTotals.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const plannedExpenses = (resourceTotals.planned.mileage || 0) + (resourceTotals.planned.perDiem || 0) + (resourceTotals.planned.flight || 0) + (resourceTotals.planned.carRental || 0) + (resourceTotals.planned.hotel || 0);
                            return formatValue(plannedCharges + plannedExpenses, true);
                          },
                          getActual: () => {
                            const actualCharges = (resourceTotals.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const actualExpenses = (resourceTotals.actual.mileage || 0) + (resourceTotals.actual.perDiem || 0) + (resourceTotals.actual.flight || 0) + (resourceTotals.actual.carRental || 0) + (resourceTotals.actual.hotel || 0);
                            return formatValue(actualCharges + actualExpenses, true);
                          },
                          getDelta: () => {
                            const plannedCharges = (resourceTotals.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const actualCharges = (resourceTotals.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (resourceTotals.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (resourceTotals.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (resourceTotals.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (resourceTotals.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (resourceTotals.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const plannedExpenses = (resourceTotals.planned.mileage || 0) + (resourceTotals.planned.perDiem || 0) + (resourceTotals.planned.flight || 0) + (resourceTotals.planned.carRental || 0) + (resourceTotals.planned.hotel || 0);
                            const actualExpenses = (resourceTotals.actual.mileage || 0) + (resourceTotals.actual.perDiem || 0) + (resourceTotals.actual.flight || 0) + (resourceTotals.actual.carRental || 0) + (resourceTotals.actual.hotel || 0);
                            return formatValue((actualCharges + actualExpenses) - (plannedCharges + plannedExpenses), true);
                          },
                          bgcolor: colorTheme.totalsBg,
                          specialBorder: 'double'
                        }
                      ];

                      return rows.map((row, rowIndex) => (
                        <Box key={rowIndex} sx={{
                          height: '48px',
                          display: 'flex',
                          borderBottom: row.specialBorder === 'thick' ? `6px double #888` : 
                                       row.specialBorder === 'double' ? `6px double #888` : 
                                       `1px solid ${colorTheme.panelBorder}`
                        }}>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: row.bgcolor,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            color: (() => {
                              const plannedValue = row.getPlanned();
                              const numValue = parseFloat(plannedValue.replace(/[$,]/g, ''));
                              return numValue === 0 ? '#888' : '#fff';
                            })()
                          }}>
                            {row.getPlanned()}
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: row.bgcolor,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            color: (() => {
                              const actualValue = row.getActual();
                              const numValue = parseFloat(actualValue.replace(/[$,]/g, ''));
                              return numValue === 0 ? '#888' : '#fff';
                            })()
                          }}>
                            {row.getActual()}
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: row.bgcolor,
                            color: (() => {
                              const deltaValue = row.getDelta();
                              const numValue = parseFloat(deltaValue.replace(/[$,]/g, ''));
                              if (numValue === 0) return '#888';
                              if (numValue > 0) return colorTheme.redText;
                              if (numValue < 0) return colorTheme.greenText;
                              return '#fff';
                            })(),
                            fontWeight: 'bold'
                          }}>
                            {row.getDelta()}
                          </Box>
                        </Box>
                      ));
                    }
                  })()}
                </Box>

                {/* Scrollable Content */}
                <Box 
                  data-scroll-container
                  sx={{
                    overflowX: 'auto',
                    overflowY: 'hidden',
                    ml: '220px', // Offset for frozen column
                    mr: '420px', // Offset for frozen totals column
                    '&::-webkit-scrollbar': {
                      height: '8px'
                    },
                    '&::-webkit-scrollbar-track': {
                      bgcolor: colorTheme.panelBorder
                    },
                    '&::-webkit-scrollbar-thumb': {
                      bgcolor: '#888',
                      borderRadius: '4px'
                    },
                    '&::-webkit-scrollbar-thumb:hover': {
                      bgcolor: '#666'
                    }
                  }}
                >
                  <Box sx={{
                    display: 'inline-block',
                    minWidth: '100%',
                    width: `${resource.days.length * 420}px` // 3 columns per day * 140px each
                  }}>
                    {/* Days Header Row */}
                    <Box sx={{
                      display: 'flex',
                      height: '40px',
                      borderBottom: `1px solid ${colorTheme.panelBorder}`
                    }}>
                      {resource.days.map((day, dayIndex) => {
                        // Calculate the actual date if startDate is provided
                        let dayInfo = '';
                        let dayOfWeek = '';
                        if (startDate) {
                          const actualDate = startDate.add(day.dayNumber - 1, 'day');
                          dayOfWeek = actualDate.format('dddd'); // Monday, Tuesday, etc.
                          const dateStr = actualDate.format('MMM D'); // Jan 15, Feb 3, etc.
                          dayInfo = `${dateStr}, ${dayOfWeek}, Day ${day.dayNumber}`;
                        } else {
                          // No dates chosen, use the day of week calculated by the estimator engine
                          dayOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][day.dayOfWeek];
                          dayInfo = `${dayOfWeek}, Day ${day.dayNumber}`;
                        }
                        
                        // Debug: Log the day info for this resource
                        if (dayIndex === 0) {
                          console.log(`Resource ${resource.resourceName}: Day ${day.dayNumber} = ${dayOfWeek} (dayOfWeek: ${day.dayOfWeek})`);
                        }
                        
                        return (
                          <Box key={dayIndex} sx={{
                            width: '420px', // 3 columns per day * 140px each
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.headerBg,
                            borderRight: `2px solid #888`,
                            fontWeight: 'bold',
                            color: colorTheme.panelText,
                            fontSize: '0.9rem'
                          }}>
                            {dayInfo}
                          </Box>
                        );
                      })}
                    </Box>

                    {/* Header Row */}
                    <Box sx={{
                      display: 'flex',
                      height: '48px',
                      borderBottom: `1px solid ${colorTheme.panelBorder}`
                    }}>
                      {resource.days.map((day, dayIndex) => (
                        <React.Fragment key={dayIndex}>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.headerBg,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            fontWeight: 'bold',
                            color: colorTheme.panelText
                          }}>
                            Planned
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.headerBg,
                            borderRight: `1px solid ${colorTheme.panelBorder}`,
                            fontWeight: 'bold',
                            color: colorTheme.panelText
                          }}>
                            Actual
                          </Box>
                          <Box sx={{
                            width: '140px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: colorTheme.headerBg,
                            borderRight: `2px solid #888`,
                            fontWeight: 'bold',
                            color: colorTheme.panelText
                          }}>
                            Delta
                          </Box>
                        </React.Fragment>
                      ))}
                    </Box>

                    {/* Data Rows */}
                    {(() => {
                      if (resource.isMinimized) {
                        // Minimized view - only show totals row
                        const totalsRow = { 
                          getPlanned: (day: any) => {
                            const plannedCharges = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const plannedExpenses = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                            return formatValue(plannedCharges + plannedExpenses, true);
                          },
                          getActual: (day: any) => {
                            const actualCharges = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const actualExpenses = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                            return formatValue(actualCharges + actualExpenses, true);
                          },
                          getDelta: (day: any) => {
                            const plannedCharges = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const actualCharges = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                              (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                              (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                              (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                              (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                              (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                            const plannedExpenses = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                            const actualExpenses = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                            return formatValue((actualCharges + actualExpenses) - (plannedCharges + plannedExpenses), true);
                          },
                          bgcolor: colorTheme.totalsBg,
                          specialBorder: false
                        };
                        
                        return (
                          <Box sx={{
                            display: 'flex',
                            height: '48px',
                            borderBottom: `1px solid ${colorTheme.panelBorder}`
                          }}>
                            {resource.days.map((day, dayIndex) => (
                              <React.Fragment key={dayIndex}>
                                <Box sx={{
                                  width: '140px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: totalsRow.bgcolor,
                                  borderRight: `1px solid ${colorTheme.panelBorder}`,
                                  color: (() => {
                                    const plannedValue = totalsRow.getPlanned(day);
                                    const numValue = parseFloat(plannedValue.replace(/[$,]/g, ''));
                                    return numValue === 0 ? '#888' : '#fff';
                                  })()
                                }}>
                                  {totalsRow.getPlanned(day)}
                                </Box>
                                <Box sx={{
                                  width: '140px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: totalsRow.bgcolor,
                                  borderRight: `1px solid ${colorTheme.panelBorder}`,
                                  color: (() => {
                                    const actualValue = totalsRow.getActual(day);
                                    const numValue = parseFloat(actualValue.replace(/[$,]/g, ''));
                                    return numValue === 0 ? '#888' : '#fff';
                                  })()
                                }}>
                                  {totalsRow.getActual(day)}
                                </Box>
                                <Box sx={{
                                  width: '140px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: totalsRow.bgcolor,
                                  borderRight: `2px solid #888`,
                                  color: (() => {
                                    const deltaValue = totalsRow.getDelta(day);
                                    const numValue = parseFloat(deltaValue.replace(/[$,]/g, ''));
                                    if (numValue === 0) return '#888';
                                    if (numValue > 0) return colorTheme.redText;
                                    if (numValue < 0) return colorTheme.greenText;
                                    return '#fff';
                                  })(),
                                  fontWeight: 'bold'
                                }}>
                                  {totalsRow.getDelta(day)}
                                </Box>
                              </React.Fragment>
                            ))}
                          </Box>
                        );
                      } else {
                        // Full view - show all rows
                        const rows = [
                          // Regular Labour
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.regularLabour),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="regularLabour"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.regularLabour),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Overtime Labour
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.overtimeLabour),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="overtimeLabour"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.overtimeLabour),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Premium Labour
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.premiumLabour),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="premiumLabour"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.premiumLabour),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Subtotal Hours - Labour
                          { 
                            getPlanned: (day: any) => {
                              const total = (day.planned.regularLabour || 0) + (day.planned.overtimeLabour || 0) + (day.planned.premiumLabour || 0);
                              return formatValue(total);
                            },
                            getActual: (day: any) => {
                              const total = (day.actual.regularLabour || 0) + (day.actual.overtimeLabour || 0) + (day.actual.premiumLabour || 0);
                              return formatValue(total);
                            },
                            getDelta: (day: any) => {
                              const plannedTotal = (day.planned.regularLabour || 0) + (day.planned.overtimeLabour || 0) + (day.planned.premiumLabour || 0);
                              const actualTotal = (day.actual.regularLabour || 0) + (day.actual.overtimeLabour || 0) + (day.actual.premiumLabour || 0);
                              return formatValue(actualTotal - plannedTotal);
                            },
                            bgcolor: colorTheme.hoursSubtotalBg,
                            specialBorder: 'thick'
                          },
                          // Regular Travel
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.regularTravel),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="regularTravel"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.regularTravel),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Overtime Travel
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.overtimeTravel),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="overtimeTravel"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.overtimeTravel),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Premium Travel
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.premiumTravel),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="premiumTravel"
                                  isCurrency={false}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.premiumTravel),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Subtotal Hours - Travel
                          { 
                            getPlanned: (day: any) => {
                              const total = (day.planned.regularTravel || 0) + (day.planned.overtimeTravel || 0) + (day.planned.premiumTravel || 0);
                              return formatValue(total);
                            },
                            getActual: (day: any) => {
                              const total = (day.actual.regularTravel || 0) + (day.actual.overtimeTravel || 0) + (day.actual.premiumTravel || 0);
                              return formatValue(total);
                            },
                            getDelta: (day: any) => {
                              const plannedTotal = (day.planned.regularTravel || 0) + (day.planned.overtimeTravel || 0) + (day.planned.premiumTravel || 0);
                              const actualTotal = (day.actual.regularTravel || 0) + (day.actual.overtimeTravel || 0) + (day.actual.premiumTravel || 0);
                              return formatValue(actualTotal - plannedTotal);
                            },
                            bgcolor: colorTheme.hoursSubtotalBg,
                            specialBorder: 'thick'
                          },
                          // Total Hours Charges
                          { 
                            getPlanned: (day: any) => {
                              const regularLabour = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate;
                              const overtimeLabour = (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate;
                              const premiumLabour = (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate;
                              const regularTravel = (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate;
                              const overtimeTravel = (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate;
                              const premiumTravel = (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              return formatValue(regularLabour + overtimeLabour + premiumLabour + regularTravel + overtimeTravel + premiumTravel, true);
                            },
                            getActual: (day: any) => {
                              const regularLabour = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate;
                              const overtimeLabour = (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate;
                              const premiumLabour = (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate;
                              const regularTravel = (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate;
                              const overtimeTravel = (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate;
                              const premiumTravel = (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              return formatValue(regularLabour + overtimeLabour + premiumLabour + regularTravel + overtimeTravel + premiumTravel, true);
                            },
                            getDelta: (day: any) => {
                              const plannedCharges = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              const actualCharges = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              return formatValue(actualCharges - plannedCharges, true);
                            },
                            bgcolor: colorTheme.chargesSubtotalBg,
                            specialBorder: 'double'
                          },
                          // Mileage
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.mileage, true),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="mileage"
                                  isCurrency={true}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.mileage, true),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Per Diem
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.perDiem, true),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="perDiem"
                                  isCurrency={true}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.perDiem, true),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Flight
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.flight, true),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="flight"
                                  isCurrency={true}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.flight, true),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Car Rental
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.carRental, true),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="carRental"
                                  isCurrency={true}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.carRental, true),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Hotel
                          { 
                            getPlanned: (day: any) => formatValue(day.planned.hotel, true),
                            getActual: (day: any) => (
                                <MemoizedDayCell
                                  day={day}
                                  resourceIndex={resourceIndex}
                                  fieldName="hotel"
                                  isCurrency={true}
                                  onValueChange={handleActualValueChange}
                                  darkMode={darkMode}
                                  getDayIndex={(day) => resource.days.indexOf(day)}
                                />
                            ),
                            getDelta: (day: any) => formatValue(day.delta.hotel, true),
                            bgcolor: 'transparent',
                            specialBorder: false
                          },
                          // Total Expenses
                          { 
                            getPlanned: (day: any) => {
                              const total = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                              return formatValue(total, true);
                            },
                            getActual: (day: any) => {
                              const total = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                              return formatValue(total, true);
                            },
                            getDelta: (day: any) => {
                              const plannedTotal = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                              const actualTotal = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                              return formatValue(actualTotal - plannedTotal, true);
                            },
                            bgcolor: colorTheme.chargesSubtotalBg,
                            specialBorder: 'double'
                          },
                          // Totals
                          { 
                            getPlanned: (day: any) => {
                              const plannedCharges = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              const plannedExpenses = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                              return formatValue(plannedCharges + plannedExpenses, true);
                            },
                            getActual: (day: any) => {
                              const actualCharges = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              const actualExpenses = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                              return formatValue(actualCharges + actualExpenses, true);
                            },
                            getDelta: (day: any) => {
                              const plannedCharges = (day.planned.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.planned.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.planned.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.planned.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.planned.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.planned.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              const actualCharges = (day.actual.regularLabour || 0) * rateCalculations.regularLabourRate +
                                (day.actual.overtimeLabour || 0) * rateCalculations.overtimeLabourRate +
                                (day.actual.premiumLabour || 0) * rateCalculations.premiumLabourRate +
                                (day.actual.regularTravel || 0) * rateCalculations.regularTravelRate +
                                (day.actual.overtimeTravel || 0) * rateCalculations.overtimeTravelRate +
                                (day.actual.premiumTravel || 0) * rateCalculations.premiumTravelRate;
                              const plannedExpenses = (day.planned.mileage || 0) + (day.planned.perDiem || 0) + (day.planned.flight || 0) + (day.planned.carRental || 0) + (day.planned.hotel || 0);
                              const actualExpenses = (day.actual.mileage || 0) + (day.actual.perDiem || 0) + (day.actual.flight || 0) + (day.actual.carRental || 0) + (day.actual.hotel || 0);
                              return formatValue((actualCharges + actualExpenses) - (plannedCharges + plannedExpenses), true);
                            },
                            bgcolor: colorTheme.totalsBg,
                            specialBorder: 'double'
                          }
                        ];

                        return rows.map((row, rowIndex) => (
                          <Box key={rowIndex} sx={{
                            display: 'flex',
                            height: '48px',
                            borderBottom: row.specialBorder === 'thick' ? `6px double #888` : 
                                         row.specialBorder === 'double' ? `6px double #888` : 
                                         `1px solid ${colorTheme.panelBorder}`
                          }}>
                            {resource.days.map((day, dayIndex) => (
                              <React.Fragment key={dayIndex}>
                                <Box sx={{
                                  width: '160px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: row.bgcolor,
                                  borderRight: `1px solid ${colorTheme.panelBorder}`,
                                  color: (() => {
                                    const plannedValue = row.getPlanned(day);
                                    const numValue = parseFloat(plannedValue.replace(/[$,]/g, ''));
                                    return numValue === 0 ? '#888' : '#fff';
                                  })()
                                }}>
                                  {row.getPlanned(day)}
                                </Box>
                                <Box sx={{
                                  width: '160px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: row.bgcolor,
                                  borderRight: `1px solid ${colorTheme.panelBorder}`,
                                  color: (() => {
                                    const actualValue = row.getActual(day);
                                    // Check if it's a React component (not a string)
                                    if (typeof actualValue === 'object' && actualValue !== null) {
                                      return '#fff'; // Default color for input components
                                    }
                                    // It's a string, check if it's zero
                                    const numValue = parseFloat(actualValue.replace(/[$,]/g, ''));
                                    return numValue === 0 ? '#888' : '#fff';
                                  })()
                                }}>
                                  {row.getActual(day)}
                                </Box>
                                <Box sx={{
                                  width: '160px',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'center',
                                  bgcolor: row.bgcolor,
                                  borderRight: `2px solid #888`,
                                  color: (() => {
                                    const deltaValue = row.getDelta(day);
                                    const numValue = parseFloat(deltaValue.replace(/[$,]/g, ''));
                                    if (numValue === 0) return '#888';
                                    if (numValue > 0) return colorTheme.redText;
                                    if (numValue < 0) return colorTheme.greenText;
                                    return '#fff';
                                  })(),
                                  fontWeight: 'bold'
                                }}>
                                  {row.getDelta(day)}
                                </Box>
                              </React.Fragment>
                            ))}
                          </Box>
                        ));
                      }
                    })()}
                  </Box>
                </Box>
              </Box>
          </Box>
        );
      })}

        {/* Action Buttons */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 2 }}>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              onClick={handleImportData}
              startIcon={<UploadIcon />}
              sx={{ 
                color: colorTheme.panelText, 
                borderColor: colorTheme.panelBorder,
                '&:hover': {
                  borderColor: colorTheme.blueAccent
                }
              }}
            >
              Import Data
            </Button>
            <Button
              variant="outlined"
              onClick={handleExportCSV}
              sx={{ color: colorTheme.panelText, borderColor: colorTheme.panelBorder }}
            >
              Export Details (CSV)
            </Button>
          </Box>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              onClick={handleSave}
              sx={{ 
                color: colorTheme.panelText, 
                borderColor: colorTheme.panelBorder,
                '&:hover': {
                  borderColor: colorTheme.blueAccent
                }
              }}
            >
              Save
            </Button>
            <Button
              variant="outlined"
              onClick={handleSaveAs}
              sx={{ 
                color: colorTheme.panelText, 
                borderColor: colorTheme.panelBorder,
                '&:hover': {
                  borderColor: colorTheme.blueAccent
                }
              }}
            >
              Save As
            </Button>
            <Button
              variant="contained"
              onClick={() => setCloseConfirmDialogOpen(true)}
              sx={{ 
                bgcolor: colorTheme.blueAccent,
                color: '#fff',
                '&:hover': {
                  bgcolor: colorTheme.tealAccent
                }
              }}
            >
              Close
            </Button>
          </Box>
        </Box>

        {/* Hidden file input for import */}
        <input
          ref={fileInputRef}
          type="file"
          accept=".xls,.xlsx"
          style={{ display: 'none' }}
          onChange={handleFileSelect}
        />
      </DialogContent>

      <BulkEditDialog
        open={bulkEditDialogOpen}
        onClose={() => setBulkEditDialogOpen(false)}
        fieldName={bulkEditFieldName}
        isCurrency={bulkEditIsCurrency}
        onConfirm={bulkEditOnConfirm || (() => {})}
        darkMode={darkMode}
      />

      <SaveDialog
        open={saveDialogOpen}
        onClose={() => setSaveDialogOpen(false)}
        onSave={handleSaveTrackingData}
        defaultFilename={getDefaultTrackingFilename()}
        darkMode={darkMode}
        isSaveAs={false}
      />
      <SaveDialog
        open={saveAsDialogOpen}
        onClose={() => setSaveAsDialogOpen(false)}
        onSave={handleSaveTrackingData}
        defaultFilename={getDefaultTrackingFilename()}
        darkMode={darkMode}
        isSaveAs={true}
      />

      <CloseConfirmDialog
        open={closeConfirmDialogOpen}
        onClose={() => setCloseConfirmDialogOpen(false)}
        onConfirm={onClose}
        onSave={async () => {
          if (currentFilename) {
            await handleSaveTrackingData(currentFilename);
            onClose(); // Close after saving
          } else {
            setDefaultSaveFilename(getDefaultTrackingFilename());
            setSaveDialogOpen(true);
          }
        }}
        hasBeenSaved={false}
        filename={undefined}
        darkMode={darkMode}
      />

      {/* Import dialogs */}
      <ValidationWarningDialog
        open={validationWarningDialogOpen}
        warnings={validationWarnings}
        onConfirm={handleValidationWarningConfirm}
        onCancel={() => setValidationWarningDialogOpen(false)}
      />

      <ImportSummaryDialog
        open={importSummaryDialogOpen}
        processedRows={importResult.processedRows}
        skippedRows={importResult.skippedRows}
        errors={importResult.errors}
        validationWarnings={importResult.validationWarnings}
        onClose={() => setImportSummaryDialogOpen(false)}
      />

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={6000}
        onClose={() => setSnackbarOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert 
          onClose={() => setSnackbarOpen(false)} 
          severity={snackbarSeverity}
          sx={{ width: '100%' }}
        >
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </Dialog>
  );
}; 