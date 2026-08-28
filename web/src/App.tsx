import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { AppShell } from "./routes/AppShell";
import { Login } from "./routes/Login";
import { MainMenu } from "./routes/MainMenu";
import { NotFound } from "./routes/NotFound";

/**
 * Renders the Main Menu (inside AppShell) when authenticated, otherwise
 * redirects to /login (BL-001 FR-7 / AC-7; BL-002 FR-2).
 */
function RequireAuth() {
  const { token } = useAuth();
  if (!token) {
    return <Navigate to="/login" replace />;
  }
  return (
    <AppShell>
      <MainMenu />
    </AppShell>
  );
}

/**
 * The routing shell only (frontend-routing pillar). No capability route
 * lives here — each future backlog item adds its own route(s) beneath
 * AppShell once it is built.
 */
export default function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route path="/" element={<RequireAuth />} />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </ErrorBoundary>
  );
}
