import { apiFetch } from "./client";

export interface HealthResponse {
  status: string;
}

/**
 * Calls the foundation's one health-check endpoint (GET /api/health).
 * Exists to prove frontend-to-API connectivity end to end; not a backlog
 * capability.
 */
export function getHealth(): Promise<HealthResponse> {
  return apiFetch<HealthResponse>("/health", { method: "GET" });
}
