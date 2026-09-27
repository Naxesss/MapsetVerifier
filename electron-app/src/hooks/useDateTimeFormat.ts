import { useMemo } from 'react';
import { useSettings } from '../context/SettingsContext';
import { formatDate, formatDateTime, formatTime } from '../utils/dateTime';

type DateInput = Parameters<typeof formatDate>[0];

/**
 * Date and time formatters that follow the user's clock format setting. Use these for every
 * wall-clock time shown in the app, so 12/24-hour display stays consistent.
 */
export function useDateTimeFormat() {
  const clock = useSettings().settings.clockFormat;

  return useMemo(
    () => ({
      formatDate,
      formatTime: (value: DateInput, options?: { withSeconds?: boolean }) =>
        formatTime(value, clock, options),
      formatDateTime: (value: DateInput, options?: { withYear?: boolean; withSeconds?: boolean }) =>
        formatDateTime(value, clock, options),
    }),
    [clock]
  );
}
