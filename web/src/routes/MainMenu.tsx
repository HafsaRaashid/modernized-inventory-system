import { useNavigate } from "react-router-dom";
import "./MainMenu.css";

/**
 * Main Menu screen (SCR-002, BL-002 T2). Reproduces the legacy layout: a
 * 2x2 grid of navigation buttons (ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA
 * TANIMLAMA, ADMİN) followed by one full-width reporting button.
 *
 * ARAMALAR, ODA DEMİRBAŞ İŞLEMLERİ, ODA TANIMLAMA, and Rapor Çıktısı Al
 * navigate to their respective routes (FR-3); none of those destination
 * screens exist yet, so they currently fall through to NotFound. ADMİN
 * renders in its legacy default-disabled state (FR-4) — the gate logic
 * that conditionally enables it is BL-003's scope, not this item's.
 */
export function MainMenu() {
  const navigate = useNavigate();

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
        <button type="button" className="main-menu__button" disabled>
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
