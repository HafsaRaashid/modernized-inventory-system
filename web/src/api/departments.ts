import { apiFetch } from "./client";

export interface Department {
  id: number;
  name: string;
}

/**
 * Calls GET /api/departments to list the departments a room can be
 * assigned to. On failure, apiFetch rejects with an ApiError — the caller
 * decides how to surface that.
 */
export function listDepartments(): Promise<Department[]> {
  return apiFetch<Department[]>("/departments");
}
