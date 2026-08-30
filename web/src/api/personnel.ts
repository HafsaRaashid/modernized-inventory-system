import { apiFetch } from "./client";

export interface Personnel {
  id: number;
  firstName: string;
  lastName: string;
}

/**
 * Calls GET /api/personnel to list the personnel a room can be assigned
 * to. On failure, apiFetch rejects with an ApiError — the caller decides
 * how to surface that.
 */
export function listPersonnel(): Promise<Personnel[]> {
  return apiFetch<Personnel[]>("/personnel");
}
