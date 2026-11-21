import { codeOf } from "./api";

export const MAX_NOTE_LENGTH = 500;

export function problemWith(text: string): string | null {
    if (text.trim().length === 0) return "Write something first.";
    if (text.length > MAX_NOTE_LENGTH) return `A note has at most ${MAX_NOTE_LENGTH} characters.`;
    return null;
}

export interface NoteForm {
    text: string;
    mistake: string | null;
    failure: string | null;
}

export const EMPTY_NOTE_FORM: NoteForm = { text: "", mistake: null, failure: null };

function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
}

export async function submitNote(text: string, save: (text: string) => Promise<unknown>): Promise<NoteForm> {
    const mistake = problemWith(text);
    if (mistake !== null) return { text, mistake, failure: null };
    try {
        await save(text);
        return EMPTY_NOTE_FORM;
    } catch (error) {
        if (codeOf(error) === "note.textInvalid") return { text, mistake: messageOf(error), failure: null };
        return { text, mistake: null, failure: messageOf(error) };
    }
}
