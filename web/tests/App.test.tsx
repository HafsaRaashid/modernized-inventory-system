import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import App from "../src/App";
import {
  AUTH_TOKEN_STORAGE_KEY,
  AUTH_USERNAME_STORAGE_KEY,
  AuthProvider,
} from "../src/auth/AuthContext";

/**
 * Proves the frontend test runner (test-frontend pillar) executes end to
 * end against this foundation, and that the "/" route's auth gate (BL-001
 * FR-7 / AC-7) behaves correctly for both the unauthenticated and
 * authenticated cases.
 */
describe("App shell", () => {
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
});
