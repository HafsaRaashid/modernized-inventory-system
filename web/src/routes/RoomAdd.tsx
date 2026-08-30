import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./RoomAdd.css";
import { ApiError } from "../api/client";
import { listDepartments, type Department } from "../api/departments";
import { createRoom } from "../api/rooms";

const SUCCESS_MESSAGE = "Oda başarıyla eklendi.";
const DUPLICATE_MESSAGE = "Kayıtlı Oda...";
const GENERIC_FAILURE_MESSAGE = "Oda eklenirken bir hata oluştu.";

/**
 * Room Add screen (SCR-010, "ODA EKLEME"). A bordered section holding a
 * room-name input, a department picker (a single <select> stands in for
 * the legacy paired ID/name list — picking a name option inherently picks
 * its ID), a disabled input echoing the selected department's numeric ID
 * (FR-2), and a centered "EKLE" submit button. A back link returns to
 * /admin, mirroring AdminPanel's useNavigate() pattern.
 *
 * Submission is blocked client-side until both the room name (after trim)
 * and a department selection are present. On success, the form resets and
 * shows the legacy success message; on a duplicate-name conflict (409),
 * shows the legacy "Kayıtlı Oda..." message; any other failure shows a
 * generic message without crashing the UI.
 */
export function RoomAdd() {
  const navigate = useNavigate();

  const [departments, setDepartments] = useState<Department[]>([]);
  const [roomName, setRoomName] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  useEffect(() => {
    let cancelled = false;
    listDepartments()
      .then((result) => {
        if (!cancelled) {
          setDepartments(result);
        }
      })
      .catch(() => {
        // Leave the picker empty; the form simply can't be submitted.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const canSubmit = roomName.trim() !== "" && departmentId !== "";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSubmit) {
      return;
    }

    try {
      await createRoom(roomName.trim(), Number(departmentId));
      setRoomName("");
      setDepartmentId("");
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setMessage({ text: DUPLICATE_MESSAGE, kind: "error" });
      } else {
        setMessage({ text: GENERIC_FAILURE_MESSAGE, kind: "error" });
      }
    }
  }

  return (
    <div className="room-add">
      <form className="room-add__section" onSubmit={handleSubmit}>
        <h2 className="room-add__title">ODA EKLEME</h2>
        <button
          type="button"
          className="room-add__back"
          onClick={() => navigate("/admin")}
        >
          Geri
        </button>
        <div className="room-add__field">
          <label htmlFor="room-name">Oda Adı</label>
          <input
            id="room-name"
            name="room-name"
            type="text"
            className="room-add__input"
            value={roomName}
            onChange={(event) => setRoomName(event.target.value)}
          />
        </div>
        <div className="room-add__field">
          <label htmlFor="department">Departman</label>
          <select
            id="department"
            name="department"
            className="room-add__select"
            value={departmentId}
            onChange={(event) => setDepartmentId(event.target.value)}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {departments.map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </select>
        </div>
        <div className="room-add__field">
          <label htmlFor="department-id">Departman ID</label>
          <input
            id="department-id"
            name="department-id"
            type="text"
            className="room-add__input"
            value={departmentId}
            disabled
            readOnly
          />
        </div>
        {message && (
          <span
            className={`room-add__message room-add__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button type="submit" className="room-add__button" disabled={!canSubmit}>
          EKLE
        </button>
      </form>
    </div>
  );
}
