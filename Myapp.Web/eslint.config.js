import js from "@eslint/js";
import { defineConfig } from "eslint/config";
import boundaries from "eslint-plugin-boundaries";
import reactHooks from "eslint-plugin-react-hooks";
import tseslint from "typescript-eslint";

const TOOL_DIRECTIVES = [/^ @ts-expect-error \S/, /^ eslint-disable(-next-line)? [\w@/-]+/, /^ @vitest-environment [\w-]+$/];
const CRITERION_MARKER = /^ AC-[a-z0-9]+(-[a-z0-9]+)*-[0-9]+$/;
const BROWSER_STORAGE = ["localStorage", "sessionStorage", "indexedDB"];
const BROWSER_STORAGE_FIX =
    "Keep the state on the server (an action) or in the address; browser storage is allowed only in one named file, added here as the exception when the owner asks for it.";

export const noComments = {
    meta: {
        type: "problem",
        schema: [],
        messages: {
            comment:
                'Delete the comment; rename or split the code so it explains itself; put lasting knowledge in a test, the spec or AGENTS.md (the only comments allowed are the tool directives listed in AGENTS.md, "No comments").',
        },
    },
    create(context) {
        const readsCriterionMarkers = context.filename.endsWith("eslint.config.js");
        return {
            Program() {
                for (const comment of context.sourceCode.getAllComments()) {
                    const allowed =
                        comment.type === "Shebang" ||
                        TOOL_DIRECTIVES.some((directive) => directive.test(comment.value)) ||
                        (readsCriterionMarkers && CRITERION_MARKER.test(comment.value));
                    if (!allowed) {
                        context.report({ loc: comment.loc, messageId: "comment" });
                    }
                }
            },
        };
    },
};

export default defineConfig(
    { ignores: ["dist", "src/api/openapi.d.ts"] },
    js.configs.recommended,
    tseslint.configs.strictTypeChecked,
    tseslint.configs.stylisticTypeChecked,
    reactHooks.configs.flat["recommended-latest"],
    {
        languageOptions: {
            parserOptions: { project: ["./tsconfig.app.json", "./tsconfig.node.json"], tsconfigRootDir: import.meta.dirname },
        },
        rules: {
            "@typescript-eslint/restrict-template-expressions": ["error", { allowNumber: true }],
            "@typescript-eslint/no-confusing-void-expression": ["error", { ignoreArrowShorthand: true }],
            "@typescript-eslint/no-unused-vars": ["error", { ignoreRestSiblings: true }],
        },
    },
    {
        files: ["src/**/*.{ts,tsx}"],
        ignores: ["src/**/*.e2e.ts"],
        plugins: { boundaries },
        settings: {
            "import/resolver": { typescript: { project: "./tsconfig.app.json" }, node: { extensions: [".ts", ".tsx"] } },
            "boundaries/elements": [
                { type: "api", pattern: "src/api" },
                { type: "design", pattern: "src/design" },
                { type: "feature", pattern: "src/*", capture: ["name"] },
            ],
            "boundaries/files": [{ category: "entry", pattern: "src/*.{ts,tsx}" }],
        },
        rules: {
            "boundaries/dependencies": [
                "error",
                {
                    default: "disallow",
                    policies: [
                        { allow: { to: { module: { origin: "external" } } } },
                        { allow: { dependency: { relationship: { to: "internal" } } } },
                        {
                            from: { file: { categories: "entry" } },
                            allow: {
                                to: [{ file: { categories: "entry" } }, { element: { types: { anyOf: ["feature", "api", "design"] } } }],
                            },
                        },
                        { from: { element: { type: "feature" } }, allow: { to: { element: { types: { anyOf: ["api", "design"] } } } } },
                        {
                            from: { element: { type: "feature" } },
                            allow: { to: { element: { type: "feature", captured: { name: "{{from.element.captured.name}}" } } } },
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ["src/**/*.{ts,tsx}"],
        rules: {
            "no-restricted-globals": ["error", ...BROWSER_STORAGE.map((name) => ({ name, message: BROWSER_STORAGE_FIX }))],
            "no-restricted-properties": [
                "error",
                ...BROWSER_STORAGE.map((property) => ({ object: "window", property, message: BROWSER_STORAGE_FIX })),
            ],
        },
    },
    {
        files: ["**/*.{ts,tsx,js,mjs,cjs}"],
        plugins: { local: { rules: { "no-comments": noComments } } },
        rules: { "local/no-comments": "error" },
    },
    {
        files: ["eslint.config.js", "eslint.config.test.js"],
        extends: [tseslint.configs.disableTypeChecked],
    },
);
