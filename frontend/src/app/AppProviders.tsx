import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useMemo, useState, type PropsWithChildren } from 'react';
import { ColorModeContext, type ColorModeContextValue } from './colorMode';
import { createAppTheme, type ColorMode } from './theme';

const colorModeStorageKey = 'order-management.color-mode';

function getInitialColorMode(): ColorMode {
  return localStorage.getItem(colorModeStorageKey) === 'dark' ? 'dark' : 'light';
}

export function AppProviders({ children }: PropsWithChildren) {
  const [mode, setMode] = useState<ColorMode>(getInitialColorMode);
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            refetchOnWindowFocus: false
          }
        }
      })
  );
  const theme = useMemo(() => createAppTheme(mode), [mode]);
  const colorMode = useMemo<ColorModeContextValue>(() => ({
    mode,
    toggleColorMode: () => {
      setMode((currentMode) => {
        const nextMode = currentMode === 'light' ? 'dark' : 'light';
        localStorage.setItem(colorModeStorageKey, nextMode);
        return nextMode;
      });
    }
  }), [mode]);

  return (
    <QueryClientProvider client={queryClient}>
      <ColorModeContext.Provider value={colorMode}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </ColorModeContext.Provider>
    </QueryClientProvider>
  );
}
