/** Removes the trailing dash suffix from a timestamp, then trims whitespace. */
export function trimTimestamp(timestamp: string) {
  return timestamp.replace(/\s*-\s*$/, '').trim();
}
