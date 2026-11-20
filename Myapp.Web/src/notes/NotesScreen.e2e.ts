import { expect, test } from "@playwright/test";

test("a note submitted with Enter appears in the list, the field empties, and the list shows it after a reload", async ({ page }) => {
    const text = `note ${Date.now()}`;
    await page.goto("/notes");

    await page.getByLabel("New note").fill(text);
    await page.getByLabel("New note").press("Enter");

    await expect(page.getByText(text, { exact: true })).toBeVisible();
    await expect(page.getByLabel("New note")).toHaveValue("");
    await page.reload();
    await expect(page.getByText(text, { exact: true })).toBeVisible();
});

test("a blank note is refused with a sentence under the field", async ({ page }) => {
    await page.goto("/notes");

    await page.getByRole("button", { name: "Add note", exact: true }).click();

    await expect(page.getByLabel("New note")).toHaveAccessibleDescription("Write something first.");
});

test("while a note is saving it cannot be sent again, and a refusal keeps the text with the server's sentence", async ({ page }) => {
    let answer: () => void = () => undefined;
    const answered = new Promise<void>((resolve) => {
        answer = resolve;
    });
    await page.route("**/api/notes", async (route) => {
        if (route.request().method() !== "POST") return route.fallback();
        await answered;
        return route.fulfill({
            status: 503,
            contentType: "application/problem+json",
            body: JSON.stringify({ detail: "The notes are read-only for a moment; try again." }),
        });
    });
    await page.goto("/notes");

    await page.getByLabel("New note").fill("buy milk");
    await page.getByRole("button", { name: "Add note", exact: true }).click();

    await expect(page.getByRole("button", { name: "Add note", exact: true })).toBeDisabled();
    answer();
    await expect(page.getByRole("alert")).toHaveText("The notes are read-only for a moment; try again.");
    await expect(page.getByLabel("New note")).toHaveValue("buy milk");
    await expect(page.getByRole("button", { name: "Add note", exact: true })).toBeEnabled();
});

test("a list that fails to load shows the error, and Try again loads it", async ({ page }) => {
    let failing = true;
    await page.route("**/api/notes", (route) => {
        if (!failing || route.request().method() !== "GET") return route.fallback();
        return route.fulfill({
            status: 502,
            contentType: "application/problem+json",
            body: JSON.stringify({ detail: "The server is away." }),
        });
    });
    await page.goto("/notes");

    await expect(page.getByRole("alert")).toContainText("The server is away.");
    failing = false;
    await page.getByRole("button", { name: "Try again", exact: true }).click();

    await expect(page.getByLabel("New note")).toBeVisible();
    await expect(page.getByRole("alert")).toHaveCount(0);
});
