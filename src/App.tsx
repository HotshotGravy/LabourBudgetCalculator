import React, { useState } from 'react';
import QuickEstimator from './components/QuickEstimator';
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

function App() {
  const [darkMode, setDarkMode] = useState(false);
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

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ position: 'fixed', top: 8, right: 16, zIndex: 2000 }}>
        <IconButton size="small" onClick={() => setDarkMode((d) => !d)} color="inherit" aria-label="toggle dark mode">
          {darkMode ? <Brightness7Icon fontSize="small" /> : <Brightness4Icon fontSize="small" />}
        </IconButton>
      </Box>
      <QuickEstimator darkMode={darkMode} />
    </ThemeProvider>
  );
}

export default App;