import { createContext, useContext } from 'react';

export const RealtimeAvailabilityContext = createContext(false);

export function useRealtimeStatusAvailability() {
  return useContext(RealtimeAvailabilityContext);
}
