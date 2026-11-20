import { ErrorBoundary } from "@maniumek/suwfrontkit";
import { Redirect, Route, Switch, useLocation } from "wouter";

import { NotesScreen } from "./notes/NotesScreen";

export function App() {
    const [pathname] = useLocation();

    return (
        <main className="p-8">
            <ErrorBoundary key={pathname}>
                <Switch>
                    <Route path="/notes" component={NotesScreen} />
                    <Route>
                        <Redirect to="/notes" replace />
                    </Route>
                </Switch>
            </ErrorBoundary>
        </main>
    );
}
