import {
  AppShell,
  Container,
  CSSVariablesResolver,
  MantineProvider,
  ScrollArea,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Notifications } from '@mantine/notifications';
import { useLayoutEffect, useRef } from 'react';
import { Outlet } from 'react-router-dom';
import BackendGate from './components/backend/BackendGate.tsx';
import NavBars from './components/navbar/NavBars.tsx';
import UpdaterModal from './components/settings/UpdaterModal';
import WindowBar from './components/window/WindowBar.tsx';
import { BeatmapProvider, useBeatmap } from './context/BeatmapContext.tsx';
import { BeatmapReparseProvider } from './context/BeatmapReparseRegistry.tsx';
import { SettingsProvider } from './context/SettingsContext.tsx';
import { UpdaterProvider } from './context/UpdaterContext';
import { theme } from './theme/Theme.ts';
import '@mantine/core/styles.css';
import '@mantine/charts/styles.css';
import '@mantine/notifications/styles.css';
import './theme/global.scss';

export const cssVarResolver: CSSVariablesResolver = () => ({
  variables: {},
  light: {},
  dark: {
    // Default dark mode makes the text color use --mantine-color-dark-0 which we don't want
    '--mantine-color-text': '#fff',
    '--mantine-color-dimmed': '#9e9e9e',
  },
});

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

function App() {
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Notifications position="top-center" zIndex={2100} />
      <WindowBar />
      <SettingsProvider>
        <UpdaterProvider>
          <BeatmapProvider>
            <BackendGate>
              <AppShell
                header={{ height: 92 }}
                navbar={{
                  width: '256',
                  breakpoint: 'xs',
                  collapsed: { desktop: !desktopOpened },
                }}
              >
                <NavBars desktopOpened={desktopOpened} toggleDesktop={toggleDesktop} />
                <AppShell.Main>
                  <ScrollArea
                    offsetScrollbars
                    type="always"
                    h="calc(100vh - var(--app-shell-header-offset, 0rem) + var(--app-shell-padding))"
                  >
                    <Container p="sm" fluid>
                      <BeatmapReparseProvider>
                        <BeatmapKeyedOutlet />
                      </BeatmapReparseProvider>
                    </Container>
                  </ScrollArea>
                </AppShell.Main>
              </AppShell>
            </BackendGate>
          </BeatmapProvider>
          <UpdaterModal />
        </UpdaterProvider>
      </SettingsProvider>
    </MantineProvider>
  );
}

export default App;
