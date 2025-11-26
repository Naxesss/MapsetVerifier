import {
  AppShell,
  Container, CSSVariablesResolver, Group,
  MantineProvider, ScrollArea,
  Text
} from '@mantine/core';
import {Route, Routes} from "react-router-dom";
import Home from "./pages/Home.tsx";
import Documentation from "./components/documentation/Documentation.tsx";
import '@mantine/core/styles.css';
import './styles/App.scss';
import {useDisclosure} from "@mantine/hooks";
import NavBars from "./components/navbar/NavBars.tsx";
import {theme} from "./theme/Theme.ts";
import Checks from "./components/checks/Checks.tsx";

export const cssVarResolver: CSSVariablesResolver = () => ({
  variables: {},
  light: {},
  dark: {
    // Default dark mode makes the text color use --mantine-color-dark-0 which we don't want
    '--mantine-color-text': '#fff',
    '--mantine-color-dimmed': '#9e9e9e'
  },
})

function App() {
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  
  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <AppShell
        // Restore combined header height (WindowBar 32 + nav 60 = 92)
        header={{ height: 92 }}
        navbar={{
          width: "256",
          breakpoint: 'xs',
          collapsed: { desktop: !desktopOpened },
        }}
      >
        <NavBars
          desktopOpened={desktopOpened}
          toggleDesktop={toggleDesktop}
        />
        <AppShell.Main>
          <Container p="sm" fluid>
            <Routes>
              <Route>
                <Route path="/" element={<Home />} />
                <Route path="/documentation" element={<Documentation />} />
                <Route path="/checks" element={<Checks />} />
                <Route path="/checks/:folder" element={<Checks />} />
                <Route path="*" element={<Text>TODO : NOT FOUND</Text>} />
              </Route>
            </Routes>
          </Container>
        </AppShell.Main>
      </AppShell>
    </MantineProvider>
  );
}

export default App;
