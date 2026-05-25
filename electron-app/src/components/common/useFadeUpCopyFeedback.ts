import { useEffect, useRef, useState } from 'react';

const COPY_FEEDBACK_MS = 650;

export function useFadeUpCopyFeedback() {
  const [showCopied, setShowCopied] = useState(false);
  const [copiedAnimating, setCopiedAnimating] = useState(false);
  const feedbackTimeoutRef = useRef<number | undefined>(undefined);

  useEffect(
    () => () => {
      window.clearTimeout(feedbackTimeoutRef.current);
    },
    []
  );

  const triggerCopyFeedback = () => {
    window.clearTimeout(feedbackTimeoutRef.current);
    setShowCopied(true);
    setCopiedAnimating(false);
    requestAnimationFrame(() => {
      requestAnimationFrame(() => setCopiedAnimating(true));
    });
    feedbackTimeoutRef.current = window.setTimeout(() => {
      setShowCopied(false);
      setCopiedAnimating(false);
    }, COPY_FEEDBACK_MS);
  };

  return { showCopied, copiedAnimating, triggerCopyFeedback };
}
