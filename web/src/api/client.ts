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
 * - a slot for an Authorization header, currently a no-op
 *
 * Auth boundary (SQ-004): no token exists yet. A future backlog item is
 * what will start populating getAuthHeader() with a real token — nothing
 * in this file authenticates anyone today.
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
  return {};
}
