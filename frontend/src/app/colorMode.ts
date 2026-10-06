import { createContext, useContext } from 'react';
import type { ColorMode } from './theme';

export type ColorModeContextValue = { mode: ColorMode; toggleColorMode: () => void };

export const ColorModeContext = createContext<ColorModeContextValue | undefined>(undefined);

export function useColorMode(): ColorModeContextValue {
  const value = useContext(ColorModeContext);
  if (!value) {
    throw new Error('useColorMode must be used within AppProviders.');
  }

  return value;
}
