import { Anchor, Box, Kbd } from '@mantine/core';
import { IconPin } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { getActiveNavRoute } from './navConfig.ts';
import MinorIcon from '../icons/MinorIcon.tsx';
import type { ReactNode } from 'react';

export type OverviewTab =
  | 'Summary'
  | 'General'
  | 'Beatmap'
  | 'Difficulty'
  | 'Audio'
  | 'Video'
  | 'Objects';

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
        Press <Kbd size="xs">F5</Kbd> to refresh the mapset.
      </>
    ),
  };
}

function badgeFilterHint(id: string, what: string): PageHint {
  return {
    id,
    content: (
      <>
        <Kbd size="xs">Left Click</Kbd> a count badge to filter {what}.
      </>
    ),
  };
}

function difficultyBarHint(): PageHint {
  return {
    id: 'difficulty-bar',
    content: (
      <>
        <Kbd size="xs">Left Click</Kbd> a segment in the bar under the difficulty picker to jump to
        that difficulty.
      </>
    ),
  };
}

function difficultyStepHint(): PageHint {
  return {
    id: 'difficulty-step',
    content: (
      <>
        Press <Kbd size="xs">[</Kbd> or <Kbd size="xs">]</Kbd> to go to the previous or next
        difficulty.
      </>
    ),
  };
}

function overviewPicksHint(): PageHint {
  return {
    id: 'overview-picks',
    content: (
      <>
        <Kbd size="xs">Left Click</Kbd> a segment in the bar under the picker to show or hide that
        difficulty.
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
          negligible checks
        </Box>
        ?{' '}
        <Anchor component={Link} to="/settings/checks" inherit>
          Turn them on in Settings
        </Anchor>
        .
      </>
    ),
  };
}

function bookmarkHint(bookmarksEnabled: boolean): PageHint {
  if (bookmarksEnabled) {
    return {
      id: 'bookmark-pin',
      content:
        'Use the pin icon on a mapset in the sidebar to bookmark it, then filter the list to show bookmarked mapsets only.',
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
          pin mapsets
        </Box>
        ?{' '}
        <Anchor component={Link} to="/settings/experimental" inherit>
          Turn on bookmarks in Settings
        </Anchor>
        .
      </>
    ),
  };
}

function issueDetailsSidebarHint(): PageHint {
  return {
    id: 'issue-details-sidebar',
    content: (
      <>
        <Kbd size="xs">Left Click</Kbd> an issue to open its details and documentation in the
        sidebar.
      </>
    ),
  };
}

function checkRunDeltaDisabledHint(): PageHint {
  return {
    id: 'check-run-delta-disabled',
    content: (
      <>
        Want to see what changed since your last check run? Enable &quot;Show check changes since
        last run&quot; in Settings.
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
  bookmarksEnabled: boolean,
  showCheckRunDelta: boolean
): PageHint[] {
  const route = getActiveNavRoute(pathname);

  if (!route) {
    return [];
  }

  if (route === '/documentation' || route === '/ranking-criteria' || route === '/') {
    return [];
  }

  if (route === '/overview') {
    if (overviewTab === 'Audio' || overviewTab === 'Video' || overviewTab === 'General') {
      return [refreshBeatmapHint()];
    }

    if (overviewTab === 'Summary') {
      return [refreshBeatmapHint(), overviewPicksHint()];
    }

    if (overviewTab === 'Objects') {
      const hints = [
        ...commonHints(isMac),
        overviewPicksHint(),
        contextClickHint(
          'timeline-rclick',
          isMac,
          'an object in the timeline to copy its timestamp.'
        ),
        {
          id: 'timeline-pan',
          content: (
            <>
              Drag the timeline sideways to pan, and drag a row&apos;s grip to reorder the
              difficulties.
            </>
          ),
        },
        {
          id: 'timeline-scroll-mode',
          content: (
            <>
              Hold <Kbd size="xs">Shift</Kbd> and scroll over the timeline to step through timing
              snap ticks.
            </>
          ),
        },
        {
          id: 'timeline-zoom',
          content: (
            <>
              Hold <Kbd size="xs">Ctrl</Kbd> and scroll over the timeline to zoom in or out.
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
        {
          id: 'cell-popup-filter',
          content: (
            <>
              <Kbd size="xs">Left Click</Kbd> a count badge in a cell&apos;s popup to filter its
              list by type.
            </>
          ),
        },
        {
          id: 'column-usage-hover',
          content: 'Hover a column usage cell (osu!mania) to see its notes and hold notes.',
        },
      ];
      return hints;
    }

    if (overviewTab === 'Beatmap') {
      return [
        ...commonHints(isMac),
        overviewPicksHint(),
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
        overviewPicksHint(),
        {
          id: 'chart-zoom',
          content: (
            <>
              <Kbd size="xs">Left Click</Kbd> and drag on a chart to zoom in.
            </>
          ),
        },
        contextClickHint('chart-rclick', isMac, 'on charts for timestamp actions.'),
        {
          id: 'chart-legend',
          content: (
            <>
              <Kbd size="xs">Left Click</Kbd> a legend entry to show only that line;{' '}
              {isMac ? <Kbd size="xs">⌘</Kbd> : <Kbd size="xs">Ctrl</Kbd>} +{' '}
              <Kbd size="xs">Left Click</Kbd> to show or hide just that one.
            </>
          ),
        },
      ];
    }
  }

  if (route === '/checks') {
    return [
      ...commonHints(isMac),
      difficultyBarHint(),
      difficultyStepHint(),
      badgeFilterHint('severity-filter', 'issues by severity'),
      issueDetailsSidebarHint(),
      bookmarkHint(bookmarksEnabled),
      ...(!showMinor ? [minorChecksDisabledHint()] : []),
      ...(!showCheckRunDelta ? [checkRunDeltaDisabledHint()] : []),
    ];
  }

  if (route === '/snapshots') {
    return [
      ...commonHints(isMac),
      difficultyBarHint(),
      difficultyStepHint(),
      badgeFilterHint('diff-type-filter', 'changes by type'),
    ];
  }

  return commonHints(isMac);
}
