#!/usr/bin/env bash
set -euo pipefail
usage() {
    cat <<'USAGE'
usage: just tmux-dev start [--restart] | just tmux-dev stop
The local stack in one tmux session:
  left:  a shell in the repository
  right: the server under dotnet watch (+ the database of compose.yaml) / the web app under pnpm dev, with hot reload
The session is reused when it exists; --restart kills it (and everything running in it) first; stop kills it and stops there.
In a tmux pane the client is switched, not nested; any other terminal attaches where it was called.
TMUX_DEV_NO_ATTACH=1 builds the session and stops there (used to inspect the layout).
USAGE
}
if [ "${1:-}" = "--help" ]; then
    usage
    exit 0
fi

SESSION=myapp-dev
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

refuse() {
    usage >&2
    exit 2
}

restart=0
stop=0
case "${1:-} ${2:-}" in
    "start ") ;;
    "start --restart") restart=1 ;;
    "stop ") stop=1 ;;
    *) refuse ;;
esac

if ! command -v tmux >/dev/null 2>&1; then
    echo "tmux is not installed. Install it: sudo dnf install tmux" >&2
    exit 1
fi

this_terminal_is_a_tmux_pane() {
    [[ -n "${TMUX:-}" ]] || return 1
    local panes here
    panes="$(tmux list-panes -a -F '#{pane_tty}')"
    here="$(tty)" || return 1
    [[ $'\n'"$panes"$'\n' == *$'\n'"$here"$'\n'* ]]
}

attach() {
    if [[ "${TMUX_DEV_NO_ATTACH:-}" == "1" ]]; then
        return
    fi
    if this_terminal_is_a_tmux_pane; then
        tmux switch-client -t "$SESSION"
    else
        env -u TMUX tmux attach-session -t "$SESSION"
    fi
}

inside_session() {
    this_terminal_is_a_tmux_pane && [[ "$(tmux display-message -p '#S')" == "$SESSION" ]]
}

running() {
    tmux has-session -t "=$SESSION" 2>/dev/null
}

if ((stop)); then
    if ! running; then
        echo "No $SESSION session is running."
        exit 0
    fi
    inside_session || echo "Stopping the $SESSION session."
    tmux kill-session -t "=$SESSION"
    exit 0
fi

if ((restart)) && running; then
    if inside_session; then
        echo "Run --restart from outside the $SESSION session: killing it would end this shell before the new one starts." >&2
        exit 1
    fi
    tmux kill-session -t "=$SESSION"
fi

if running; then
    attach
    exit 0
fi

type_into_pane_shell() { tmux send-keys -t "$1" "$2" Enter; }

shell=$(tmux new-session -d -s "$SESSION" -c "$REPO" -x 220 -y 50 -P -F '#{pane_id}')
server=$(tmux split-window -h -t "$shell" -c "$REPO" -P -F '#{pane_id}')
web=$(tmux split-window -v -t "$server" -c "$REPO" -P -F '#{pane_id}')

tmux set-option -t "$SESSION" pane-border-status top
tmux select-pane -t "$shell" -T "shell"
tmux select-pane -t "$server" -T "server: (6300)"
tmux select-pane -t "$web" -T "web: (3300)"

type_into_pane_shell "$server" "./scripts/.internal/db.sh up && ./scripts/.internal/server.sh"
type_into_pane_shell "$web" "cd Myapp.Web && { [ -d node_modules ] || pnpm install; } && pnpm dev"

tmux select-pane -t "$shell"
attach
