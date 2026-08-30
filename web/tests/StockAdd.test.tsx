import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { StockAdd } from "../src/routes/StockAdd";
import { ApiError } from "../src/api/client";
import { listAssetTypes } from "../src/api/assetTypes";
import { createFixedAsset } from "../src/api/fixedAssets";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock("../src/api/assetTypes", () => ({
  listAssetTypes: vi.fn(),
}));

vi.mock("../src/api/fixedAssets", () => ({
  createFixedAsset: vi.fn(),
}));

function renderStockAdd() {
  return render(
    <MemoryRouter>
      <StockAdd />
    </MemoryRouter>,
  );
}

const ASSET_TYPES = [
  { id: 1, name: "Bilgisayar" },
  { id: 2, name: "Yazıcı" },
];

describe("StockAdd", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listAssetTypes).mockReset();
    vi.mocked(createFixedAsset).mockReset();
    vi.mocked(listAssetTypes).mockResolvedValue([]);
  });

  it("AC-1: renders the asset-name, price, purchase-date, asset-type, disabled asset-type-id echo, quantity inputs, and EKLE button", async () => {
    renderStockAdd();

    expect(screen.getByLabelText("Demirbaş Adı")).toBeInTheDocument();
    expect(screen.getByLabelText("Fiyat")).toBeInTheDocument();
    expect(screen.getByLabelText("Alım Tarihi")).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş Türü")).toBeInTheDocument();
    const assetTypeIdInput = screen.getByLabelText("Demirbaş Türü ID");
    expect(assetTypeIdInput).toBeInTheDocument();
    expect(assetTypeIdInput).toBeDisabled();
    expect(screen.getByLabelText("Miktar")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "EKLE" })).toBeInTheDocument();

    await waitFor(() => expect(listAssetTypes).toHaveBeenCalled());
  });

  it("AC-2: selecting an asset type option updates the disabled echo input's value to that type's id", async () => {
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);

    renderStockAdd();

    const select = await screen.findByLabelText("Demirbaş Türü");
    fireEvent.change(select, { target: { value: "2" } });

    expect(screen.getByLabelText("Demirbaş Türü ID")).toHaveValue("2");
  });

  it("AC-3: submitting with all fields filled calls createFixedAsset with the right args and shows success, resetting fields", async () => {
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);
    vi.mocked(createFixedAsset).mockResolvedValue({
      id: 5,
      name: "Laptop",
      price: 1500,
      purchaseDate: "2026-01-15",
      assetTypeId: 2,
      quantity: 3,
    });

    renderStockAdd();

    const select = await screen.findByLabelText("Demirbaş Türü");
    fireEvent.change(select, { target: { value: "2" } });
    fireEvent.change(screen.getByLabelText("Demirbaş Adı"), {
      target: { value: "Laptop" },
    });
    fireEvent.change(screen.getByLabelText("Fiyat"), {
      target: { value: "1500" },
    });
    fireEvent.change(screen.getByLabelText("Alım Tarihi"), {
      target: { value: "2026-01-15" },
    });
    fireEvent.change(screen.getByLabelText("Miktar"), {
      target: { value: "3" },
    });

    fireEvent.click(screen.getByRole("button", { name: "EKLE" }));

    await waitFor(() =>
      expect(createFixedAsset).toHaveBeenCalledWith("Laptop", 1500, "2026-01-15", 2, 3),
    );

    expect(await screen.findByText("Demirbaş başarıyla eklendi.")).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş Adı")).toHaveValue("");
    expect(screen.getByLabelText("Fiyat")).toHaveValue("");
    expect(screen.getByLabelText("Alım Tarihi")).toHaveValue("");
    expect(screen.getByLabelText("Demirbaş Türü ID")).toHaveValue("");
    expect(screen.getByLabelText("Miktar")).toHaveValue("");
  });

  it("AC-4: leaving the asset name empty keeps EKLE disabled and never calls createFixedAsset", async () => {
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);

    renderStockAdd();

    const select = await screen.findByLabelText("Demirbaş Türü");
    fireEvent.change(select, { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Fiyat"), { target: { value: "100" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "1" } });

    const button = screen.getByRole("button", { name: "EKLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createFixedAsset).not.toHaveBeenCalled();
  });

  it("AC-4: leaving no asset type selected keeps EKLE disabled and never calls createFixedAsset", async () => {
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);

    renderStockAdd();
    await waitFor(() => expect(listAssetTypes).toHaveBeenCalled());

    fireEvent.change(screen.getByLabelText("Demirbaş Adı"), {
      target: { value: "Laptop" },
    });
    fireEvent.change(screen.getByLabelText("Fiyat"), { target: { value: "100" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "1" } });

    const button = screen.getByRole("button", { name: "EKLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createFixedAsset).not.toHaveBeenCalled();
  });

  it("shows the duplicate-asset message when createFixedAsset rejects with a 409 ApiError", async () => {
    vi.mocked(listAssetTypes).mockResolvedValue(ASSET_TYPES);
    vi.mocked(createFixedAsset).mockRejectedValue(new ApiError("conflict", 409));

    renderStockAdd();

    const select = await screen.findByLabelText("Demirbaş Türü");
    fireEvent.change(select, { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Demirbaş Adı"), {
      target: { value: "Mevcut Demirbaş" },
    });
    fireEvent.change(screen.getByLabelText("Fiyat"), { target: { value: "100" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "1" } });

    fireEvent.click(screen.getByRole("button", { name: "EKLE" }));

    expect(await screen.findByText("Kayıtlı Demirbaş...")).toBeInTheDocument();
  });

  it("AC-12: clicking the back button calls navigate with /admin", async () => {
    renderStockAdd();
    await waitFor(() => expect(listAssetTypes).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });
});
