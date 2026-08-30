import { apiFetch } from "./client";

export interface Room {
  id: number;
  name: string;
  departmentId: number;
}

/**
 * Calls POST /api/rooms to create a new room under the given department.
 * On success, resolves with the created room. On failure (e.g. a
 * duplicate room name), apiFetch rejects with an ApiError — the caller
 * shows the appropriate failure message.
 */
export function createRoom(name: string, departmentId: number): Promise<Room> {
  return apiFetch<Room>("/rooms", {
    method: "POST",
    body: { name, departmentId },
  });
}

/**
 * Calls GET /api/rooms to list every room. Used to populate the Room
 * Update screen's existing-room selector.
 */
export function listRooms(): Promise<Room[]> {
  return apiFetch<Room[]>("/rooms");
}

/**
 * Calls PUT /api/rooms to rename a room, matching it by its current name
 * (oldName) rather than its ID. On success, resolves with the updated
 * room. On failure (e.g. the room isn't found, or newName collides with
 * an existing room), apiFetch rejects with an ApiError — the caller shows
 * the appropriate failure message.
 */
export function updateRoom(oldName: string, newName: string): Promise<Room> {
  return apiFetch<Room>("/rooms", {
    method: "PUT",
    body: { oldName, newName },
  });
}

/**
 * Calls DELETE /api/rooms to remove a room, matching it by name. On
 * success, resolves with the deleted room. On failure (e.g. the room
 * isn't found), apiFetch rejects with an ApiError — the caller shows the
 * appropriate failure message.
 */
export function deleteRoom(name: string): Promise<Room> {
  return apiFetch<Room>("/rooms", {
    method: "DELETE",
    body: { name },
  });
}
