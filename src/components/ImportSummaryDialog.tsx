import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider
} from '@mui/material';
import {
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon
} from '@mui/icons-material';

interface ImportSummaryDialogProps {
  open: boolean;
  processedRows: number;
  skippedRows: number;
  errors: string[];
  validationWarnings: string[];
  onClose: () => void;
}

const ImportSummaryDialog: React.FC<ImportSummaryDialogProps> = ({
  open,
  processedRows,
  skippedRows,
  errors,
  validationWarnings,
  onClose
}) => {
  const totalRows = processedRows + skippedRows;
  const hasErrors = errors.length > 0;
  const hasWarnings = validationWarnings.length > 0;

  return (
    <Dialog open={open} maxWidth="md" fullWidth>
      <DialogTitle>
        <Typography variant="h6" color={hasErrors ? 'error.main' : 'success.main'}>
          Import Summary
        </Typography>
      </DialogTitle>
      
      <DialogContent>
        <Box sx={{ mb: 2 }}>
          <Typography variant="body1" gutterBottom>
            Import completed with the following results:
          </Typography>
          
          <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
            <Box sx={{ textAlign: 'center', flex: 1 }}>
              <Typography variant="h4" color="success.main">
                {processedRows}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Rows Processed
              </Typography>
            </Box>
            
            <Box sx={{ textAlign: 'center', flex: 1 }}>
              <Typography variant="h4" color="warning.main">
                {skippedRows}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Rows Skipped
              </Typography>
            </Box>
            
            <Box sx={{ textAlign: 'center', flex: 1 }}>
              <Typography variant="h4" color="info.main">
                {totalRows}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Total Rows
              </Typography>
            </Box>
          </Box>
        </Box>

        {hasErrors && (
          <Alert severity="error" sx={{ mb: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Errors Found ({errors.length})
            </Typography>
            <List dense>
              {errors.map((error, index) => (
                <ListItem key={index} sx={{ py: 0 }}>
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <ErrorIcon color="error" fontSize="small" />
                  </ListItemIcon>
                  <ListItemText
                    primary={error}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                </ListItem>
              ))}
            </List>
          </Alert>
        )}

        {hasWarnings && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Validation Warnings ({validationWarnings.length})
            </Typography>
            <List dense>
              {validationWarnings.map((warning, index) => (
                <ListItem key={index} sx={{ py: 0 }}>
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <WarningIcon color="warning" fontSize="small" />
                  </ListItemIcon>
                  <ListItemText
                    primary={warning}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                </ListItem>
              ))}
            </List>
          </Alert>
        )}

        {!hasErrors && !hasWarnings && processedRows > 0 && (
          <Alert severity="success">
            <Typography variant="subtitle2">
              Import completed successfully! All data has been updated.
            </Typography>
          </Alert>
        )}

        {skippedRows > 0 && (
          <Alert severity="info" sx={{ mt: 2 }}>
            <Typography variant="body2">
              {skippedRows} row{skippedRows !== 1 ? 's were' : ' was'} skipped because they:
            </Typography>
            <List dense sx={{ mt: 1 }}>
              <ListItem sx={{ py: 0 }}>
                <ListItemIcon sx={{ minWidth: 32 }}>
                  <InfoIcon color="info" fontSize="small" />
                </ListItemIcon>
                <ListItemText
                  primary="Had missing essential data (date, charge type, employee, or item)"
                  primaryTypographyProps={{ variant: 'body2' }}
                />
              </ListItem>
              <ListItem sx={{ py: 0 }}>
                <ListItemIcon sx={{ minWidth: 32 }}>
                  <InfoIcon color="info" fontSize="small" />
                </ListItemIcon>
                <ListItemText
                  primary="Could not be matched to existing resources"
                  primaryTypographyProps={{ variant: 'body2' }}
                />
              </ListItem>
              <ListItem sx={{ py: 0 }}>
                <ListItemIcon sx={{ minWidth: 32 }}>
                  <InfoIcon color="info" fontSize="small" />
                </ListItemIcon>
                <ListItemText
                  primary="Referenced dates not found in current tracking data"
                  primaryTypographyProps={{ variant: 'body2' }}
                />
              </ListItem>
            </List>
          </Alert>
        )}
      </DialogContent>
      
      <DialogActions>
        <Button onClick={onClose} variant="contained" color="primary">
          Close
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ImportSummaryDialog; 