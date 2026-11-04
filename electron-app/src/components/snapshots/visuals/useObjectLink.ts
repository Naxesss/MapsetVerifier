import { useCallback, type MouseEvent } from 'react';
import { useOpenOsuTimestamp } from '../../../hooks/useOpenOsuTimestamp.ts';
import { notifyError, notifySuccess } from '../../../utils/notify.tsx';
import { isCopyModifierClick } from '../../common/osuLinkUtils.ts';
import { formatClock } from '../describe';
import type { PairedObject } from './pairing';

/**
 * The editor timestamp that selects an object, as the checks write it: its combo number, or
 * time|column in mania. A removed object is no longer in the map, so only its time is linked.
 */
export function objectStamp(pair: PairedObject): string {
  const object = pair.after;
  if (!object) return formatClock(pair.before!.time);

  const ref =
    object.column != null
      ? `${Math.round(object.time)}|${object.column}`
      : object.combo != null
        ? String(object.combo)
        : null;

  return ref ? `${formatClock(object.time)} (${ref})` : formatClock(object.time);
}

/**
 * Props that make an object in a picture work like a timestamp link: a click opens it in osu!,
 * a ctrl-click copies the timestamp.
 */
export function useObjectLink() {
  const openOsuTimestamp = useOpenOsuTimestamp();

  return useCallback(
    (pair: PairedObject) => {
      const stamp = objectStamp(pair);

      const handle = async (event: MouseEvent) => {
        if (isCopyModifierClick(event)) {
          event.preventDefault();
          try {
            await navigator.clipboard.writeText(stamp);
            notifySuccess(`Copied ${stamp}`);
          } catch {
            notifyError('Clipboard is unavailable.');
          }
          return;
        }

        if (event.button !== 0 && event.button !== 1) return;
        event.preventDefault();
        await openOsuTimestamp(stamp);
      };

      return { onClick: handle, onAuxClick: handle, style: { cursor: 'pointer' } };
    },
    [openOsuTimestamp]
  );
}
