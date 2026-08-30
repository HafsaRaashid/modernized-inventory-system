import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { StockUpdate } from "../src/routes/StockUpdate";
import { listFixedAssets, updateFixedAsset } from "../src/api/fixedAssets";
import { listAssetTypes } from "../src/api/assetTypes";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock("../src/api/fixedAssets", () => ({
  listFixedAssets: vi.fn(),
  updateFixedAsset: vi.fn(),
}));

vi.mock("../src/api/assetTypes", () => ({
  listAssetTypes: vi.fn(),
}));

function renderStockUpdate() {
  return render(
    <MemoryRouter>
      <StockUpdate />
    </MemoryRouter>,
  );
}

const ASSET_TYPES = [
  { id: 1, name: "Bilgisayar" },
  { id: 2, name: "Yazıcı" },
];

const ASSETS = [
  {
    id: 10,
    name: "Laptop",
    price: 1500,
    purchaseDate: "2026-01-15",
    assetTypeId: 2,
    quantity: 3,
  },
  {
    id: 11,
    name: "Monitör",
    price: 800,
    purchaseDate: "2025-06-01",
    assetTypeId: 1,
    quantity: 5,
  },
];

describe("StockUpdate", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listFixedAssets).mockReset();
    vi.mocked(updateFixedAsset).mockReset();
    vi.mocked(listAssetTypes).mockReset();
    vi.mocked(listFixedAssets).mockResolvedValue([]);
    vi.mocked(listAssetTypes).mockResolvedValue([]);
  });

  it("AC-1: renders the asset selector, asset fields, and GÜNCELLE button", async () => {
    renderStockUpdate();

    expect(screen.getByLabelText("Demirbaş")).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş Adı")).toBeInTheDocument();
    expect(screen.getByLabelText("Fiyat")).toBeInTheDocument();
    expect(screen.getByLabelText("Alım Tarihi")).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş Türü")).toBeInTheDocument();
    const assetTypeIdInput = screen.getByLabelText("Demirbaş Türü ID");
    expect(assetTypeIdInput).toBeInTheDocument();
    expect(assetTypeIdInput).toBeDisabled();
    expect(screen.getByLabelText("Miktar")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "GÜNCELLE" })).toBeInTheDocument();

    await waitFor(() => expect(listFixedAssets).toHaveBeenCalled());
  });

  it("AC-2: selecting an asset populates all fields with its current values", async () => {
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);

    renderStockUpdate();

    const select = await screen.findByLabelText("Demirbaş");
    fireEvent.change(select, { target: { value: "10" } });

    expect(screen.getByLabelText("Demirbaş Adı")).toHaveValue("Laptop");
    expect(screen.getByLabelText("Fiyat")).toHaveValue("1500");
    expect(screen.getByLabelText("Alım Tarihi")).toHaveValue("2026-01-15");
    expect(screen.getByLabelText("Demirbaş Türü ID")).toHaveValue("2");
    expect(screen.getByLabelText("Demirbaş Türü")).toHaveValue("2");
    expect(screen.getByLabelText("Miktar")).toHaveValue("3");
  });

  it("AC-3: submitting edited fields calls updateFixedAsset with the right args, shows success, and resets the form", async () => {
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);
    vi.mocked(updateFixedAsset).mockResolvedValue({
      id: 10,
      name: "Laptop Pro",
      price: 2000,
      purchaseDate: "2026-01-15",
      assetTypeId: 2,
      quantity: 4,
    });

    renderStockUpdate();

    const select = await screen.findByLabelText("Demirbaş");
    fireEvent.change(select, { target: { value: "10" } });

    fireEvent.change(screen.getByLabelText("Demirbaş Adı"), {
      target: { value: "Laptop Pro" },
    });
    fireEvent.change(screen.getByLabelText("Fiyat"), {
      target: { value: "2000" },
    });
    fireEvent.change(screen.getByLabelText("Miktar"), {
      target: { value: "4" },
    });

    fireEvent.click(screen.getByRole("button", { name: "GÜNCELLE" }));

    await waitFor(() =>
      expect(updateFixedAsset).toHaveBeenCalledWith(
        10,
        "Laptop Pro",
        2000,
        "2026-01-15",
        2,
        4,
      ),
    );

    expect(
      await screen.findByText("Demirbaş başarıyla güncellendi."),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş")).toHaveValue("");
    expect(screen.getByLabelText("Demirbaş Adı")).toHaveValue("");
    expect(screen.getByLabelText("Fiyat")).toHaveValue("");
    expect(screen.getByLabelText("Alım Tarihi")).toHaveValue("");
    expect(screen.getByLabelText("Demirbaş Türü ID")).toHaveValue("");
    expect(screen.getByLabelText("Miktar")).toHaveValue("");
    expect(listFixedAssets).toHaveBeenCalledTimes(2);
  });

  it("AC-4: no asset selected keeps GÜNCELLE disabled and never calls updateFixedAsset", async () => {
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderStockUpdate();
    await waitFor(() => expect(listFixedAssets).toHaveBeenCalled());

    const button = screen.getByRole("button", { name: "GÜNCELLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(updateFixedAsset).not.toHaveBeenCalled();
  });

  it("AC-4: an asset selected but the name cleared keeps GÜNCELLE disabled and never calls updateFixedAsset", async () => {
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderStockUpdate();

    const select = await screen.findByLabelText("Demirbaş");
    fireEvent.change(select, { target: { value: "10" } });
    fireEvent.change(screen.getByLabelText("Demirbaş Adı"), {
      target: { value: "" },
    });

    const button = screen.getByRole("button", { name: "GÜNCELLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(updateFixedAsset).not.toHaveBeenCalled();
  });

  it("AC-5: the asset-name field blocks digit keys but allows letter keys", async () => {
    renderStockUpdate();

    const nameInput = await screen.findByLabelText("Demirbaş Adı");

    expect(fireEvent.keyDown(nameInput, { key: "5" })).toBe(false);
    expect(fireEvent.keyDown(nameInput, { key: "a" })).toBe(true);
  });

  it("AC-14: clicking the back button navigates to /admin", async () => {
    renderStockUpdate();
    await waitFor(() => expect(listFixedAssets).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });

  it("shows the generic failure message when updateFixedAsset rejects for any reason", async () => {
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);
    vi.mocked(updateFixedAsset).mockRejectedValue(new Error("boom"));

    renderStockUpdate();

    const select = await screen.findByLabelText("Demirbaş");
    fireEvent.change(select, { target: { value: "11" } });

    fireEvent.click(screen.getByRole("button", { name: "GÜNCELLE" }));

    expect(
      await screen.findByText("Güncellenirken hata oluştu..."),
    ).toBeInTheDocument();
  });
});
