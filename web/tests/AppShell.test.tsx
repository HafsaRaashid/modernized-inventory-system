import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AppShell } from "../src/routes/AppShell";
import {
  AUTH_TOKEN_STORAGE_KEY,
  AUTH_USERNAME_STORAGE_KEY,
  AuthProvider,
} from "../src/auth/AuthContext";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

function renderAppShell() {
  return render(
    <AuthProvider>
      <MemoryRouter>
        <AppShell>
          <div>test child content</div>
        </AppShell>
      </MemoryRouter>
    </AuthProvider>,
  );
}

/**
 * BL-002 FR-1: AppShell is now a generic wrapper — header chrome stays
 * persistent regardless of children, and Sign Out (BL-001) must keep
 * working unchanged after the refactor (AC-5 regression check).
 */
describe("AppShell", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it("AC-1: renders header chrome and arbitrary children", () => {
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");

    renderAppShell();

    expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument();
    expect(screen.getByText("Signed in as testuser")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign Out" })).toBeInTheDocument();
    expect(screen.getByText("test child content")).toBeInTheDocument();
  });

  it("AC-5: Sign Out logs out and navigates to /login (BL-001 regression)", () => {
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");

    renderAppShell();

    fireEvent.click(screen.getByRole("button", { name: "Sign Out" }));

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(sessionStorage.getItem(AUTH_USERNAME_STORAGE_KEY)).toBeNull();
    expect(mockNavigate).toHaveBeenCalledWith("/login");
  });
});
