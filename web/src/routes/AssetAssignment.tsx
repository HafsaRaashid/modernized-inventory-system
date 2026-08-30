import { useEffect, useState, type ChangeEvent, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./AssetAssignment.css";
import { ApiError } from "../api/client";
import { listRooms, type Room } from "../api/rooms";
import { listFixedAssets, type FixedAsset } from "../api/fixedAssets";
import {
  createAssetAssignment,
  listRoomAssetAssignments,
  type RoomAssetAssignmentRow,
} from "../api/assetAssignments";

const SUCCESS_MESSAGE = "Odaya Demirbaş Atandı";
const GENERIC_FAILURE_MESSAGE = "Demirbaş atanırken bir hata oluştu.";

const ALLOWED_CONTROL_KEYS = new Set([
  "Backspace",
  "Delete",
  "Tab",
  "Escape",
  "Enter",
  "ArrowLeft",
  "ArrowRight",
  "ArrowUp",
  "ArrowDown",
  "Home",
  "End",
]);

/**
 * Blocks any keypress that isn't a digit, a comma, a navigation/editing
 * key, or a modifier-combo (copy/paste/select-all/cut), used by the
 * Quantity field. Parsing is deferred to submit time.
 */
function handleDigitsAndCommaKeyDown(event: KeyboardEvent<HTMLInputElement>) {
  if (event.ctrlKey || event.metaKey || ALLOWED_CONTROL_KEYS.has(event.key)) {
    return;
  }
  if (!/^[0-9,]$/.test(event.key)) {
    event.preventDefault();
  }
}

/**
 * Asset Assignment screen (SCR — Demirbaş Atama). Two side-by-side
 * selectors (room, fixed asset) populated on mount, each feeding a
 * disabled echo input below it (room name / asset name), a quantity input
 * accepting only digits and a comma, and a "KAYDET" button that stays
 * disabled until a room and asset are selected, a quantity is entered, and
 * that quantity does not exceed the selected asset's currently-known
 * stock. A back link returns to "/" (this screen is reached from the Main
 * Menu, not the Admin Panel).
 *
 * Below the form, a read-only panel lists the selected room's current
 * asset assignments, re-fetched every time the room selection changes.
 *
 * Unlike every other Add/Update screen in this codebase, a successful save
 * clears ONLY the quantity field — the room and asset selections are left
 * as-is, since assigning several assets to the same room in a row is the
 * expected flow. The fixed-asset list and the current-assignments panel
 * are both re-fetched after a successful save so the stock-exceeds check
 * and the panel reflect the just-issued quantity.
 */
export function AssetAssignment() {
  const navigate = useNavigate();

  const [rooms, setRooms] = useState<Room[]>([]);
  const [assets, setAssets] = useState<FixedAsset[]>([]);
  const [roomId, setRoomId] = useState("");
  const [assetId, setAssetId] = useState("");
  const [quantity, setQuantity] = useState("");
  const [assignments, setAssignments] = useState<RoomAssetAssignmentRow[]>([]);
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  useEffect(() => {
    let cancelled = false;
    listRooms()
      .then((result) => {
        if (!cancelled) {
          setRooms(result);
        }
      })
      .catch(() => {
        // Leave the picker empty; the form simply can't be submitted.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    listFixedAssets()
      .then((result) => {
        if (!cancelled) {
          setAssets(result);
        }
      })
      .catch(() => {
        // Leave the picker empty; the form simply can't be submitted.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const selectedRoom = rooms.find((room) => String(room.id) === roomId);
  const selectedAsset = assets.find((asset) => String(asset.id) === assetId);
  const parsedQuantity = Number(quantity.replace(",", "."));
  const quantityExceedsStock =
    selectedAsset !== undefined && quantity !== "" && parsedQuantity > selectedAsset.quantity;
  const canSubmit =
    roomId !== "" && assetId !== "" && quantity !== "" && !quantityExceedsStock;

  function handleRoomChange(event: ChangeEvent<HTMLSelectElement>) {
    const newRoomId = event.target.value;
    setRoomId(newRoomId);

    if (newRoomId === "") {
      setAssignments([]);
      return;
    }

    listRoomAssetAssignments(Number(newRoomId))
      .then((result) => {
        setAssignments(result);
      })
      .catch(() => {
        setAssignments([]);
      });
  }

  async function handleSave() {
    if (!canSubmit) {
      return;
    }

    try {
      await createAssetAssignment(Number(roomId), Number(assetId), parsedQuantity);
      setQuantity("");
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });

      listFixedAssets()
        .then((result) => setAssets(result))
        .catch(() => {
          // Keep the previously-known asset list; the stock check simply
          // won't reflect this issuance until the next successful fetch.
        });
      listRoomAssetAssignments(Number(roomId))
        .then((result) => setAssignments(result))
        .catch(() => {
          // Keep the previous panel contents.
        });
    } catch (error) {
      if (error instanceof ApiError) {
        const body = error.body as { error?: string; message?: string } | undefined;
        if (body?.error === "INSUFFICIENT_STOCK") {
          setMessage({ text: body.message ?? GENERIC_FAILURE_MESSAGE, kind: "error" });
          return;
        }
      }
      setMessage({ text: GENERIC_FAILURE_MESSAGE, kind: "error" });
    }
  }

  return (
    <div className="asset-assignment">
      <div className="asset-assignment__section">
        <h2 className="asset-assignment__title">DEMİRBAŞ ATAMA</h2>
        <button
          type="button"
          className="asset-assignment__back"
          onClick={() => navigate("/")}
        >
          Geri
        </button>
        <div className="asset-assignment__row">
          <div className="asset-assignment__field">
            <label htmlFor="room">Oda</label>
            <select
              id="room"
              name="room"
              className="asset-assignment__select"
              value={roomId}
              onChange={handleRoomChange}
            >
              <option value="" disabled>
                Seçiniz
              </option>
              {rooms.map((room) => (
                <option key={room.id} value={room.id}>
                  {room.name}
                </option>
              ))}
            </select>
          </div>
          <div className="asset-assignment__field">
            <label htmlFor="asset">Demirbaş</label>
            <select
              id="asset"
              name="asset"
              className="asset-assignment__select"
              value={assetId}
              onChange={(event) => setAssetId(event.target.value)}
            >
              <option value="" disabled>
                Seçiniz
              </option>
              {assets.map((asset) => (
                <option key={asset.id} value={asset.id}>
                  {asset.name}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="asset-assignment__row">
          <div className="asset-assignment__field">
            <label htmlFor="room-name">Oda Adı</label>
            <input
              id="room-name"
              name="room-name"
              type="text"
              className="asset-assignment__input"
              value={selectedRoom?.name ?? ""}
              disabled
              readOnly
            />
          </div>
          <div className="asset-assignment__field">
            <label htmlFor="asset-name">Demirbaş Adı</label>
            <input
              id="asset-name"
              name="asset-name"
              type="text"
              className="asset-assignment__input"
              value={selectedAsset?.name ?? ""}
              disabled
              readOnly
            />
          </div>
        </div>
        <div className="asset-assignment__field">
          <label htmlFor="quantity">Miktar</label>
          <input
            id="quantity"
            name="quantity"
            type="text"
            className="asset-assignment__input"
            value={quantity}
            onKeyDown={handleDigitsAndCommaKeyDown}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        {message && (
          <span
            className={`asset-assignment__message asset-assignment__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button
          type="button"
          className="asset-assignment__button"
          disabled={!canSubmit}
          onClick={handleSave}
        >
          KAYDET
        </button>
        <div className="asset-assignment__panel">
          <h3 className="asset-assignment__panel-title">Odadaki Demirbaşlar</h3>
          <table className="asset-assignment__table">
            <thead>
              <tr>
                <th>Demirbaş</th>
                <th>Miktar</th>
              </tr>
            </thead>
            <tbody>
              {assignments.map((assignment) => (
                <tr key={assignment.id}>
                  <td>{assignment.assetName}</td>
                  <td>{assignment.quantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
