import { AppShell, Container, MantineProvider, ScrollArea } from '@mantine/core';
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
import ReferencePanel from './components/reference/ReferencePanel.tsx';
import UpdaterModal from './components/settings/UpdaterModal';
import SetupWizardGate from './components/setup/SetupWizardGate.tsx';
import WindowBar from './components/window/WindowBar.tsx';
import { BeatmapProvider, useBeatmap } from './context/BeatmapContext.tsx';
import { BeatmapReparseProvider } from './context/BeatmapReparseRegistry.tsx';
import { DocumentationProvider } from './context/DocumentationContext.tsx';
import { OverviewProvider } from './context/OverviewContext.tsx';
import { PageHintsProvider } from './context/PageHintsContext.tsx';
import { ReferencePanelProvider } from './context/ReferencePanelContext.tsx';
import { SettingsProvider } from './context/SettingsContext.tsx';
import { UpdaterProvider } from './context/UpdaterContext';
import { cssVarResolver } from './theme/cssVarResolver.ts';
import { NAV_BAR_HEIGHT, Z_INDEX } from './theme/layers.ts';
import { useAppTheme } from './theme/useAppTheme.ts';
import '@mantine/core/styles.css';
import '@mantine/charts/styles.css';
import '@mantine/notifications/styles.css';
import './theme/global.scss';

const MAPSET_SECTIONS = ['checks', 'snapshots', 'overview'];

/**
 * Switching pages renders a lightweight page skeleton first and the page itself a frame later.
 * The navbar and header update in the same frame as the click, and a page that is heavy to render
 * shows its skeleton meanwhile instead of freezing the app until it is done. Keyed by the first
 * path segment, so moving within a page (settings sections) is not delayed.
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

  // Only pages that are heavy to render get the skeleton first: the mapset pages once a mapset is
  // selected (without one they only show their empty state). Everything else, like Home and
  // Settings, renders straight away.
  const inMapsetFrame = MAPSET_SECTIONS.includes(section) && !!selectedFolder;

  const content = (
    <div ref={wrapRef} className="mv-route-outlet-wrap">
      {ready || !inMapsetFrame ? (
        // Mapset pages swap in place under their shared banner, without a fade.
        <div key={section} className={inMapsetFrame ? undefined : 'mv-deferred-content-enter'}>
          <Outlet />
        </div>
      ) : (
        <PageSkeleton />
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
  const isSettingsRoute = location.pathname.startsWith('/settings');

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Notifications position="top-center" zIndex={Z_INDEX.notifications} />
      <ErrorBoundary title="The app encountered an error">
        <WindowBar />
        <UpdaterProvider>
          <BeatmapProvider>
            <PageHintsProvider>
              <OverviewProvider>
                <BackendGate>
                  <SetupWizardGate>
                    <DocumentationProvider>
                      <ReferencePanelProvider>
                        <AppShell
                          header={{ height: NAV_BAR_HEIGHT }}
                          navbar={{
                            width: '256',
                            breakpoint: 'xs',
                            collapsed: { desktop: false, mobile: false },
                          }}
                        >
                          <BeatmapReparseProvider>
                            {!isSettingsRoute && <BeatmapSelectionNavigator />}
                            <NavBars showBeatmapSidebar={!isSettingsRoute} />
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
                        <ReferencePanel />
                      </ReferencePanelProvider>
                    </DocumentationProvider>
                  </SetupWizardGate>
                </BackendGate>
              </OverviewProvider>
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
