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
