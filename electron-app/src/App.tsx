import { AppShell, Container, MantineProvider, ScrollArea } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Notifications } from '@mantine/notifications';
import { useLayoutEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import BackendGate from './components/backend/BackendGate.tsx';
import BeatmapSelectionNavigator from './components/beatmaps/BeatmapSelectionNavigator.tsx';
import ErrorBoundary from './components/common/ErrorBoundary.tsx';
import RouteErrorBoundary from './components/common/RouteErrorBoundary.tsx';
import NavBars from './components/navbar/NavBars.tsx';
import UpdaterModal from './components/settings/UpdaterModal';
import WindowBar from './components/window/WindowBar.tsx';
import { BeatmapProvider, useBeatmap } from './context/BeatmapContext.tsx';
import { BeatmapReparseProvider } from './context/BeatmapReparseRegistry.tsx';
import { DocumentationProvider } from './context/DocumentationContext.tsx';
import { PageHintsProvider } from './context/PageHintsContext.tsx';
import { SettingsProvider } from './context/SettingsContext.tsx';
import { UpdaterProvider } from './context/UpdaterContext';
import { cssVarResolver } from './theme/cssVarResolver.ts';
import { useAppTheme } from './theme/useAppTheme.ts';
import '@mantine/core/styles.css';
import '@mantine/charts/styles.css';
import '@mantine/notifications/styles.css';
import './theme/global.scss';

function BeatmapKeyedOutlet() {
  const { selectedFolder } = useBeatmap();
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

  return (
    <div ref={wrapRef} className="mv-route-outlet-wrap">
      <Outlet />
    </div>
  );
}

function AppContent() {
  const theme = useAppTheme();
  const location = useLocation();
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  const isSettingsRoute = location.pathname.startsWith('/settings');

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Notifications position="top-center" zIndex={2100} />
      <ErrorBoundary title="The app encountered an error">
        <WindowBar />
        <UpdaterProvider>
          <BeatmapProvider>
            <PageHintsProvider>
              <BackendGate>
                <DocumentationProvider>
                  <AppShell
                    header={{ height: 92 }}
                    navbar={{
                      width: '256',
                      breakpoint: 'xs',
                      collapsed: {
                        desktop: isSettingsRoute || !desktopOpened,
                        mobile: isSettingsRoute,
                      },
                    }}
                  >
                    <BeatmapReparseProvider>
                      {!isSettingsRoute && <BeatmapSelectionNavigator />}
                      <NavBars
                        desktopOpened={desktopOpened}
                        showBeatmapSidebar={!isSettingsRoute}
                        toggleDesktop={toggleDesktop}
                      />
                      <AppShell.Main pb={isSettingsRoute ? 0 : undefined}>
                        <ScrollArea
                          offsetScrollbars
                          type="always"
                          scrollbars={isSettingsRoute ? 'y' : undefined}
                          h="calc(100vh - var(--app-shell-header-offset, 0rem) + var(--app-shell-padding))"
                        >
                          <Container
                            py={isSettingsRoute ? 0 : undefined}
                            px={isSettingsRoute ? 0 : 'sm'}
                            fluid
                          >
                            <RouteErrorBoundary>
                              <BeatmapKeyedOutlet />
                            </RouteErrorBoundary>
                          </Container>
                        </ScrollArea>
                      </AppShell.Main>
                    </BeatmapReparseProvider>
                  </AppShell>
                </DocumentationProvider>
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
