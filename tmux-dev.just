# `just tmux-dev <recipe>` — the local stack in one tmux session: a shell, the server under dotnet watch with its database, and the web app under pnpm dev.

set shell := ["bash", "-euo", "pipefail", "-c"]

[private]
default:
    @just --list tmux-dev --unsorted

# Open the session, or attach to the one running (--restart: kill the old session and everything in it first)
start *args:
    @./scripts/.internal/tmux-dev.sh start {{ args }}

# Kill the session and everything running in it (also from inside it: your shell there ends with it)
stop:
    @./scripts/.internal/tmux-dev.sh stop
