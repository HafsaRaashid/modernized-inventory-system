import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { getSession } from "./api/auth";
import { useAuth } from "./auth/AuthContext";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { AdminPanel } from "./routes/AdminPanel";
import { AppShell } from "./routes/AppShell";
import { AssetAssignment } from "./routes/AssetAssignment";
import { Login } from "./routes/Login";
import { MainMenu } from "./routes/MainMenu";
import { NotFound } from "./routes/NotFound";
import { RoomAdd } from "./routes/RoomAdd";
import { RoomAssignment } from "./routes/RoomAssignment";
import { RoomDelete } from "./routes/RoomDelete";
import { RoomUpdate } from "./routes/RoomUpdate";
import { StockAdd } from "./routes/StockAdd";
import { StockUpdate } from "./routes/StockUpdate";

/**
 * Renders `children` (inside AppShell) when authenticated, otherwise
 * redirects to /login (BL-001 FR-7 / AC-7; BL-002 FR-2). Generalized from
 * hardcoding `<MainMenu />` so any route requiring only authentication
 * (not admin) — e.g. `/room-assignment` (BL-008) — can share this one
 * guard, the same refactor already applied to `RequireAdmin` (BL-005).
 */
function RequireAuth({ children }: { children: ReactNode }) {
  const { token } = useAuth();
  if (!token) {
    return <Navigate to="/login" replace />;
  }
  return <AppShell>{children}</AppShell>;
}

/**
 * Renders `children` when authenticated and admin, otherwise redirects:
 * to /login if unauthenticated, to / if authenticated but not an admin
 * (BL-004 FR-4/FR-5). Extends BL-003's admin gate to whichever admin-only
 * route it wraps, since the URL bar is a second entry point the legacy app
 * never had. Renders nothing while the admin check is pending, to avoid
 * flashing either the wrapped content or a premature redirect. Generalized
 * from hardcoding `<AdminPanel />` (BL-004) so admin-only routes beyond
 * `/admin` — e.g. `/room-add` (BL-005) — can share this one guard instead of
 * each duplicating its loading/redirect logic.
 */
function RequireAdmin({ children }: { children: ReactNode }) {
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
  return <AppShell>{children}</AppShell>;
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
        <Route
          path="/"
          element={
            <RequireAuth>
              <MainMenu />
            </RequireAuth>
          }
        />
        <Route
          path="/room-assignment"
          element={
            <RequireAuth>
              <RoomAssignment />
            </RequireAuth>
          }
        />
        <Route
          path="/asset-assignment"
          element={
            <RequireAuth>
              <AssetAssignment />
            </RequireAuth>
          }
        />
        <Route
          path="/admin"
          element={
            <RequireAdmin>
              <AdminPanel />
            </RequireAdmin>
          }
        />
        <Route
          path="/room-add"
          element={
            <RequireAdmin>
              <RoomAdd />
            </RequireAdmin>
          }
        />
        <Route
          path="/room-update"
          element={
            <RequireAdmin>
              <RoomUpdate />
            </RequireAdmin>
          }
        />
        <Route
          path="/room-delete"
          element={
            <RequireAdmin>
              <RoomDelete />
            </RequireAdmin>
          }
        />
        <Route
          path="/stock-add"
          element={
            <RequireAdmin>
              <StockAdd />
            </RequireAdmin>
          }
        />
        <Route
          path="/stock-update"
          element={
            <RequireAdmin>
              <StockUpdate />
            </RequireAdmin>
          }
        />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </ErrorBoundary>
  );
}
