import { ApiError } from "@maniumek/suwfrontkit";
import { describe, expect, it } from "vitest";

import { EMPTY_NOTE_FORM, MAX_NOTE_LENGTH, problemWith, submitNote } from "./notes";

describe("problemWith", () => {
    it("accepts a note of ordinary length", () => {
        expect(problemWith("buy milk")).toBeNull();
    });

    it("refuses an empty or blank note", () => {
        expect(problemWith("")).not.toBeNull();
        expect(problemWith("   ")).not.toBeNull();
    });

    it("refuses a note longer than the limit, and accepts one exactly at it", () => {
        expect(problemWith("x".repeat(MAX_NOTE_LENGTH))).toBeNull();
        expect(problemWith("x".repeat(MAX_NOTE_LENGTH + 1))).not.toBeNull();
    });
});

function recordingSave() {
    const saved: string[] = [];
    return {
        saved,
        save: (text: string) => {
            saved.push(text);
            return Promise.resolve();
        },
    };
}

describe("submitNote", () => {
    it("saves a valid note and empties the form", async () => {
        const { saved, save } = recordingSave();

        const form = await submitNote("buy milk", save);

        expect(saved).toEqual(["buy milk"]);
        expect(form).toEqual(EMPTY_NOTE_FORM);
    });

    it("keeps a blank note in the form with the mistake, and never sends it", async () => {
        const { saved, save } = recordingSave();

        const form = await submitNote("   ", save);

        expect(saved).toEqual([]);
        expect(form).toEqual({ text: "   ", mistake: "Write something first.", failure: null });
    });

    it("keeps the typed text and the server's sentence when the save fails", async () => {
        const form = await submitNote("buy milk", () => Promise.reject(new ApiError(503, { detail: "The database is away." })));

        expect(form).toEqual({ text: "buy milk", mistake: null, failure: "The database is away." });
    });

    it("puts the server's refusal of the text under the field, keeping the text", async () => {
        const refusal = new ApiError(422, { detail: "Send a non-empty text.", code: "note.textInvalid" });

        const form = await submitNote("buy milk", () => Promise.reject(refusal));

        expect(form).toEqual({ text: "buy milk", mistake: "Send a non-empty text.", failure: null });
    });
});
