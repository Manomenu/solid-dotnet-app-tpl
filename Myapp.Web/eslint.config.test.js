import { RuleTester } from "eslint";
import { describe, it } from "vitest";

import { noComments } from "./eslint.config.js";

RuleTester.describe = describe;
RuleTester.it = it;
RuleTester.itOnly = it.only;

const tester = new RuleTester();
const comment = [{ messageId: "comment" }];

tester.run("local/no-comments", noComments, {
    valid: [
        { code: "const a = 1;" },
        { code: "const url = 'http://x'; const b = '/* no */'; const c = `// no`;" },
        { code: "// @ts-expect-error the library types miss the overload\nf(1);" },
        { code: "// eslint-disable-next-line no-console\nconsole.log(1);" },
        { code: "/* eslint-disable no-console */\nconsole.log(1);" },
        { code: "// @vitest-environment node\nconst a = 1;" },
        { code: "#!/usr/bin/env node\nconst a = 1;" },
        { code: "export default [\n    // AC-" + "notes-1\n    { rules: {} },\n];", filename: "eslint.config.js" },
    ],
    invalid: [
        { code: "// the clock\nconst a = 1;", errors: comment },
        { code: "const a = 1; // one", errors: comment },
        { code: "/* a block */\nconst a = 1;", errors: comment },
        { code: "/** JSDoc of a */\nconst a = 1;", errors: comment },
        { code: "// const b = 2;\nconst a = 1;", errors: comment },
        { code: "// TODO: later\nconst a = 1;", errors: comment },
        { code: "// @ts-expect-error\nf(1);", errors: comment },
        { code: '/// <reference types="vite/client" />\nconst a = 1;', errors: comment },
        { code: "// AC-" + "notes-1\nconst a = 1;", filename: "src/notes/rules.ts", errors: comment },
        { code: "// AC-" + "notes-1 keeps it strict\nexport default [];", filename: "eslint.config.js", errors: comment },
    ],
});
