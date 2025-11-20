import {
  AppShell,
  Container,
  MantineProvider,
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

function App() {
  const [desktopOpened, { toggle: toggleDesktop }] = useDisclosure(true);
  
  return (
    <MantineProvider defaultColorScheme="dark" theme={theme}>
      <AppShell
        header={{ height: 60 }}
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
          <Container p="sm">
            <Routes>
              <Route>
                <Route path="/" element={<Home />} />
                <Route path="/documentation" element={<Documentation />} />
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
