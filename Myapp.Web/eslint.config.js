import { app } from "@maniumek/suwgate/eslint";
import { defineConfig } from "eslint/config";
import reactHooks from "eslint-plugin-react-hooks";

export default defineConfig(
    app({
        root: import.meta.dirname,
        projects: ["./tsconfig.json", "./tsconfig.node.json"],
        ignores: ["src/api/openapi.d.ts"],
        untyped: ["prettier.config.js"],
        features: ["notes"],
        shared: ["api"],
    }),
    reactHooks.configs.flat["recommended-latest"],
);
