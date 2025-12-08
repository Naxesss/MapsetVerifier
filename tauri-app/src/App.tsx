import {AppShell, Container, CSSVariablesResolver, MantineProvider, ScrollArea, Text} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Route, Routes } from 'react-router-dom';
import BackendGate from './components/backend/BackendGate.tsx';
import Checks from './components/checks/Checks.tsx';
import Documentation from './components/documentation/Documentation.tsx';
import Home from "./components/home/Home.tsx";
import NavBars from './components/navbar/NavBars.tsx';
import Snapshots from './components/snapshots/Snapshots.tsx';
import WindowBar from "./components/window/WindowBar.tsx";
import { BeatmapProvider } from "./context/BeatmapContext.tsx";
import { SettingsProvider } from "./context/SettingsContext.tsx";
import { theme } from './theme/Theme.ts';
import '@mantine/core/styles.css';
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

function App() {
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <WindowBar />
      <SettingsProvider>
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
                    <Routes>
                      <Route>
                        <Route path="/" element={<Home />} />
                        <Route path="/documentation" element={<Documentation />} />
                        <Route path="/checks" element={<Checks />} />
                        <Route path="/snapshots" element={<Snapshots />} />
                        <Route path="*" element={<Text>404</Text>} />
                      </Route>
                    </Routes>
                  </Container>
                </ScrollArea>
              </AppShell.Main>
            </AppShell>
          </BackendGate>
        </BeatmapProvider>
      </SettingsProvider>
    </MantineProvider>
  );
}

export default App;
