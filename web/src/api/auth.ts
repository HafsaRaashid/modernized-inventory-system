import { apiFetch } from "./client";

export interface LoginResponse {
  token: string;
  username: string;
}

export interface SessionResponse {
  username: string;
  isAdmin: boolean;
}

/**
 * Calls POST /api/auth/login with the submitted credentials.
 * On success, resolves with the issued JWT and the authenticated username.
 * On failure (wrong password, unknown username, or empty field), apiFetch
 * rejects with an ApiError — the caller shows the login failure message.
 */
export function login(username: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/auth/login", {
    method: "POST",
    body: { username, password },
  });
}

/**
 * Calls GET /api/auth/me to re-evaluate the current session, matching
 * legacy ANA_MENU_Load's on-every-load authorization check (BL-003 FR-3).
 * On success, resolves with the authenticated username and whether they
 * hold admin rights (YetkiID). On failure (missing/invalid token), apiFetch
 * rejects with an ApiError — the caller keeps its safe (non-admin) default.
 */
export function getSession(): Promise<SessionResponse> {
  return apiFetch<SessionResponse>("/auth/me");
}
