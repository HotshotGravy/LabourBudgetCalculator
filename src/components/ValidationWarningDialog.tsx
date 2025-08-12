import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  RadioGroup,
  FormControlLabel,
  Radio,
  Alert,
  List,
  ListItem,
  ListItemText,
  Divider
} from '@mui/material';
import { ValidationWarning } from '../utils/ExcelImportManager';

interface ValidationWarningDialogProps {
  open: boolean;
  warnings: ValidationWarning[];
  onConfirm: (updatedWarnings: ValidationWarning[]) => void;
  onCancel: () => void;
}

const ValidationWarningDialog: React.FC<ValidationWarningDialogProps> = ({
  open,
  warnings,
  onConfirm,
  onCancel
}) => {
  const [updatedWarnings, setUpdatedWarnings] = useState<ValidationWarning[]>(warnings);

  const handleChoiceChange = (index: number, choice: 'useQuantity' | 'useAmount' | 'skip') => {
    const newWarnings = [...updatedWarnings];
    newWarnings[index] = { ...newWarnings[index], userChoice: choice };
    setUpdatedWarnings(newWarnings);
  };

  const handleConfirm = () => {
    onConfirm(updatedWarnings);
  };

  const handleCancel = () => {
    setUpdatedWarnings(warnings); // Reset to original
    onCancel();
  };

  return (
    <Dialog open={open} maxWidth="md" fullWidth>
      <DialogTitle>
        <Typography variant="h6" color="warning.main">
          Validation Warnings Found
        </Typography>
        <Typography variant="body2" color="text.secondary">
          The following rows have discrepancies between Quantity × Rate and Amount. Please choose how to handle each:
        </Typography>
      </DialogTitle>
      
      <DialogContent>
        <Alert severity="warning" sx={{ mb: 2 }}>
          Found {warnings.length} validation warning{warnings.length !== 1 ? 's' : ''}
        </Alert>
        
        <List>
          {updatedWarnings.map((warning, index) => (
            <React.Fragment key={index}>
              <ListItem>
                <ListItemText
                  primary={
                    <Typography variant="subtitle2" color="error">
                      Row {warning.row}: {warning.message}
                    </Typography>
                  }
                  secondary={
                    <Box sx={{ mt: 1 }}>
                      <Typography variant="body2">
                        Expected: ${warning.expectedAmount.toFixed(2)} | Actual: ${warning.actualAmount.toFixed(2)}
                      </Typography>
                      
                      <RadioGroup
                        row
                        value={warning.userChoice}
                        onChange={(e) => handleChoiceChange(index, e.target.value as any)}
                        sx={{ mt: 1 }}
                      >
                        <FormControlLabel
                          value="useQuantity"
                          control={<Radio size="small" />}
                          label={`Use Quantity (${warning.expectedAmount.toFixed(2)})`}
                        />
                        <FormControlLabel
                          value="useAmount"
                          control={<Radio size="small" />}
                          label={`Use Amount (${warning.actualAmount.toFixed(2)})`}
                        />
                        <FormControlLabel
                          value="skip"
                          control={<Radio size="small" />}
                          label="Skip this row"
                        />
                      </RadioGroup>
                    </Box>
                  }
                />
              </ListItem>
              {index < updatedWarnings.length - 1 && <Divider />}
            </React.Fragment>
          ))}
        </List>
      </DialogContent>
      
      <DialogActions>
        <Button onClick={handleCancel} color="inherit">
          Cancel
        </Button>
        <Button onClick={handleConfirm} variant="contained" color="primary">
          Apply Changes
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ValidationWarningDialog; 