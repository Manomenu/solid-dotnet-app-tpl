import { Alert, Button, Field, Input, Page } from "@maniumek/suwfrontkit";
import { useEffect, useState } from "react";

import { createNote, fetchNotes, type Note } from "./api";
import { MAX_NOTE_LENGTH, problemWith } from "./notes";

function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
}

export function NotesScreen() {
    const [notes, setNotes] = useState<Note[]>([]);
    const [text, setText] = useState("");
    const [mistake, setMistake] = useState<string | null>(null);
    const [failure, setFailure] = useState<string | null>(null);

    useEffect(() => {
        const controller = new AbortController();
        fetchNotes(controller.signal)
            .then(setNotes)
            .catch((e: unknown) => {
                if (!controller.signal.aborted) setFailure(messageOf(e));
            });
        return () => {
            controller.abort();
        };
    }, []);

    function add() {
        const problem = problemWith(text);
        setMistake(problem);
        if (problem !== null) return;
        setFailure(null);
        createNote(text)
            .then((note) => {
                setNotes((current) => [note, ...current]);
                setText("");
            })
            .catch((e: unknown) => {
                setFailure(messageOf(e));
            });
    }

    return (
        <main className="p-8">
            <Page title="Notes" width="reading">
                <Field
                    prefix="notes"
                    name="new-note"
                    label="New note"
                    hint={`At most ${MAX_NOTE_LENGTH} characters.`}
                    errors={mistake === null ? [] : [mistake]}
                >
                    {(field) => (
                        <Input
                            {...field}
                            value={text}
                            onChange={(event) => {
                                setText(event.currentTarget.value);
                            }}
                        />
                    )}
                </Field>
                <Button className="self-start" onClick={add}>
                    Add note
                </Button>
                {failure !== null && <Alert variant="destructive">{failure}</Alert>}
                {notes.length === 0 ? (
                    <p className="text-muted-foreground">No notes yet.</p>
                ) : (
                    <ul className="list-disc pl-6">
                        {notes.map((note) => (
                            <li key={note.id}>{note.text}</li>
                        ))}
                    </ul>
                )}
            </Page>
        </main>
    );
}
