import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Login } from "../src/routes/Login";
import { AuthProvider } from "../src/auth/AuthContext";
import { login } from "../src/api/auth";

const LOGIN_FAILURE_MESSAGE = "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!";

vi.mock("../src/api/auth", () => ({
  login: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * Login only reads useAuth() (AuthContext) and useNavigate() (react-router-dom),
 * so those are the only providers/mocks needed to render it in isolation.
 */
function renderLogin() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <Login />
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("Login", () => {
  beforeEach(() => {
    vi.mocked(login).mockReset();
    mockNavigate.mockReset();
    sessionStorage.clear();
  });

  it("AC-2: shows the failure message and resets both fields when login is rejected", async () => {
    vi.mocked(login).mockRejectedValueOnce(new Error("INVALID_LOGIN_CREDENTIALS"));
    renderLogin();

    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı") as HTMLInputElement;
    const passwordInput = screen.getByPlaceholderText("Şifre") as HTMLInputElement;

    fireEvent.change(usernameInput, { target: { value: "wronguser" } });
    fireEvent.change(passwordInput, { target: { value: "wrongpass" } });
    fireEvent.click(screen.getByRole("button", { name: "GİRİŞ" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(LOGIN_FAILURE_MESSAGE);
    expect(usernameInput.value).toBe("");
    expect(passwordInput.value).toBe("");
  });

  it("AC-4: blurring the empty username field shows its hint, and typing clears it", () => {
    renderLogin();
    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı");

    expect(screen.queryByText("*")).not.toBeInTheDocument();

    fireEvent.blur(usernameInput);
    expect(screen.getByText("*")).toBeInTheDocument();

    fireEvent.change(usernameInput, { target: { value: "someone" } });
    expect(screen.queryByText("*")).not.toBeInTheDocument();
  });

  it("AC-4: blurring the empty password field shows its own hint, and typing clears it", () => {
    renderLogin();
    const passwordInput = screen.getByPlaceholderText("Şifre");

    expect(screen.queryByText("*")).not.toBeInTheDocument();

    fireEvent.blur(passwordInput);
    expect(screen.getByText("*")).toBeInTheDocument();

    fireEvent.change(passwordInput, { target: { value: "secret" } });
    expect(screen.queryByText("*")).not.toBeInTheDocument();
  });

  it("AC-4: the hints never disable the submit button", () => {
    renderLogin();
    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı");
    const passwordInput = screen.getByPlaceholderText("Şifre");

    fireEvent.blur(usernameInput);
    fireEvent.blur(passwordInput);

    // Both hints are showing (empty fields), yet the button stays enabled -
    // FR-5: the cosmetic indicator never gates submission.
    expect(screen.getAllByText("*")).toHaveLength(2);
    const submitButton = screen.getByRole("button", { name: "GİRİŞ" });
    expect(submitButton).toBeEnabled();
  });

  it("AC-7: a successful login navigates to /", async () => {
    vi.mocked(login).mockResolvedValueOnce({ token: "signed-jwt", username: "alice" });
    renderLogin();

    fireEvent.change(screen.getByPlaceholderText("Kullanıcı Adı"), {
      target: { value: "alice" },
    });
    fireEvent.change(screen.getByPlaceholderText("Şifre"), {
      target: { value: "correct-password" },
    });
    fireEvent.click(screen.getByRole("button", { name: "GİRİŞ" }));

    await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/"));
  });
});
