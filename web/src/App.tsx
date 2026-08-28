import { Route, Routes } from "react-router-dom";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { AppShell } from "./routes/AppShell";
import { NotFound } from "./routes/NotFound";

/**
 * The routing shell only (frontend-routing pillar). No capability route
 * lives here — each future backlog item adds its own route(s) beneath
 * AppShell once it is built.
 */
export default function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route path="/" element={<AppShell />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </ErrorBoundary>
  );
}
