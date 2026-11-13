import { Box, Button, Drawer, SegmentedControl, Stack, Text } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconArrowLeft } from '@tabler/icons-react';
import { useLayoutEffect, useRef, useState } from 'react';
import { useReferencePanel, type ReferenceTab } from '../../context/ReferencePanelContext';
import { Z_INDEX } from '../../theme/layers';
import { SectionTitle } from '../common/Headings';
import { DetailNavigationContext, detailKey, detailTitle } from '../details/detailNavigation';
import DocumentationChecksBrowser from '../documentation/DocumentationBrowser';
import DocumentationCheckDetails from '../documentation/DocumentationCheckDetails';
import RankingCriteriaBrowser from '../rankingCriteria/RankingCriteriaBrowser';
import RcStatementDetails, { RcStatementTitle } from '../rankingCriteria/RcStatementDetails';

const TABS: { value: ReferenceTab; label: string }[] = [
  { value: 'rules', label: 'Ranking criteria' },
  { value: 'checks', label: 'Checks' },
];

/** Below this window width the panel takes the whole width instead of covering part of the page. */
const FULL_WIDTH_QUERY = '(max-width: 760px)';
const PANEL_WIDTH = 600;

/**
 * The ranking criteria and check documentation, slid in from the right over whatever page is open.
 * A list per tab, and a rule or check opened from one shown in its place; links between rules and
 * checks replace what is shown, and "Back" steps back to the previous one and then to the list.
 */
export default function ReferencePanel() {
  const panel = useReferencePanel();
  const fullWidth = useMediaQuery(FULL_WIDTH_QUERY);
  const bodyRef = useRef<HTMLDivElement>(null);
  const listScrollTop = useRef(0);

  // The lists mount on first use and then stay, so search and scroll survive closing the panel and
  // opening a rule. Nothing loads until the panel is first opened.
  const [visitedTabs, setVisitedTabs] = useState<ReferenceTab[]>([]);
  if (panel.opened && !visitedTabs.includes(panel.tab)) {
    setVisitedTabs([...visitedTabs, panel.tab]);
  }

  const { history } = panel;
  const current = history[history.length - 1] ?? null;
  const previous = history[history.length - 2] ?? null;
  const currentKey = current ? detailKey(current) : null;

  // Leaving the list remembers where it was scrolled to; coming back restores it. Each rule or check
  // starts at the top.
  const wasShowingList = useRef(true);
  useLayoutEffect(() => {
    const body = bodyRef.current;
    if (!body) return;
    if (currentKey === null) {
      if (!wasShowingList.current) body.scrollTop = listScrollTop.current;
    } else {
      body.scrollTop = 0;
    }
    wasShowingList.current = currentKey === null;
  }, [currentKey]);

  const rememberListScroll = () => {
    if (currentKey === null && bodyRef.current) listScrollTop.current = bodyRef.current.scrollTop;
  };

  const backLabel = previous
    ? `Back to ${detailTitle(previous)}`
    : `Back to ${panel.tab === 'rules' ? 'ranking criteria' : 'checks'}`;

  return (
    <Drawer.Root
      opened={panel.opened}
      onClose={panel.close}
      position="right"
      size={fullWidth ? '100%' : PANEL_WIDTH}
      zIndex={Z_INDEX.referencePanel}
      // The page never scrolls itself (each page has its own scroll area), so locking it only padded
      // the page for a scrollbar that isn't there, making the content behind the panel narrower.
      lockScroll={false}
      // Below the nav bar, like the issue details drawer.
      styles={{
        overlay: {
          top: 'var(--mv-top-chrome-height)',
          height: 'calc(100dvh - var(--mv-top-chrome-height))',
        },
        content: {
          marginTop: 'var(--mv-top-chrome-height)',
          height: 'calc(100dvh - var(--mv-top-chrome-height))',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        },
      }}
    >
      <Drawer.Overlay />
      <Drawer.Content aria-label="Documentation">
        <Drawer.Header
          style={{ alignItems: 'flex-start', paddingBottom: 'var(--mantine-spacing-sm)' }}
        >
          <Stack gap="sm" style={{ flex: 1, minWidth: 0 }}>
            {current ? (
              <>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  ml={-8}
                  maw="100%"
                  leftSection={<IconArrowLeft size={14} />}
                  onClick={panel.back}
                  style={{ alignSelf: 'flex-start' }}
                  styles={{ inner: { minWidth: 0 }, label: { minWidth: 0 } }}
                >
                  <Text span inherit truncate>
                    {backLabel}
                  </Text>
                </Button>
                {current.kind === 'rule' ? (
                  <RcStatementTitle statement={current.statement} />
                ) : (
                  <SectionTitle component="span">{current.check.description}</SectionTitle>
                )}
              </>
            ) : (
              <>
                <Drawer.Title>
                  <SectionTitle component="span">Documentation</SectionTitle>
                </Drawer.Title>
                <SegmentedControl
                  data={TABS}
                  value={panel.tab}
                  onChange={(value) => {
                    panel.setTab(value as ReferenceTab);
                    bodyRef.current?.scrollTo({ top: 0 });
                  }}
                  style={{ alignSelf: 'flex-start' }}
                />
              </>
            )}
          </Stack>
          <Drawer.CloseButton aria-label="Close documentation" />
        </Drawer.Header>

        <Drawer.Body
          ref={bodyRef}
          style={{ flex: 1, overflowY: 'auto', paddingTop: 0 }}
          onScroll={rememberListScroll}
        >
          <DetailNavigationContext.Provider value={{ open: panel.openDetail, previous }}>
            {current?.kind === 'rule' && <RcStatementDetails statement={current.statement} />}
            {current?.kind === 'check' && <DocumentationCheckDetails check={current.check} />}
          </DetailNavigationContext.Provider>

          {visitedTabs.includes('rules') && (
            <Box display={current || panel.tab !== 'rules' ? 'none' : undefined}>
              <RankingCriteriaBrowser />
            </Box>
          )}
          {visitedTabs.includes('checks') && (
            <Box display={current || panel.tab !== 'checks' ? 'none' : undefined}>
              <DocumentationChecksBrowser />
            </Box>
          )}
        </Drawer.Body>
      </Drawer.Content>
    </Drawer.Root>
  );
}
