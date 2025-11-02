import { Outlet, Route, Routes } from 'react-router-dom';
import Home from "./pages/Home";
import Beatmaps from "./components/beatmaps/Beatmaps.tsx";
import Documentation from "./components/documentation/Documentation.tsx";
import Menu from "./components/menu/Menu.tsx";

function AppRoutes() {
  const Layout = () => (
    <div className="screen-container">
      <div className="side panel">
        <Beatmaps />
      </div>
      <div className="main panel">
        <Menu />
        <Outlet />
      </div>
    </div>
  );
    
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Home />} />
        <Route path="/documentation" element={<Documentation />} />
        <Route path="*" element={<div>TODO : NOT FOUND</div>} />
      </Route>
    </Routes>
  )
}

export default AppRoutes;