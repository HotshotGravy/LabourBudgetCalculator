import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Box,
  Typography,
  IconButton,
  FormControlLabel,
  Checkbox
} from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import { DayType } from '../models/ResourceDayData';
import { DatePicker, LocalizationProvider, TimePicker } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { Dayjs } from 'dayjs';
import dayjs from 'dayjs';

interface ManualDayOverride {
  dayType: DayType;
  labourHours: number;
  travelHours: number;
  includeExpenses?: boolean;
  startTime?: string;
}

interface DayEditorDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (override: ManualDayOverride | null) => void;
  dayNumber: number;
  currentOverride: ManualDayOverride | null;
  defaultDayType: DayType;
  defaultLabourHours: number;
  defaultTravelHours: number;
  defaultStartTime: Dayjs | null;
}

const dayTypeLabels: Record<DayType, string> = {
  [DayType.Work]: 'Work Day',
  [DayType.Travel]: 'Travel Day',
  [DayType.Holdover]: 'Holdover',
  [DayType.Nil]: 'No Activity'
};

export const DayEditorDialog: React.FC<DayEditorDialogProps> = ({
  open,
  onClose,
  onSave,
  dayNumber,
  currentOverride,
  defaultDayType,
  defaultLabourHours,
  defaultTravelHours,
  defaultStartTime
}) => {
  const [dayType, setDayType] = useState<DayType>(defaultDayType);
  const [labourHours, setLabourHours] = useState<number>(defaultLabourHours);
  const [travelHours, setTravelHours] = useState<number>(defaultTravelHours);
  const [includeExpenses, setIncludeExpenses] = useState<boolean>(true);
  const [startTime, setStartTime] = useState<Dayjs | null>(defaultStartTime);

  useEffect(() => {
    if (currentOverride) {
      setDayType(currentOverride.dayType);
      setLabourHours(currentOverride.labourHours);
      setTravelHours(currentOverride.travelHours);
      setIncludeExpenses(currentOverride.includeExpenses !== false);
      setStartTime(currentOverride.startTime ? dayjs(currentOverride.startTime, 'HH:mm') : defaultStartTime);
    } else {
      setDayType(defaultDayType);
      setLabourHours(defaultLabourHours);
      setTravelHours(defaultTravelHours);
      setIncludeExpenses(true);
      setStartTime(defaultStartTime);
    }
  }, [currentOverride, defaultDayType, defaultLabourHours, defaultTravelHours, defaultStartTime]);

  // Update hours when day type changes to holdover
  useEffect(() => {
    if (dayType === DayType.Holdover) {
      setLabourHours(8);
      setTravelHours(0);
    }
    if (dayType === DayType.Travel) {
      setLabourHours(0);
      setTravelHours(defaultTravelHours);
    }
  }, [dayType, defaultTravelHours]);

  // Update hours and expenses when day type changes to No Activity
  useEffect(() => {
    if (dayType === DayType.Nil) {
      setLabourHours(0);
      setTravelHours(0);
      setIncludeExpenses(false);
    }
  }, [dayType]);

  const handleSave = () => {
    const override: ManualDayOverride = {
      dayType,
      labourHours,
      travelHours,
      includeExpenses: dayType === DayType.Nil ? includeExpenses : undefined,
      startTime: startTime ? startTime.format('HH:mm') : undefined
    };
    onSave(override);
    onClose();
  };

  const handleReset = () => {
    onSave(null);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">Edit Day {dayNumber}</Typography>
          <IconButton onClick={onClose} size="small">
            <CloseIcon />
          </IconButton>
        </Box>
      </DialogTitle>
      
      <DialogContent>
        <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 3 }}>
          <FormControl fullWidth>
            <InputLabel>Day Type</InputLabel>
            <Select
              value={dayType}
              onChange={(e) => setDayType(e.target.value as DayType)}
              label="Day Type"
            >
              {Object.entries(dayTypeLabels).map(([value, label]) => (
                <MenuItem key={value} value={value}>
                  {label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label="Labour Hours"
            type="number"
            value={dayType === DayType.Holdover ? 8 : labourHours}
            onChange={(e) => setLabourHours(parseFloat(e.target.value) || 0)}
            inputProps={{ min: 0, step: 0.5 }}
            fullWidth
            disabled={dayType === DayType.Holdover}
            helperText={dayType === DayType.Holdover ? "Holdover days are fixed at 8 hours" : ""}
          />

          <TextField
            label="Travel Hours"
            type="number"
            value={dayType === DayType.Holdover ? 0 : travelHours}
            onChange={(e) => setTravelHours(parseFloat(e.target.value) || 0)}
            inputProps={{ min: 0, step: 0.5 }}
            fullWidth
            disabled={dayType === DayType.Holdover}
            helperText={dayType === DayType.Holdover ? "Holdover days have no travel time" : ""}
          />

          {dayType === DayType.Nil && (
            <FormControlLabel
              control={<Checkbox checked={includeExpenses} onChange={e => setIncludeExpenses(e.target.checked)} />}
              label="Include expenses for this day"
            />
          )}

          {/* Start Time Picker for Work Day */}
          {dayType === DayType.Work && (
            <LocalizationProvider dateAdapter={AdapterDayjs}>
              <TimePicker
                label="Start Time"
                value={startTime}
                onChange={setStartTime}
                minutesStep={30}
                ampm
                slotProps={{ textField: { size: 'small', fullWidth: true } }}
                format="hh:mm A"
              />
            </LocalizationProvider>
          )}

          {currentOverride && (
            <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
              <Typography variant="body2" color="text.secondary">
                This day has been manually overridden. Click "Reset to Auto" to revert to automatic calculation.
              </Typography>
            </Box>
          )}
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        {currentOverride && (
          <Button onClick={handleReset} color="warning">
            Reset to Auto
          </Button>
        )}
        <Button onClick={handleSave} variant="contained">
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}; 