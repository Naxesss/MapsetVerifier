import { Box, Kbd } from '@mantine/core';
import { IconPin } from '@tabler/icons-react';
import { getActiveNavRoute } from './navConfig.ts';
import MinorIcon from '../icons/MinorIcon.tsx';
import type { ReactNode } from 'react';

export type OverviewTab = 'Metadata' | 'Beatmap' | 'Difficulty' | 'Audio' | 'Objects';

export type PageHint = {
  id: string;
  content: ReactNode;
};

function copyTimestampHint(isMac: boolean): PageHint {
  return {
    id: 'copy-timestamp',
    content: (
      <>
        {isMac ? <Kbd size="xs">⌘</Kbd> : <Kbd size="xs">Ctrl</Kbd>} +{' '}
        <Kbd size="xs">Left Click</Kbd> to copy a timestamp.
      </>
    ),
  };
}

function refreshBeatmapHint(): PageHint {
  return {
    id: 'refresh-beatmap',
    content: (
      <>
        Press <Kbd size="xs">F5</Kbd> to refresh the beatmap.
      </>
    ),
  };
}

function contextClickHint(id: string, isMac: boolean, action: ReactNode): PageHint {
  return {
    id,
    content: isMac ? (
      <>
        <Kbd size="xs">⌃</Kbd> + <Kbd size="xs">Left Click</Kbd> {action}
      </>
    ) : (
      <>
        <Kbd size="xs">Right Click</Kbd> {action}
      </>
    ),
  };
}

function minorChecksDisabledHint(): PageHint {
  return {
    id: 'minor-checks-disabled',
    content: (
      <>
        Looking for{' '}
        <Box
          component="span"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
            verticalAlign: 'middle',
          }}
        >
          <MinorIcon size={16} />
          minor checks
        </Box>
        ? Enable them in Settings.
      </>
    ),
  };
}

function bookmarkHint(bookmarksEnabled: boolean): PageHint {
  if (bookmarksEnabled) {
    return {
      id: 'bookmark-pin',
      content:
        'Use the pin icon on a beatmapset in the sidebar to bookmark it, then filter the list to show bookmarked sets only.',
    };
  }

  return {
    id: 'bookmarks-disabled',
    content: (
      <>
        Looking to{' '}
        <Box
          component="span"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 4,
            verticalAlign: 'middle',
          }}
        >
          <IconPin size={16} />
          pin beatmapsets
        </Box>
        ? Enable bookmarks in Experimental Settings.
      </>
    ),
  };
}

function issueDetailsSidebarHint(): PageHint {
  return {
    id: 'issue-details-sidebar',
    content: (
      <>
        <Kbd size="xs">Left Click</Kbd> an issue to open its details and documentation in the sidebar.
      </>
    ),
  };
}

function commonHints(isMac: boolean): PageHint[] {
  return [copyTimestampHint(isMac), refreshBeatmapHint()];
}

export function getPageHints(
  pathname: string,
  overviewTab: OverviewTab | null,
  objectsHasHitsoundModes: boolean,
  isMac: boolean,
  showMinor: boolean,
  bookmarksEnabled: boolean
): PageHint[] {
  const route = getActiveNavRoute(pathname);

  if (!route) {
    return [];
  }

  if (route === '/documentation' || route === '/') {
    return [];
  }

  if (route === '/overview') {
    if (overviewTab === 'Audio' || overviewTab === 'Metadata') {
      return [refreshBeatmapHint()];
    }

    if (overviewTab === 'Objects') {
      const hints = [
        ...commonHints(isMac),
        contextClickHint(
          'timeline-rclick',
          isMac,
          'an object in the timeline to copy its timestamp.'
        ),
        {
          id: 'timeline-scroll-mode',
          content: (
            <>
              Hold <Kbd size="xs">Shift</Kbd> over the timeline for{' '}
              <strong>timeline scroll mode</strong>. While <Kbd size="xs">Shift</Kbd> is held, the
              wheel steps timing snap ticks.
            </>
          ),
        },
        ...(objectsHasHitsoundModes
          ? [
              {
                id: 'hitsound-view',
                content:
                  'Use the Structure / Hitsounding toggle in the timeline to access the hitsounding overview.',
              } satisfies PageHint,
            ]
          : []),
        {
          id: 'table-cell',
          content: (
            <>
              <Kbd size="xs">Left Click</Kbd> on a cell in the tables to view a list of all related
              objects.
            </>
          ),
        },
      ];
      return hints;
    }

    if (overviewTab === 'Beatmap') {
      return [
        ...commonHints(isMac),
        {
          id: 'cell-groups',
          content:
            'Certain matching values across difficulties share the same cell highlight color.',
        },
      ];
    }

    if (overviewTab === 'Difficulty') {
      return [
        ...commonHints(isMac),
        {
          id: 'chart-zoom',
          content: (
            <>
              <Kbd size="xs">Left Click</Kbd> and drag on a chart to zoom in.
            </>
          ),
        },
        contextClickHint('chart-rclick', isMac, 'on charts for timestamp actions.'),
      ];
    }
  }

  if (route === '/checks') {
    return [
      ...commonHints(isMac),
      issueDetailsSidebarHint(),
      bookmarkHint(bookmarksEnabled),
      ...(!showMinor ? [minorChecksDisabledHint()] : []),
    ];
  }

  return commonHints(isMac);
}
