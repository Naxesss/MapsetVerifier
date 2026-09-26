import { Button, Modal, Stack, Text } from '@mantine/core';
import { IconArrowLeft } from '@tabler/icons-react';
import { useEffect, useRef, useState } from 'react';
import { DetailNavigationContext, DetailView, detailKey, detailTitle } from './detailNavigation';
import DocumentationCheckDetails from '../documentation/DocumentationCheckDetails';
import RcStatementDetails, { RcStatementTitle } from '../rankingCriteria/RcStatementDetails';

interface DetailModalProps {
  /** What to open the modal on, or null to close it. */
  view: DetailView | null;
  onClose: () => void;
}

/**
 * The one modal for ranking criteria statements and check documentation. Following a link between them
 * replaces its content instead of stacking another modal, and "Back" returns to what was shown before.
 */
export default function DetailModal({ view, onClose }: DetailModalProps) {
  const [history, setHistory] = useState<DetailView[]>(view ? [view] : []);
  const [openedKey, setOpenedKey] = useState(view ? detailKey(view) : null);
  const bodyRef = useRef<HTMLDivElement>(null);

  // Start over whenever it is opened on something else. Closing keeps the history so the content stays
  // in place while the modal animates out.
  const key = view ? detailKey(view) : null;
  if (key !== openedKey) {
    setOpenedKey(key);
    if (view) setHistory([view]);
  }

  const current = history[history.length - 1];
  const previous = history[history.length - 2] ?? null;
  const currentKey = current ? detailKey(current) : null;

  useEffect(() => {
    bodyRef.current?.closest('.mantine-Modal-content')?.scrollTo({ top: 0 });
  }, [currentKey]);

  const open = (next: DetailView) => {
    const index = history.findIndex((shown) => detailKey(shown) === detailKey(next));
    setHistory(index >= 0 ? history.slice(0, index + 1) : [...history, next]);
  };

  return (
    <Modal
      opened={view !== null}
      onClose={onClose}
      title={
        current && (
          <Stack gap={4}>
            {previous && (
              <Button
                variant="subtle"
                size="compact-sm"
                ml={-8}
                maw="100%"
                leftSection={<IconArrowLeft size={14} />}
                onClick={() => setHistory(history.slice(0, -1))}
                style={{ alignSelf: 'flex-start' }}
                styles={{ inner: { minWidth: 0 }, label: { minWidth: 0 } }}
              >
                <Text span inherit truncate>
                  Back to {detailTitle(previous)}
                </Text>
              </Button>
            )}
            {current.kind === 'rule' ? (
              <RcStatementTitle statement={current.statement} />
            ) : (
              <Text fw="bold" size="lg">
                {current.check.description}
              </Text>
            )}
          </Stack>
        )
      }
      yOffset="120px"
      size="80%"
      styles={{ content: { maxWidth: 1000 }, title: { flex: 1, minWidth: 0 } }}
    >
      <DetailNavigationContext.Provider value={{ open, previous }}>
        <div ref={bodyRef}>
          {current?.kind === 'rule' && <RcStatementDetails statement={current.statement} />}
          {current?.kind === 'check' && <DocumentationCheckDetails check={current.check} />}
        </div>
      </DetailNavigationContext.Provider>
    </Modal>
  );
}
