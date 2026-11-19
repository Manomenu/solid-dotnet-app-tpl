export const MAX_NOTE_LENGTH = 500;

export function problemWith(text: string): string | null {
    if (text.trim().length === 0) return "Write something first.";
    if (text.length > MAX_NOTE_LENGTH) return `A note has at most ${MAX_NOTE_LENGTH} characters.`;
    return null;
}
