import path from "node:path";

import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    resolve: { alias: { "@": path.resolve(import.meta.dirname, "./src") } },
    server: {
        port: 3300,
        strictPort: true,
        proxy: {
            "/api": {
                target: process.env["API_PROXY_TARGET"] ?? "http://127.0.0.1:6300",
                changeOrigin: true,
            },
        },
    },
});
