const BASE = import.meta.env.VITE_API_BASE ?? "/api";

export async function toError(response: Response): Promise<Error> {
    const body = (await response.json().catch(() => null)) as { detail?: unknown } | null;
    const detail = typeof body?.detail === "string" ? body.detail : null;
    return new Error(detail ?? `${response.status} ${response.statusText}`);
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const response = await fetch(`${BASE}${path}`, {
        ...init,
        headers: init.body === undefined ? {} : { "content-type": "application/json" },
    });
    if (!response.ok) throw await toError(response);
    return (response.status === 204 ? undefined : await response.json()) as T;
}
