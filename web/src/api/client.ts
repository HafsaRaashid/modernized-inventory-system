import { AUTH_TOKEN_STORAGE_KEY } from "../auth/AuthContext";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly body?: unknown,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
}

/**
 * The single typed entry point every future API call goes through
 * (api-client pillar). Owns:
 * - base-URL resolution (VITE_API_BASE_URL, see .env.example)
 * - JSON request/response shaping
 * - a stable error type (ApiError) callers can catch by type
 * - a slot for an Authorization header, sourced from AuthContext's
 *   sessionStorage-backed token (BL-001)
 *
 * Auth boundary (SQ-004): getAuthHeader() reads the token AuthContext
 * persisted to sessionStorage under AUTH_TOKEN_STORAGE_KEY, since this
 * module has no React context access.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...rest } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeader(),
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    let parsedBody: unknown;
    try {
      parsedBody = await response.json();
    } catch {
      parsedBody = undefined;
    }
    throw new ApiError(`Request to ${path} failed with ${response.status}`, response.status, parsedBody);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  return token ? { Authorization: `Bearer ${token}` } : {};
}
