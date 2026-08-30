import { apiFetch } from "./client";

export interface RoomAssignment {
  id: number;
  roomId: number;
  personnelId: number;
}

/**
 * Calls POST /api/room-assignments to assign a room to a member of
 * personnel. On success, resolves with the created assignment. On failure
 * (e.g. a missing selection or an invalid reference), apiFetch rejects
 * with an ApiError — the caller shows the appropriate failure message.
 */
export function createRoomAssignment(roomId: number, personnelId: number): Promise<RoomAssignment> {
  return apiFetch<RoomAssignment>("/room-assignments", {
    method: "POST",
    body: { roomId, personnelId },
  });
}
