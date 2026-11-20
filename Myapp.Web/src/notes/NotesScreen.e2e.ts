import { expect, test } from "@playwright/test";

test("a note added on the Notes screen appears in the list", async ({ page }) => {
    const text = `note ${Date.now()}`;
    await page.goto("/");

    await page.getByLabel("New note").fill(text);
    await page.getByRole("button", { name: "Add note", exact: true }).click();

    await expect(page.getByText(text, { exact: true })).toBeVisible();
});

test("a blank note is refused with a sentence under the field", async ({ page }) => {
    await page.goto("/");

    await page.getByRole("button", { name: "Add note", exact: true }).click();

    await expect(page.getByLabel("New note")).toHaveAccessibleDescription("Write something first.");
});
