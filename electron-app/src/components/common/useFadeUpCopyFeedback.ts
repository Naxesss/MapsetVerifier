import { useEffect, useRef, useState } from 'react';

/** How long the feedback stays fully visible before it fades, so it can be read. */
const COPY_FEEDBACK_HOLD_MS = 400;
/** Keep in sync with the `.mv-copy-bubble` transition in global.scss. */
const COPY_FEEDBACK_FADE_MS = 300;

export function useFadeUpCopyFeedback() {
  const [showCopied, setShowCopied] = useState(false);
  const [copiedAnimating, setCopiedAnimating] = useState(false);
  const fadeTimeoutRef = useRef<number | undefined>(undefined);
  const hideTimeoutRef = useRef<number | undefined>(undefined);

  useEffect(
    () => () => {
      window.clearTimeout(fadeTimeoutRef.current);
      window.clearTimeout(hideTimeoutRef.current);
    },
    []
  );

  const triggerCopyFeedback = () => {
    window.clearTimeout(fadeTimeoutRef.current);
    window.clearTimeout(hideTimeoutRef.current);
    setShowCopied(true);
    setCopiedAnimating(false);
    fadeTimeoutRef.current = window.setTimeout(
      () => setCopiedAnimating(true),
      COPY_FEEDBACK_HOLD_MS
    );
    hideTimeoutRef.current = window.setTimeout(() => {
      setShowCopied(false);
      setCopiedAnimating(false);
    }, COPY_FEEDBACK_HOLD_MS + COPY_FEEDBACK_FADE_MS);
  };

  return { showCopied, copiedAnimating, triggerCopyFeedback };
}
