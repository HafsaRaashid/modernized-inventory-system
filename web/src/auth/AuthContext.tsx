import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

/**
 * sessionStorage keys backing auth state (BL-001 / FR-3, FR-7). Exported so
 * other modules that cannot reach this React context — e.g. api/client.ts's
 * getAuthHeader() — can read the same key directly instead of duplicating
 * the string.
 */
export const AUTH_TOKEN_STORAGE_KEY = "auth.token";
export const AUTH_USERNAME_STORAGE_KEY = "auth.username";

interface AuthContextValue {
  token: string | null;
  username: string | null;
  login(token: string, username: string): void;
  logout(): void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * Holds the signed-in session's token/username (BL-001, replacing the
 * legacy static-field identity carrier — SQ-004). Persists to
 * sessionStorage so the session survives a page reload but not a closed
 * tab, and initializes from sessionStorage on mount for the same reason.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() =>
    sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY),
  );
  const [username, setUsername] = useState<string | null>(() =>
    sessionStorage.getItem(AUTH_USERNAME_STORAGE_KEY),
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      username,
      login(newToken: string, newUsername: string) {
        sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, newToken);
        sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, newUsername);
        setToken(newToken);
        setUsername(newUsername);
      },
      logout() {
        sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
        sessionStorage.removeItem(AUTH_USERNAME_STORAGE_KEY);
        setToken(null);
        setUsername(null);
      },
    }),
    [token, username],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
