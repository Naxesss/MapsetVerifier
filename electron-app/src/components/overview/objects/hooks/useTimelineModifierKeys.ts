import { useEffect, useState } from 'react';

export type TimelineModifierKeys = {
  shiftHeld: boolean;
  ctrlHeld: boolean;
};

export function useTimelineModifierKeys(): TimelineModifierKeys {
  const [shiftHeld, setShiftHeld] = useState(false);
  const [ctrlHeld, setCtrlHeld] = useState(false);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Shift') {
        setShiftHeld(true);
      }
      if (event.key === 'Control' || event.key === 'Meta') {
        setCtrlHeld(true);
      }
    };

    const handleKeyUp = (event: KeyboardEvent) => {
      if (event.key === 'Shift') {
        setShiftHeld(false);
      }
      if (event.key === 'Control' || event.key === 'Meta') {
        setCtrlHeld(false);
      }
    };

    const handleBlur = () => {
      setShiftHeld(false);
      setCtrlHeld(false);
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    window.addEventListener('blur', handleBlur);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
      window.removeEventListener('blur', handleBlur);
    };
  }, []);

  return { shiftHeld, ctrlHeld };
}
