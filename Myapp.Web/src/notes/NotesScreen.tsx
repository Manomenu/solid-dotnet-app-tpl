import { Alert, Button, Group, List, Stack, Text, TextInput, Title } from "@mantine/core";
import { useEffect, useState } from "react";

import { createNote, fetchNotes, type Note } from "./api";
import { problemWith } from "./notes";

function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
}

/** The Notes screen: the list, and a field to add one. The template's example feature; delete it when it is in the way. */
export function NotesScreen() {
    const [notes, setNotes] = useState<Note[]>([]);
    const [text, setText] = useState("");
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        // StrictMode runs effects twice in development: abort the first request, ignore its result.
        const controller = new AbortController();
        fetchNotes(controller.signal)
            .then(setNotes)
            .catch((e: unknown) => {
                if (!controller.signal.aborted) setError(messageOf(e));
            });
        return () => {
            controller.abort();
        };
    }, []);

    function add() {
        const problem = problemWith(text);
        if (problem !== null) {
            setError(problem);
            return;
        }
        setError(null);
        createNote(text)
            .then((note) => {
                setNotes((current) => [note, ...current]);
                setText("");
            })
            .catch((e: unknown) => {
                setError(messageOf(e));
            });
    }

    return (
        <Stack maw={560} mx="auto" p="md">
            <Title order={2}>Notes</Title>
            <Group align="flex-end">
                <TextInput
                    label="New note"
                    value={text}
                    onChange={(event) => {
                        setText(event.currentTarget.value);
                    }}
                    style={{ flex: 1 }}
                />
                <Button onClick={add}>Add note</Button>
            </Group>
            {error !== null && (
                <Alert color="red" role="alert">
                    {error}
                </Alert>
            )}
            {notes.length === 0 ? (
                <Text c="dimmed">No notes yet.</Text>
            ) : (
                <List>
                    {notes.map((note) => (
                        <List.Item key={note.id}>{note.text}</List.Item>
                    ))}
                </List>
            )}
        </Stack>
    );
}
