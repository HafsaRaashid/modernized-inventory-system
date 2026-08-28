import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getSession } from "../api/auth";
import "./MainMenu.css";

/**
 * Main Menu screen (SCR-002, BL-002 T2). Reproduces the legacy layout: a
 * 2x2 grid of navigation buttons (ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA
 * TANIMLAMA, ADMİN) followed by one full-width reporting button.
 *
 * ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA TANIMLAMA, and Rapor Çıktısı Al
 * navigate to their respective routes (FR-3); none of those destination
 * screens exist yet, so they currently fall through to NotFound. ADMİN
 * starts disabled and only becomes enabled once a fresh server-side
 * authorization check (GET /auth/me) resolves with isAdmin true, matching
 * legacy ANA_MENU_Load's re-evaluation on every load (BL-003 FR-3). A
 * rejected check leaves it at its safe disabled default.
 */
export function MainMenu() {
  const navigate = useNavigate();
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    let cancelled = false;
    getSession()
      .then((session) => {
        if (!cancelled) {
          setIsAdmin(session.isAdmin);
        }
      })
      .catch(() => {
        // Safe default (disabled) is already in place; no error UI needed.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="main-menu">
      <div className="main-menu__grid">
        <button
          type="button"
          className="main-menu__button"
          onClick={() => navigate("/search")}
        >
          ARAMALAR
        </button>
        <button
          type="button"
          className="main-menu__button"
          onClick={() => navigate("/asset-assignment")}
        >
          ODA DEMİRBAŞ İŞLEMLERİ
        </button>
        <button
          type="button"
          className="main-menu__button"
          onClick={() => navigate("/room-assignment")}
        >
          ODA TANIMLAMA
        </button>
        <button
          type="button"
          className="main-menu__button"
          disabled={!isAdmin}
        >
          ADMİN
        </button>
      </div>
      <button
        type="button"
        className="main-menu__button main-menu__button--wide"
        onClick={() => navigate("/reports")}
      >
        Rapor Çıktısı Al
      </button>
    </div>
  );
}
