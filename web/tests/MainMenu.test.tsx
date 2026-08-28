import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MainMenu } from "../src/routes/MainMenu";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

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

  it("AC-4: the ADMİN button is disabled and clicking it does not navigate", () => {
    renderMainMenu();
    const adminButton = screen.getByRole("button", { name: "ADMİN" });

    expect(adminButton).toBeDisabled();

    fireEvent.click(adminButton);
    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
