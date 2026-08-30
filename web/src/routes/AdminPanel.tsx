import { useNavigate } from "react-router-dom";
import "./AdminPanel.css";

/**
 * Admin Panel screen (SCR-007, BL-004 FR-2/FR-3). Two large buttons side by
 * side in the upper portion (Stok Ekle, Stok Güncelle), followed by three
 * smaller buttons in a row beneath (Oda Sil, Oda Ekle, Oda Güncelle).
 *
 * None of the five destination screens exist yet, so each currently falls
 * through to the app's existing NotFound catch-all route (same pattern as
 * MainMenu's other unimplemented destinations).
 */
export function AdminPanel() {
  const navigate = useNavigate();

  return (
    <div className="admin-panel">
      <div className="admin-panel__large-grid">
        <button
          type="button"
          className="admin-panel__button admin-panel__button--large"
          onClick={() => navigate("/stock-add")}
        >
          Stok Ekle
        </button>
        <button
          type="button"
          className="admin-panel__button admin-panel__button--large"
          onClick={() => navigate("/stock-update")}
        >
          Stok Güncelle
        </button>
      </div>
      <div className="admin-panel__small-grid">
        <button
          type="button"
          className="admin-panel__button admin-panel__button--small"
          onClick={() => navigate("/room-delete")}
        >
          Oda Sil
        </button>
        <button
          type="button"
          className="admin-panel__button admin-panel__button--small"
          onClick={() => navigate("/room-add")}
        >
          Oda Ekle
        </button>
        <button
          type="button"
          className="admin-panel__button admin-panel__button--small"
          onClick={() => navigate("/room-update")}
        >
          Oda Güncelle
        </button>
      </div>
    </div>
  );
}
