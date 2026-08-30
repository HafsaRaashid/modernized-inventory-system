import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminPanel } from "../src/routes/AdminPanel";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

function renderAdminPanel() {
  return render(
    <MemoryRouter>
      <AdminPanel />
    </MemoryRouter>,
  );
}

describe("AdminPanel", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
  });

  it("AC-2: renders all five button labels", () => {
    renderAdminPanel();

    expect(screen.getByRole("button", { name: "Stok Ekle" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Stok Güncelle" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Oda Sil" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Oda Ekle" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Oda Güncelle" })).toBeInTheDocument();
  });

  it("AC-3: clicking Stok Ekle navigates to /stock-add", () => {
    renderAdminPanel();
    fireEvent.click(screen.getByRole("button", { name: "Stok Ekle" }));
    expect(mockNavigate).toHaveBeenCalledWith("/stock-add");
  });

  it("AC-3: clicking Stok Güncelle navigates to /stock-update", () => {
    renderAdminPanel();
    fireEvent.click(screen.getByRole("button", { name: "Stok Güncelle" }));
    expect(mockNavigate).toHaveBeenCalledWith("/stock-update");
  });

  it("AC-3: clicking Oda Sil navigates to /room-delete", () => {
    renderAdminPanel();
    fireEvent.click(screen.getByRole("button", { name: "Oda Sil" }));
    expect(mockNavigate).toHaveBeenCalledWith("/room-delete");
  });

  it("AC-3: clicking Oda Ekle navigates to /room-add", () => {
    renderAdminPanel();
    fireEvent.click(screen.getByRole("button", { name: "Oda Ekle" }));
    expect(mockNavigate).toHaveBeenCalledWith("/room-add");
  });

  it("AC-3: clicking Oda Güncelle navigates to /room-update", () => {
    renderAdminPanel();
    fireEvent.click(screen.getByRole("button", { name: "Oda Güncelle" }));
    expect(mockNavigate).toHaveBeenCalledWith("/room-update");
  });
});
