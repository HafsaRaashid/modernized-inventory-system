import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { getSession } from "./api/auth";
import { useAuth } from "./auth/AuthContext";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { AdminPanel } from "./routes/AdminPanel";
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
 * Renders the Admin Panel when authenticated and admin, otherwise redirects:
 * to /login if unauthenticated, to / if authenticated but not an admin
 * (BL-004 FR-4/FR-5). Extends BL-003's admin gate to the /admin route
 * itself, since the URL bar is a second entry point the legacy app never
 * had. Renders nothing while the admin check is pending, to avoid flashing
 * either the Admin Panel or a premature redirect.
 */
function RequireAdmin() {
  const { token } = useAuth();
  const [status, setStatus] = useState<"loading" | "admin" | "not-admin">(
    "loading",
  );

  useEffect(() => {
    if (!token) {
      return;
    }
    let cancelled = false;
    getSession()
      .then((session) => {
        if (!cancelled) {
          setStatus(session.isAdmin ? "admin" : "not-admin");
        }
      })
      .catch(() => {
        if (!cancelled) {
          setStatus("not-admin");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  if (!token) {
    return <Navigate to="/login" replace />;
  }
  if (status === "loading") {
    return null;
  }
  if (status === "not-admin") {
    return <Navigate to="/" replace />;
  }
  return (
    <AppShell>
      <AdminPanel />
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
        <Route path="/admin" element={<RequireAdmin />} />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </ErrorBoundary>
  );
}
