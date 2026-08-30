import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./RoomAssignment.css";
import { listRooms, type Room } from "../api/rooms";
import { listPersonnel, type Personnel } from "../api/personnel";
import { createRoomAssignment } from "../api/roomAssignments";

const SUCCESS_MESSAGE = "Atama başarıyla kaydedildi.";
const GENERIC_FAILURE_MESSAGE = "Atama kaydedilirken bir hata oluştu.";

/**
 * Room Assignment screen (SCR-006). Two side-by-side selectors (room,
 * personnel) populated on mount, each feeding a disabled echo input below
 * it (room name / personnel full name). A "KAYDET" button stays disabled
 * until both a room and a personnel are selected. A back link returns to
 * "/" (this screen is reached from the Main Menu, not the Admin Panel).
 *
 * On success, both selections and echo fields reset and the legacy
 * success message is shown; any failure shows a generic message without
 * crashing the UI (this endpoint has no documented distinct error cases
 * for the client to branch on).
 */
export function RoomAssignment() {
  const navigate = useNavigate();

  const [rooms, setRooms] = useState<Room[]>([]);
  const [personnel, setPersonnel] = useState<Personnel[]>([]);
  const [roomId, setRoomId] = useState("");
  const [personnelId, setPersonnelId] = useState("");
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
    listPersonnel()
      .then((result) => {
        if (!cancelled) {
          setPersonnel(result);
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
  const selectedPersonnel = personnel.find((person) => String(person.id) === personnelId);
  const canSubmit = roomId !== "" && personnelId !== "";

  async function handleSave() {
    if (!canSubmit) {
      return;
    }

    try {
      await createRoomAssignment(Number(roomId), Number(personnelId));
      setRoomId("");
      setPersonnelId("");
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch {
      setMessage({ text: GENERIC_FAILURE_MESSAGE, kind: "error" });
    }
  }

  return (
    <div className="room-assignment">
      <div className="room-assignment__section">
        <h2 className="room-assignment__title">ODA ATAMA</h2>
        <button
          type="button"
          className="room-assignment__back"
          onClick={() => navigate("/")}
        >
          Geri
        </button>
        <div className="room-assignment__row">
          <div className="room-assignment__field">
            <label htmlFor="room">Oda</label>
            <select
              id="room"
              name="room"
              className="room-assignment__select"
              value={roomId}
              onChange={(event) => setRoomId(event.target.value)}
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
          <div className="room-assignment__field">
            <label htmlFor="personnel">Personel</label>
            <select
              id="personnel"
              name="personnel"
              className="room-assignment__select"
              value={personnelId}
              onChange={(event) => setPersonnelId(event.target.value)}
            >
              <option value="" disabled>
                Seçiniz
              </option>
              {personnel.map((person) => (
                <option key={person.id} value={person.id}>
                  {`${person.firstName} ${person.lastName}`}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="room-assignment__row">
          <div className="room-assignment__field">
            <label htmlFor="room-name">Oda Adı</label>
            <input
              id="room-name"
              name="room-name"
              type="text"
              className="room-assignment__input"
              value={selectedRoom?.name ?? ""}
              disabled
              readOnly
            />
          </div>
          <div className="room-assignment__field">
            <label htmlFor="personnel-name">Personel Adı</label>
            <input
              id="personnel-name"
              name="personnel-name"
              type="text"
              className="room-assignment__input"
              value={selectedPersonnel ? `${selectedPersonnel.firstName} ${selectedPersonnel.lastName}` : ""}
              disabled
              readOnly
            />
          </div>
        </div>
        {message && (
          <span
            className={`room-assignment__message room-assignment__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button
          type="button"
          className="room-assignment__button"
          disabled={!canSubmit}
          onClick={handleSave}
        >
          KAYDET
        </button>
      </div>
    </div>
  );
}
