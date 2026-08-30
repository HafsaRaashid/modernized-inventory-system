import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./RoomUpdate.css";
import { listRooms, updateRoom, type Room } from "../api/rooms";

const SUCCESS_MESSAGE = "Oda başarıyla güncellendi.";
const FAILURE_MESSAGE = "Hatalı İşlem...";

/**
 * Room Update screen (SCR-012, "ODA GÜNCELLEME"). A bordered section
 * holding an existing-room selector (populated via listRooms()) and a
 * new-name text input, and a centered "GÜNCELLE" submit button. A back
 * link returns to /admin, mirroring RoomAdd's useNavigate() pattern.
 *
 * Submission is blocked client-side until both a room is selected and the
 * new name (after trim) is non-empty. On success, the form resets and the
 * selector is re-populated (matching the legacy "combo re-populated"
 * behavior). Unlike RoomAdd, both a not-found (404) and a duplicate-name
 * (409) failure map to the SAME generic message here — there is no
 * per-status branching.
 */
export function RoomUpdate() {
  const navigate = useNavigate();

  const [rooms, setRooms] = useState<Room[]>([]);
  const [selectedRoomName, setSelectedRoomName] = useState("");
  const [newName, setNewName] = useState("");
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  function loadRooms() {
    listRooms()
      .then(setRooms)
      .catch(() => {
        // Leave the selector empty; the form simply can't be submitted.
      });
  }

  useEffect(() => {
    loadRooms();
  }, []);

  const canSubmit = selectedRoomName !== "" && newName.trim() !== "";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSubmit) {
      return;
    }

    try {
      await updateRoom(selectedRoomName, newName.trim());
      setSelectedRoomName("");
      setNewName("");
      loadRooms();
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch {
      setMessage({ text: FAILURE_MESSAGE, kind: "error" });
    }
  }

  return (
    <div className="room-update">
      <form className="room-update__section" onSubmit={handleSubmit}>
        <h2 className="room-update__title">ODA GÜNCELLEME</h2>
        <button
          type="button"
          className="room-update__back"
          onClick={() => navigate("/admin")}
        >
          Geri
        </button>
        <div className="room-update__field">
          <label htmlFor="room-select">Oda</label>
          <select
            id="room-select"
            name="room-select"
            className="room-update__select"
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
        </div>
        <div className="room-update__field">
          <label htmlFor="new-name">Yeni Ad</label>
          <input
            id="new-name"
            name="new-name"
            type="text"
            className="room-update__input"
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
          />
        </div>
        {message && (
          <span
            className={`room-update__message room-update__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button type="submit" className="room-update__button" disabled={!canSubmit}>
          GÜNCELLE
        </button>
      </form>
    </div>
  );
}
