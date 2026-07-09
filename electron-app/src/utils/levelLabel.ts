import { Level } from '../Types';

/** Display label for a level. "Minor" is shown to users as "Negligible". */
export function getLevelLabel(level: Exclude<Level, 'Check'>): string {
  return level === 'Minor' ? 'Negligible' : level;
}
