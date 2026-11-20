import "./index.css";

import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { App } from "./App";

const root = document.getElementById("root");
if (!root) throw new Error("index.html has no #root element");

function logTo(kind: string) {
    return (error: unknown, info: { componentStack?: string | undefined }) => {
        console.error(kind, error, info.componentStack);
    };
}

createRoot(root, {
    onCaughtError: logTo("A screen failed and shows its error:"),
    onUncaughtError: logTo("The app failed outside every error boundary:"),
}).render(
    <StrictMode>
        <App />
    </StrictMode>,
);
