import { render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import App from "../src/App";
import { getSession } from "../src/api/auth";
import { listDepartments } from "../src/api/departments";
import { listRooms, deleteRoom } from "../src/api/rooms";
import { listPersonnel } from "../src/api/personnel";
import { createRoomAssignment } from "../src/api/roomAssignments";
import {
  AUTH_TOKEN_STORAGE_KEY,
  AUTH_USERNAME_STORAGE_KEY,
  AuthProvider,
} from "../src/auth/AuthContext";

vi.mock("../src/api/auth", () => ({
  getSession: vi.fn(),
}));

vi.mock("../src/api/departments", () => ({
  listDepartments: vi.fn(),
}));

vi.mock("../src/api/rooms", () => ({
  listRooms: vi.fn(),
  updateRoom: vi.fn(),
  deleteRoom: vi.fn(),
}));

vi.mock("../src/api/personnel", () => ({
  listPersonnel: vi.fn(),
}));

vi.mock("../src/api/roomAssignments", () => ({
  createRoomAssignment: vi.fn(),
}));

/**
 * Proves the frontend test runner (test-frontend pillar) executes end to
 * end against this foundation, and that the "/" route's auth gate (BL-001
 * FR-7 / AC-7) behaves correctly for both the unauthenticated and
 * authenticated cases.
 */
describe("App shell", () => {
  beforeEach(() => {
    // Sensible default so pre-existing "/" tests (which never touch /admin
    // or getSession) keep working unaffected by the mock.
    vi.mocked(getSession).mockReset();
    vi.mocked(getSession).mockResolvedValue({ username: "user", isAdmin: false });
    vi.mocked(listDepartments).mockReset();
    vi.mocked(listDepartments).mockResolvedValue([]);
    vi.mocked(listRooms).mockReset();
    vi.mocked(listRooms).mockResolvedValue([]);
    vi.mocked(deleteRoom).mockReset();
    vi.mocked(listPersonnel).mockReset();
    vi.mocked(listPersonnel).mockResolvedValue([]);
    vi.mocked(createRoomAssignment).mockReset();
  });

  afterEach(() => {
    sessionStorage.clear();
    // The "/" -> "/login" redirect (AC-7) uses history.replaceState, which
    // otherwise leaks the resulting URL into the next test's render.
    window.history.pushState({}, "", "/");
  });

  it("renders without crashing and shows Login when unauthenticated", () => {
    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("redirects an unauthenticated visit to / to the Login screen (AC-7)", () => {
    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
    expect(screen.queryByText("Inventory Tracking System")).not.toBeInTheDocument();
  });

  it("shows the app shell for an already-authenticated session", () => {
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument();
    expect(screen.getByText("Signed in as testuser")).toBeInTheDocument();
  });

  it("AC-4: an unauthenticated visit to /admin shows the Login screen", () => {
    window.history.pushState({}, "", "/admin");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("AC-5: an authenticated non-admin visiting /admin ends up back at the Main Menu", async () => {
    window.history.pushState({}, "", "/admin");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "testuser", isAdmin: false });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    await waitFor(() =>
      expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument(),
    );
    expect(
      screen.queryByRole("button", { name: "Stok Ekle" }),
    ).not.toBeInTheDocument();
  });

  it("AC-6: an authenticated admin visiting /admin sees the Admin Panel", async () => {
    window.history.pushState({}, "", "/admin");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "adminuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "adminuser", isAdmin: true });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(
      await screen.findByRole("button", { name: "Stok Ekle" }),
    ).toBeInTheDocument();
  });

  it("FR-5: renders nothing while the admin check is pending", () => {
    window.history.pushState({}, "", "/admin");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockReturnValue(new Promise(() => {}));

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).not.toBeInTheDocument();
    expect(screen.queryByText("Inventory Tracking System")).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Stok Ekle" }),
    ).not.toBeInTheDocument();
  });

  it("an unauthenticated visit to /room-add shows the Login screen", () => {
    window.history.pushState({}, "", "/room-add");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("an authenticated non-admin visiting /room-add ends up back at the Main Menu", async () => {
    window.history.pushState({}, "", "/room-add");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "testuser", isAdmin: false });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    await waitFor(() =>
      expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument(),
    );
    expect(
      screen.queryByRole("button", { name: "EKLE" }),
    ).not.toBeInTheDocument();
  });

  it("an authenticated admin visiting /room-add sees the Room Add screen", async () => {
    window.history.pushState({}, "", "/room-add");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "adminuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "adminuser", isAdmin: true });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(
      await screen.findByRole("button", { name: "EKLE" }),
    ).toBeInTheDocument();
  });

  it("an unauthenticated visit to /room-update shows the Login screen", () => {
    window.history.pushState({}, "", "/room-update");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("an authenticated non-admin visiting /room-update ends up back at the Main Menu", async () => {
    window.history.pushState({}, "", "/room-update");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "testuser", isAdmin: false });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    await waitFor(() =>
      expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument(),
    );
    expect(
      screen.queryByRole("button", { name: "GÜNCELLE" }),
    ).not.toBeInTheDocument();
  });

  it("an authenticated admin visiting /room-update sees the Room Update screen", async () => {
    window.history.pushState({}, "", "/room-update");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "adminuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "adminuser", isAdmin: true });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(
      await screen.findByRole("button", { name: "GÜNCELLE" }),
    ).toBeInTheDocument();
  });

  it("an unauthenticated visit to /room-delete shows the Login screen", () => {
    window.history.pushState({}, "", "/room-delete");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("an authenticated non-admin visiting /room-delete ends up back at the Main Menu", async () => {
    window.history.pushState({}, "", "/room-delete");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "testuser", isAdmin: false });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    await waitFor(() =>
      expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument(),
    );
    expect(
      screen.queryByRole("button", { name: "SİL" }),
    ).not.toBeInTheDocument();
  });

  it("an authenticated admin visiting /room-delete sees the Room Delete screen", async () => {
    window.history.pushState({}, "", "/room-delete");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "adminuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "adminuser", isAdmin: true });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(
      await screen.findByRole("button", { name: "SİL" }),
    ).toBeInTheDocument();
  });

  it("AC-8: an unauthenticated visit to /room-assignment shows the Login screen", () => {
    window.history.pushState({}, "", "/room-assignment");

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(document.getElementById("login-form")).toBeInTheDocument();
  });

  it("AC-9: an authenticated non-admin visiting /room-assignment sees the Room Assignment screen", async () => {
    window.history.pushState({}, "", "/room-assignment");
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, "test-token");
    sessionStorage.setItem(AUTH_USERNAME_STORAGE_KEY, "testuser");
    vi.mocked(getSession).mockResolvedValueOnce({ username: "testuser", isAdmin: false });

    render(
      <AuthProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </AuthProvider>,
    );

    expect(
      await screen.findByRole("button", { name: "KAYDET" }),
    ).toBeInTheDocument();
  });
});
