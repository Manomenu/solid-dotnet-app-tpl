# Characterization tests

A characterization test records how the code behaves today, without judging whether that is
right, and from then on compares: a response, the rows a query returns, a rendered document,
saved as the expected value. It is the safety net before changing code nobody fully understands:
record first, change second, and a recording that no longer matches says exactly what changed.

This folder is empty in the template. In a project that takes over legacy code, put the
recordings here, under the feature's name. An agent may create a recording (record first,
change second); it may not change one: the PreToolUse hook refuses an edit of a file that
already exists here, so a failing recording is fixed in the code, never in the recording.
When the behaviour really is meant to change, the owner re-records in a session started
with `ALLOW_GATE_EDIT=1 claude`.
