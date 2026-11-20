set shell := ["bash", "-euo", "pipefail", "-c"]

[private]
default:
    @just --list tmux-dev --unsorted

# Open the session, or attach to the one running (--restart: kill the old session and everything in it first)
start *args:
    @Myapp.Web/node_modules/.bin/suwgate dev tmux start {{ args }}

# Kill the session and everything running in it (also from inside it: your shell there ends with it)
stop:
    @Myapp.Web/node_modules/.bin/suwgate dev tmux stop
