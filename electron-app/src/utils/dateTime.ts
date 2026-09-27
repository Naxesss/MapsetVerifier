/** Whether wall-clock times use a 24-hour clock (15:30) or a 12-hour clock (3:30 PM). */
export type ClockFormat = '24h' | '12h';

type DateInput = string | number | Date;

/** The clock the system locale uses, so a fresh install matches what the user is used to. */
export function detectClockFormat(): ClockFormat {
  try {
    const { hour12 } = new Intl.DateTimeFormat(undefined, { hour: 'numeric' }).resolvedOptions();
    return hour12 ? '12h' : '24h';
  } catch {
    return '24h';
  }
}

export function parseClockFormat(value: unknown): ClockFormat {
  return value === '12h' || value === '24h' ? value : detectClockFormat();
}

function toDate(value: DateInput): Date | null {
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

// hourCycle rather than hour12: hour12: false renders midnight as "24:00" in Chromium.
function timeOptions(clock: ClockFormat, withSeconds: boolean): Intl.DateTimeFormatOptions {
  return {
    hour: clock === '12h' ? 'numeric' : '2-digit',
    minute: '2-digit',
    second: withSeconds ? '2-digit' : undefined,
    hourCycle: clock === '12h' ? 'h12' : 'h23',
  };
}

function dateOptions(withYear: boolean): Intl.DateTimeFormatOptions {
  return { year: withYear ? 'numeric' : undefined, month: 'short', day: 'numeric' };
}

/** A calendar date, e.g. "Sep 27, 2026". Null for an unparseable value. */
export function formatDate(value: DateInput, { withYear = true } = {}): string | null {
  return toDate(value)?.toLocaleDateString(undefined, dateOptions(withYear)) ?? null;
}

/** A wall-clock time, e.g. "15:30" or "3:30 PM". Null for an unparseable value. */
export function formatTime(
  value: DateInput,
  clock: ClockFormat,
  { withSeconds = false } = {}
): string | null {
  return toDate(value)?.toLocaleTimeString(undefined, timeOptions(clock, withSeconds)) ?? null;
}

/** A date and time, e.g. "Sep 27, 2026, 15:30". Null for an unparseable value. */
export function formatDateTime(
  value: DateInput,
  clock: ClockFormat,
  { withYear = true, withSeconds = false } = {}
): string | null {
  return (
    toDate(value)?.toLocaleString(undefined, {
      ...dateOptions(withYear),
      ...timeOptions(clock, withSeconds),
    }) ?? null
  );
}
