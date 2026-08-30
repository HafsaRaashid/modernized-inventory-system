import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./RoomDelete.css";
import { deleteRoom, listRooms, type Room } from "../api/rooms";

const SUCCESS_MESSAGE = "Oda başarıyla silindi.";
const FAILURE_MESSAGE = "Hatalı İşlem...";

/**
 * Room Delete screen (SCR-011, "ODA SİLME"). A bordered section holding a
 * labeled room selector and a "SİL" button in a single row, and a back
 * link returning to /admin, mirroring RoomUpdate's useNavigate() pattern.
 *
 * There is deliberately NO confirmation dialog before deleting — this is a
 * faithful reproduction of the legacy screen's own lack of confirmation.
 * "SİL" is disabled until a room is selected. On success, the selection is
 * cleared and the selector is re-populated (matching the legacy "combo
 * cleared and re-populated" behavior).
 */
export function RoomDelete() {
  const navigate = useNavigate();

  const [rooms, setRooms] = useState<Room[]>([]);
  const [selectedRoomName, setSelectedRoomName] = useState("");
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  function loadRooms() {
    listRooms()
      .then(setRooms)
      .catch(() => {
        // Leave the selector empty; the button simply can't be used.
      });
  }

  useEffect(() => {
    loadRooms();
  }, []);

  const canDelete = selectedRoomName !== "";

  async function handleDelete() {
    if (!canDelete) {
      return;
    }

    try {
      await deleteRoom(selectedRoomName);
      setSelectedRoomName("");
      loadRooms();
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch {
      setMessage({ text: FAILURE_MESSAGE, kind: "error" });
    }
  }

  return (
    <div className="room-delete">
      <div className="room-delete__section">
        <h2 className="room-delete__title">ODA SİLME</h2>
        <button
          type="button"
          className="room-delete__back"
          onClick={() => navigate("/admin")}
        >
          Geri
        </button>
        <div className="room-delete__row">
          <label htmlFor="room-select">Oda</label>
          <select
            id="room-select"
            name="room-select"
            className="room-delete__select"
            value={selectedRoomName}
            onChange={(event) => setSelectedRoomName(event.target.value)}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {rooms.map((room) => (
              <option key={room.id} value={room.name}>
                {room.name}
              </option>
            ))}
          </select>
          <button
            type="button"
            className="room-delete__button"
            disabled={!canDelete}
            onClick={handleDelete}
          >
            SİL
          </button>
        </div>
        {message && (
          <span
            className={`room-delete__message room-delete__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
      </div>
    </div>
  );
}
