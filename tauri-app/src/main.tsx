import React from "react";
import ReactDOM from "react-dom/client";
import {BrowserRouter} from "react-router-dom";
import App from "./App";
import { SettingsProvider } from "./context/SettingsContext";

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
    <BrowserRouter>
        <SettingsProvider>
            <React.StrictMode>
                <App />
            </React.StrictMode>
        </SettingsProvider>
    </BrowserRouter>,
);
