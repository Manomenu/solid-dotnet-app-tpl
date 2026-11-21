import { dataOf, refusalReader } from "@maniumek/suwfrontkit";

import { api } from "../api/client";
import type { components } from "../api/openapi";

export type Note = components["schemas"]["Note"];

export const { codeOf } = refusalReader<components["schemas"]["NoteRefusal"]>();

export function fetchNotes(): Promise<Note[]> {
    return dataOf(api.GET("/api/notes"));
}

export function createNote(text: string): Promise<Note> {
    return dataOf(api.POST("/api/notes", { body: { text } }));
}
