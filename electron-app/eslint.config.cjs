const {
    defineConfig,
    globalIgnores,
} = require("eslint/config");

const globals = require("globals");
const tsParser = require("@typescript-eslint/parser");

const {
    fixupPluginRules,
    fixupConfigRules,
} = require("@eslint/compat");

const js = require("@eslint/js");

const {
    FlatCompat,
} = require("@eslint/eslintrc");

const compat = new FlatCompat({
    baseDirectory: __dirname,
    recommendedConfig: js.configs.recommended,
    allConfig: js.configs.all
});
const path = require("path");

module.exports = defineConfig([{
    languageOptions: {
        globals: {
            ...globals.browser,
            ...globals.node,
        },

        parser: tsParser,
        ecmaVersion: "latest",
        sourceType: "module",

        parserOptions: {
            ecmaFeatures: {
                jsx: true,
            },

            project: [path.join(__dirname, "tsconfig.json")],
        },
    },

    settings: {
        react: {
            version: "detect",
        },
    },

    extends: fixupConfigRules(compat.extends(
        "eslint:recommended",
        "plugin:@typescript-eslint/recommended",
        "plugin:react/recommended",
        "plugin:react-hooks/recommended",
        "plugin:import/recommended",
        "plugin:import/typescript",
        "prettier",
    )),

    rules: {
        "@typescript-eslint/explicit-function-return-type": "off",

        "@typescript-eslint/no-unused-vars": ["warn", {
            argsIgnorePattern: "^_",
            varsIgnorePattern: "^_",
        }],

        "@typescript-eslint/no-explicit-any": "off",
        "react/react-in-jsx-scope": "off",
        "react/jsx-uses-react": "off",
        "react-hooks/exhaustive-deps": "off",
        "react-hooks/set-state-in-effect": "error",
        "react-hooks/refs": "error",
        "react-hooks/preserve-manual-memoization": "error",
        // TanStack Virtual's useVirtualizer() is flagged unconditionally by this rule
        // (see VirtualizedList.tsx) and can't be refactored around; kept at warn.
        "react-hooks/incompatible-library": "warn",

        "import/order": ["warn", {
            groups: ["builtin", "external", "internal", ["parent", "sibling", "index"], "type"],
            "newlines-between": "never",

            alphabetize: {
                order: "asc",
                caseInsensitive: true,
            },
        }],

        "import/no-unresolved": ["error", {
            ignore: ["\\.md\\?raw$"],
        }],

        "no-console": ["warn", {
            allow: ["warn", "error"],
        }],

        "prefer-const": "warn",
    },
}, {
    files: ["**/*.cjs"],

    languageOptions: {
        parserOptions: {
            project: null,
        },
    },

    rules: {
        "@typescript-eslint/no-require-imports": "off",
    },
}, {
    files: ["**/*.ts", "**/*.tsx"],

    rules: {
        "no-undef": "off",
    },
}, globalIgnores(["**/dist", "**/node_modules"])]);
