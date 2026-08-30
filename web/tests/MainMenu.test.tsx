import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MainMenu } from "../src/routes/MainMenu";
import { getSession } from "../src/api/auth";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock("../src/api/auth", () => ({
  getSession: vi.fn(),
}));

function renderMainMenu() {
  return render(
    <MemoryRouter>
      <MainMenu />
    </MemoryRouter>,
  );
}

describe("MainMenu", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(getSession).mockReset();
    vi.mocked(getSession).mockResolvedValue({ username: "user", isAdmin: false });
  });

  it("AC-2: renders all five button labels", () => {
    renderMainMenu();

    expect(screen.getByRole("button", { name: "ARAMALAR" })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "ODA DEMİRBAŞ İŞLEMLERİ" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ODA TANIMLAMA" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "ADMİN" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Rapor Çıktısı Al" })).toBeInTheDocument();
  });

  it("AC-3: clicking ARAMALAR navigates to /search", () => {
    renderMainMenu();
    fireEvent.click(screen.getByRole("button", { name: "ARAMALAR" }));
    expect(mockNavigate).toHaveBeenCalledWith("/search");
  });

  it("AC-3: clicking ODA DEMİRBAŞ İŞLEMLERİ navigates to /asset-assignment", () => {
    renderMainMenu();
    fireEvent.click(screen.getByRole("button", { name: "ODA DEMİRBAŞ İŞLEMLERİ" }));
    expect(mockNavigate).toHaveBeenCalledWith("/asset-assignment");
  });

  it("AC-3: clicking ODA TANIMLAMA navigates to /room-assignment", () => {
    renderMainMenu();
    fireEvent.click(screen.getByRole("button", { name: "ODA TANIMLAMA" }));
    expect(mockNavigate).toHaveBeenCalledWith("/room-assignment");
  });

  it("AC-3: clicking Rapor Çıktısı Al navigates to /reports", () => {
    renderMainMenu();
    fireEvent.click(screen.getByRole("button", { name: "Rapor Çıktısı Al" }));
    expect(mockNavigate).toHaveBeenCalledWith("/reports");
  });

  it("AC-2: the ADMİN button starts disabled before getSession() resolves", () => {
    let resolveSession: (value: { username: string; isAdmin: boolean }) => void = () => {};
    vi.mocked(getSession).mockReturnValue(
      new Promise((resolve) => {
        resolveSession = resolve;
      }),
    );

    renderMainMenu();

    expect(screen.getByRole("button", { name: "ADMİN" })).toBeDisabled();

    // Avoid an unresolved-promise/act warning leaking into other tests.
    resolveSession({ username: "user", isAdmin: false });
  });

  it("AC-1: the ADMİN button becomes enabled after getSession() resolves with isAdmin: true", async () => {
    vi.mocked(getSession).mockResolvedValueOnce({ username: "admin", isAdmin: true });

    renderMainMenu();

    const adminButton = await screen.findByRole("button", { name: "ADMİN" });
    await waitFor(() => expect(adminButton).toBeEnabled());
  });

  it("AC-2: the ADMİN button stays disabled after getSession() resolves with isAdmin: false", async () => {
    vi.mocked(getSession).mockResolvedValueOnce({ username: "user", isAdmin: false });

    renderMainMenu();

    const adminButton = screen.getByRole("button", { name: "ADMİN" });
    await waitFor(() => expect(getSession).toHaveBeenCalled());
    expect(adminButton).toBeDisabled();
  });

  it("AC-2: the ADMİN button stays disabled if getSession() rejects", async () => {
    vi.mocked(getSession).mockRejectedValueOnce(new Error("unauthorized"));

    renderMainMenu();

    const adminButton = screen.getByRole("button", { name: "ADMİN" });
    await waitFor(() => expect(getSession).toHaveBeenCalled());
    expect(adminButton).toBeDisabled();
  });

  it("AC-1: clicking the enabled ADMİN button navigates to /admin", async () => {
    vi.mocked(getSession).mockResolvedValueOnce({ username: "admin", isAdmin: true });

    renderMainMenu();

    const adminButton = await screen.findByRole("button", { name: "ADMİN" });
    await waitFor(() => expect(adminButton).toBeEnabled());

    fireEvent.click(adminButton);
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });
});
