# TODO

<!-- template:start -->
Decided for the template, not built yet; each arrives as its own change when it is needed.

- A CI step that publishes the images: the registry is project-specific (`.github/workflows/ci.yml` builds and smoke-tests them, nothing more).
<!-- template:end -->

## When the first fake of an outside service appears

The first fake in tests (WireMock for an external API, a mail catcher, e2e seed data) brings a
`test-environment/` folder with its own compose file, scripts and `.just/test-environment.just`;
`db.sh` moves there. Until then the gate needs only the one PostgreSQL container from `db.sh`.
