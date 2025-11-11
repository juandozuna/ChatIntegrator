import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import { Route, Routes, Navigate } from 'react-router-dom';
import MentionsPage from './components/MentionsPage';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#0066ff'
    }
  }
});

export default function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Routes>
        <Route path="/" element={<Navigate to="/mentions" />} />
        <Route path="/mentions" element={<MentionsPage />} />
      </Routes>
    </ThemeProvider>
  );
}
