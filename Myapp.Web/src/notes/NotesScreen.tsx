import { Alert, Button, Field, Input, Page, Spinner } from "@maniumek/suwfrontkit";
import { startTransition, Suspense, use, useActionState, useState } from "react";

import { createNote, fetchNotes, type Note } from "./api";
import { EMPTY_NOTE_FORM, MAX_NOTE_LENGTH, type NoteForm, submitNote } from "./notes";

function NoteList({ notes }: { notes: Promise<Note[]> }) {
    const list = use(notes);
    if (list.length === 0) return <p className="text-muted-foreground">No notes yet.</p>;
    return (
        <ul className="list-disc pl-6">
            {list.map((note) => (
                <li key={note.id}>{note.text}</li>
            ))}
        </ul>
    );
}

export function NotesScreen() {
    const [notes, setNotes] = useState(fetchNotes);
    const [form, add, adding] = useActionState(async (_previous: NoteForm, data: FormData) => {
        const text = data.get("text");
        return submitNote(typeof text === "string" ? text : "", async (valid) => {
            await createNote(valid);
            startTransition(() => {
                setNotes(fetchNotes());
            });
        });
    }, EMPTY_NOTE_FORM);

    return (
        <Page title="Notes" width="reading">
            <form action={add} className="flex flex-col gap-4">
                <Field
                    prefix="notes"
                    name="new-note"
                    label="New note"
                    hint={`At most ${MAX_NOTE_LENGTH} characters.`}
                    errors={form.mistake === null ? [] : [form.mistake]}
                >
                    {(field) => <Input {...field} name="text" defaultValue={form.text} />}
                </Field>
                <Button type="submit" className="self-start" disabled={adding}>
                    Add note
                </Button>
                {form.failure !== null && <Alert variant="destructive">{form.failure}</Alert>}
            </form>
            <Suspense fallback={<Spinner label="Loading notes…" size="sm" />}>
                <NoteList notes={notes} />
            </Suspense>
        </Page>
    );
}
