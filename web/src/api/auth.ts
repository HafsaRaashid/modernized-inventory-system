import { apiFetch } from "./client";

export interface LoginResponse {
  token: string;
  username: string;
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
