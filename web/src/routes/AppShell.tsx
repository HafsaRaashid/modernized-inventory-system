import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

/**
 * The application shell (frontend-shell pillar): the root layout every
 * screen-bearing backlog item will eventually render inside. It owns no
 * capability of its own — just the page frame (header + content region)
 * and the global theme applied in styles/theme.css.
 */
export function AppShell() {
  const { username, logout } = useAuth();
  const navigate = useNavigate();

  function handleSignOut() {
    logout();
    navigate("/login");
  }

  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <h1>Inventory Tracking System</h1>
      </header>
      <main className="app-shell__content">
        <p>Signed in as {username}</p>
        <button type="button" onClick={handleSignOut}>
          Sign Out
        </button>
      </main>
    </div>
  );
}
