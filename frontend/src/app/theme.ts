import { createTheme } from '@mui/material/styles';

export type ColorMode = 'light' | 'dark';

export function createAppTheme(mode: ColorMode) {
  return createTheme({
  palette: {
    mode,
    primary: {
      main: '#155eef'
    },
    ...(mode === 'light'
      ? { background: { default: '#f8fafc', paper: '#ffffff' } }
      : { background: { default: '#101828', paper: '#1d2939' } })
  },
  shape: {
    borderRadius: 10
  },
  typography: {
    fontFamily: 'Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    h4: {
      fontWeight: 700
    },
    h5: {
      fontWeight: 700
    }
  },
  components: {
    MuiButton: {
      defaultProps: {
        disableElevation: true
      }
    }
  }
  });
}
