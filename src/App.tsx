import React, { useState } from 'react';
import QuickEstimator from './components/QuickEstimator';
import { WelcomeScreen } from './components/WelcomeScreen';
import { TrackingView } from './components/TrackingView';
import { ThemeProvider, createTheme, CssBaseline, IconButton, Box } from '@mui/material';
import Brightness4Icon from '@mui/icons-material/Brightness4';
import Brightness7Icon from '@mui/icons-material/Brightness7';

const darkPalette = {
  background: {
    default: '#23262b',
    paper: '#2c2f36',
  },
  text: {
    primary: '#fff',
    secondary: '#b0b0b0',
  },
  primary: {
    main: '#1976d2',
    contrastText: '#fff',
  },
  divider: '#444',
};

const lightPalette = {
  background: {
    default: '#fff',
    paper: '#fff',
  },
  text: {
    primary: '#000',
    secondary: '#444',
  },
  primary: {
    main: '#1976d2',
    contrastText: '#fff',
  },
  divider: '#ccc',
};

type AppView = 'welcome' | 'estimate' | 'tracking';

function App() {
  const [darkMode, setDarkMode] = useState(true);
  const [currentView, setCurrentView] = useState<AppView>('welcome');
  const [trackingData, setTrackingData] = useState<any>(null);
  const [loadedFilename, setLoadedFilename] = useState<string | null>(null);

  const theme = createTheme({
    palette: darkMode ? darkPalette : lightPalette,
    components: {
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
    },
  });

  const handleStartEstimate = () => {
    setCurrentView('estimate');
  };

  const handleStartTracking = () => {
    // Create a file input to load tracking data
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.trk';
    input.style.display = 'none';
    
    input.onchange = async (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (file) {
        try {
          const { TrackingDataManager } = await import('./utils/TrackingDataManager');
          const loadedTrackingData = await TrackingDataManager.loadTrackingData(file);
          setTrackingData(loadedTrackingData);
          setLoadedFilename(file.name); // Store the filename
          setCurrentView('tracking');
        } catch (error) {
          alert('Failed to load tracking file: ' + (error as Error).message);
        }
      }
    };
    
    document.body.appendChild(input);
    input.click();
    document.body.removeChild(input);
  };

  const handleBackToWelcome = () => {
    setCurrentView('welcome');
    setTrackingData(null);
    setLoadedFilename(null);
  };

  const renderCurrentView = () => {
    switch (currentView) {
      case 'welcome':
        return (
          <WelcomeScreen
            onStartEstimate={handleStartEstimate}
            onStartTracking={handleStartTracking}
            darkMode={darkMode}
          />
        );
      case 'estimate':
        return (
          <QuickEstimator
            darkMode={darkMode}
            onBackToWelcome={handleBackToWelcome}
          />
        );
      case 'tracking':
        return (
          <TrackingView
            trackingData={trackingData}
            loadedFilename={loadedFilename}
            onBackToWelcome={handleBackToWelcome}
            darkMode={darkMode}
          />
        );
      default:
        return (
          <WelcomeScreen
            onStartEstimate={handleStartEstimate}
            onStartTracking={handleStartTracking}
            darkMode={darkMode}
          />
        );
    }
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ position: 'fixed', top: 8, right: 170, zIndex: 2000 }}>
        <IconButton size="small" onClick={() => setDarkMode((d) => !d)} color="inherit" aria-label="toggle dark mode">
          {darkMode ? <Brightness7Icon fontSize="small" /> : <Brightness4Icon fontSize="small" />}
        </IconButton>
      </Box>
      {renderCurrentView()}
    </ThemeProvider>
  );
}

export default App;