import React from 'react';
import {
  Box,
  Typography,
  Button,
  Container,
  Paper,
  Grid
} from '@mui/material';
import { Calculate, Timeline } from '@mui/icons-material';

interface WelcomeScreenProps {
  onStartEstimate: () => void;
  onStartTracking: () => void;
  darkMode?: boolean;
}

export const WelcomeScreen: React.FC<WelcomeScreenProps> = ({
  onStartEstimate,
  onStartTracking,
  darkMode = false
}) => {
  const bgColor = darkMode ? '#23262b' : '#f5f5f5';
  const cardBg = darkMode ? '#2c2f36' : '#fff';
  const textColor = darkMode ? '#fff' : '#000';
  const borderColor = darkMode ? '#444' : '#ddd';
  const primaryColor = darkMode ? '#4fc3f7' : '#1976d2';
  const secondaryColor = darkMode ? '#ab47bc' : '#7b1fa2';

  return (
    <Box
      sx={{
        minHeight: '100vh',
        bgcolor: bgColor,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2
      }}
    >
      <Container maxWidth="md">
        <Paper
          elevation={3}
          sx={{
            bgcolor: cardBg,
            p: 4,
            borderRadius: 2,
            border: `1px solid ${borderColor}`
          }}
        >
          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Typography
              variant="h3"
              component="h1"
              sx={{
                color: textColor,
                fontWeight: 'bold',
                mb: 1
              }}
            >
              Labour Budget Calculator
            </Typography>
            <Typography
              variant="h6"
              sx={{
                color: darkMode ? '#aaa' : '#666',
                fontWeight: 'normal'
              }}
            >
              Time & Expense Planning & Tracking System
            </Typography>
          </Box>

          {/* Main Options */}
          <Grid container spacing={3}>
            {/* Estimate / Plan Option */}
            <Grid item xs={12} md={6}>
              <Paper
                elevation={2}
                sx={{
                  bgcolor: cardBg,
                  p: 3,
                  height: '100%',
                  border: `2px solid ${primaryColor}`,
                  borderRadius: 2,
                  cursor: 'pointer',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 4,
                    borderColor: darkMode ? '#29b6f6' : '#1565c0'
                  }
                }}
                onClick={onStartEstimate}
              >
                <Box sx={{ textAlign: 'center' }}>
                  <Calculate
                    sx={{
                      fontSize: 64,
                      color: primaryColor,
                      mb: 2
                    }}
                  />
                  <Typography
                    variant="h5"
                    component="h2"
                    sx={{
                      color: textColor,
                      fontWeight: 'bold',
                      mb: 2
                    }}
                  >
                    Estimate / Plan
                  </Typography>
                  <Typography
                    variant="body1"
                    sx={{
                      color: darkMode ? '#ccc' : '#666',
                      lineHeight: 1.6
                    }}
                  >
                    Create new time and expense estimates for projects. 
                    Plan labour hours, travel, and expenses with detailed 
                    daily breakdowns and cost calculations.
                  </Typography>
                </Box>
              </Paper>
            </Grid>

            {/* Track Option */}
            <Grid item xs={12} md={6}>
              <Paper
                elevation={2}
                sx={{
                  bgcolor: cardBg,
                  p: 3,
                  height: '100%',
                  border: `2px solid ${secondaryColor}`,
                  borderRadius: 2,
                  cursor: 'pointer',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 4,
                    borderColor: darkMode ? '#8e24aa' : '#6a1b9a'
                  }
                }}
                onClick={onStartTracking}
              >
                <Box sx={{ textAlign: 'center' }}>
                  <Timeline
                    sx={{
                      fontSize: 64,
                      color: secondaryColor,
                      mb: 2
                    }}
                  />
                  <Typography
                    variant="h5"
                    component="h2"
                    sx={{
                      color: textColor,
                      fontWeight: 'bold',
                      mb: 2
                    }}
                  >
                    Track
                  </Typography>
                  <Typography
                    variant="body1"
                    sx={{
                      color: darkMode ? '#ccc' : '#666',
                      lineHeight: 1.6
                    }}
                  >
                    Load existing tracking data to monitor project progress. 
                    Compare actual vs planned hours and expenses, update 
                    daily entries, and generate progress reports.
                  </Typography>
                </Box>
              </Paper>
            </Grid>
          </Grid>

          {/* Footer */}
          <Box sx={{ textAlign: 'center', mt: 4 }}>
            <Typography
              variant="body2"
              sx={{
                color: darkMode ? '#888' : '#999',
                fontStyle: 'italic'
              }}
            >
              Choose an option to get started
            </Typography>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}; 