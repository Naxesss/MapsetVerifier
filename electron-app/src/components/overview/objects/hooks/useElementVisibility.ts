import { useEffect, useState, type RefObject } from 'react';

// Generous vertical buffer so rows mount/draw just before they scroll into view.
const ROOT_MARGIN = '400px 0px';

export function useElementVisibility(ref: RefObject<HTMLElement | null>): boolean {
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        setIsVisible(entry?.isIntersecting ?? true);
      },
      { rootMargin: ROOT_MARGIN }
    );

    observer.observe(element);

    return () => {
      observer.disconnect();
    };
  }, [ref]);

  return isVisible;
}
