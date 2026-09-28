import { AppShell, Container, MantineProvider, ScrollArea } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Notifications } from '@mantine/notifications';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import BackendGate from './components/backend/BackendGate.tsx';
import BeatmapSelectionNavigator from './components/beatmaps/BeatmapSelectionNavigator.tsx';
import ErrorBoundary from './components/common/ErrorBoundary.tsx';
import { PageSkeleton } from './components/common/LoadingSkeletons.tsx';
import MapsetFrame from './components/common/MapsetFrame.tsx';
import RouteErrorBoundary from './components/common/RouteErrorBoundary.tsx';
import NavBars from './components/navbar/NavBars.tsx';
import UpdaterModal from './components/settings/UpdaterModal';
import SetupWizardGate from './components/setup/SetupWizardGate.tsx';
import WindowBar from './components/window/WindowBar.tsx';
import { BeatmapProvider, useBeatmap } from './context/BeatmapContext.tsx';
import { BeatmapReparseProvider } from './context/BeatmapReparseRegistry.tsx';
import { DocumentationProvider } from './context/DocumentationContext.tsx';
import { PageHintsProvider } from './context/PageHintsContext.tsx';
import { SettingsProvider } from './context/SettingsContext.tsx';
import { UpdaterProvider } from './context/UpdaterContext';
import { cssVarResolver } from './theme/cssVarResolver.ts';
import { NAV_BAR_HEIGHT, WINDOW_BAR_HEIGHT, Z_INDEX } from './theme/layers.ts';
import { useAppTheme } from './theme/useAppTheme.ts';
import '@mantine/core/styles.css';
import '@mantine/charts/styles.css';
import '@mantine/notifications/styles.css';
import './theme/global.scss';

const MAPSET_SECTIONS = ['checks', 'snapshots', 'overview'];
/** Pages with long lists that take a noticeable moment to render. */
const LIST_SECTIONS = ['documentation', 'ranking-criteria'];

/**
 * Switching pages renders a lightweight page skeleton first and the page itself a frame later.
 * The navbar and header update in the same frame as the click, and a page that is heavy to render
 * shows its skeleton meanwhile instead of freezing the app until it is done. Keyed by the first
 * path segment, so moving within a page (settings sections, ranking criteria pages) is not delayed.
 */
function useDeferredSection() {
  const location = useLocation();
  const section = location.pathname.split('/')[1] ?? '';
  const [readySection, setReadySection] = useState<string | null>(null);

  useEffect(() => {
    if (readySection === section) return;

    let inner = 0;
    const outer = requestAnimationFrame(() => {
      inner = requestAnimationFrame(() => setReadySection(section));
    });
    return () => {
      cancelAnimationFrame(outer);
      cancelAnimationFrame(inner);
    };
  }, [section, readySection]);

  return { section, ready: readySection === section };
}

function BeatmapKeyedOutlet() {
  const { selectedFolder } = useBeatmap();
  const { section, ready } = useDeferredSection();
  const wrapRef = useRef<HTMLDivElement>(null);
  const skipBeatmapFadeRef = useRef(true);

  useLayoutEffect(() => {
    if (skipBeatmapFadeRef.current) {
      skipBeatmapFadeRef.current = false;
      return;
    }
    const el = wrapRef.current;
    if (!el) return;
    el.style.animation = 'none';
    void el.offsetHeight;
    el.style.removeProperty('animation');
  }, [selectedFolder]);

  // Only pages that are heavy to render get the skeleton first: the list pages, and the mapset pages
  // once a mapset is selected (without one they only show their empty state). Everything else,
  // like Home and Settings, renders straight away.
  const isMapsetPage = MAPSET_SECTIONS.includes(section);
  const inMapsetFrame = isMapsetPage && !!selectedFolder;
  const isHeavyPage = LIST_SECTIONS.includes(section) || inMapsetFrame;

  const content = (
    <div ref={wrapRef} className="mv-route-outlet-wrap">
      {ready || !isHeavyPage ? (
        // Mapset pages swap in place under their shared banner, without a fade.
        <div key={section} className={inMapsetFrame ? undefined : 'mv-deferred-content-enter'}>
          <Outlet />
        </div>
      ) : (
        <PageSkeleton variant={isMapsetPage ? 'mapset' : 'list'} />
      )}
    </div>
  );

  // The mapset pages share one frame with the mapset's art and title, kept mounted while switching
  // between them, so only the page below the title swaps.
  return inMapsetFrame ? <MapsetFrame>{content}</MapsetFrame> : content;
}

function AppContent() {
  const theme = useAppTheme();
  const location = useLocation();
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  const isSettingsRoute = location.pathname.startsWith('/settings');
  const isNavbarOpened = desktopOpened || isSettingsRoute;

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Notifications position="top-center" zIndex={Z_INDEX.notifications} />
      <ErrorBoundary title="The app encountered an error">
        <WindowBar />
        <UpdaterProvider>
          <BeatmapProvider>
            <PageHintsProvider>
              <BackendGate>
                <SetupWizardGate>
                  <DocumentationProvider>
                    <AppShell
                      header={{ height: WINDOW_BAR_HEIGHT + NAV_BAR_HEIGHT }}
                      navbar={{
                        width: '256',
                        breakpoint: 'xs',
                        collapsed: {
                          desktop: !isNavbarOpened,
                          mobile: false,
                        },
                      }}
                    >
                      <BeatmapReparseProvider>
                        {!isSettingsRoute && <BeatmapSelectionNavigator />}
                        <NavBars
                          desktopOpened={isNavbarOpened}
                          showBeatmapSidebar={!isSettingsRoute}
                          toggleDesktop={isSettingsRoute ? undefined : toggleDesktop}
                        />
                        <AppShell.Main pb={isSettingsRoute ? 0 : undefined}>
                          <ScrollArea
                            offsetScrollbars
                            type="always"
                            scrollbars={isSettingsRoute ? 'y' : undefined}
                            h="calc(100vh - var(--app-shell-header-offset, 0rem) + var(--app-shell-padding))"
                          >
                            {/* The one page frame: every page gets a 16px gutter and adds no outer
                                padding of its own. The top offset matches the sidebar's search row
                                (xs), so the first row of every page lines up with it. */}
                            <Container px="md" pt="xs" pb="md" fluid>
                              <RouteErrorBoundary>
                                <BeatmapKeyedOutlet />
                              </RouteErrorBoundary>
                            </Container>
                          </ScrollArea>
                        </AppShell.Main>
                      </BeatmapReparseProvider>
                    </AppShell>
                  </DocumentationProvider>
                </SetupWizardGate>
              </BackendGate>
            </PageHintsProvider>
          </BeatmapProvider>
          <UpdaterModal />
        </UpdaterProvider>
      </ErrorBoundary>
    </MantineProvider>
  );
}

export default function App() {
  return (
    <SettingsProvider>
      <AppContent />
    </SettingsProvider>
  );
}
