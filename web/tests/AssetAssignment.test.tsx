import { render, screen, fireEvent, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssetAssignment } from "../src/routes/AssetAssignment";
import { ApiError } from "../src/api/client";
import { listRooms } from "../src/api/rooms";
import { listFixedAssets } from "../src/api/fixedAssets";
import { createAssetAssignment, listRoomAssetAssignments } from "../src/api/assetAssignments";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock("../src/api/rooms", () => ({
  listRooms: vi.fn(),
}));

vi.mock("../src/api/fixedAssets", () => ({
  listFixedAssets: vi.fn(),
}));

vi.mock("../src/api/assetAssignments", () => ({
  createAssetAssignment: vi.fn(),
  listRoomAssetAssignments: vi.fn(),
}));

function renderAssetAssignment() {
  return render(
    <MemoryRouter>
      <AssetAssignment />
    </MemoryRouter>,
  );
}

const ROOMS = [
  { id: 1, name: "Toplantı Odası", departmentId: 1 },
  { id: 2, name: "Depo", departmentId: 2 },
];

const ASSETS = [
  { id: 5, name: "Sandalye", price: 100, purchaseDate: "2024-01-01", assetTypeId: 1, quantity: 5 },
  { id: 6, name: "Masa", price: 200, purchaseDate: "2024-01-02", assetTypeId: 1, quantity: 10 },
];

const ASSIGNMENTS = [{ id: 1, assetId: 5, assetName: "Sandalye", quantity: 3 }];

describe("AssetAssignment", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listRooms).mockReset();
    vi.mocked(listFixedAssets).mockReset();
    vi.mocked(createAssetAssignment).mockReset();
    vi.mocked(listRoomAssetAssignments).mockReset();
    vi.mocked(listRooms).mockResolvedValue([]);
    vi.mocked(listFixedAssets).mockResolvedValue([]);
    vi.mocked(listRoomAssetAssignments).mockResolvedValue([]);
  });

  it("AC-1: renders both selects, both disabled echo inputs, the quantity input, the KAYDET button, and the assignments panel", async () => {
    renderAssetAssignment();

    expect(screen.getByLabelText("Oda")).toBeInTheDocument();
    expect(screen.getByLabelText("Demirbaş")).toBeInTheDocument();

    const roomNameInput = screen.getByLabelText("Oda Adı");
    expect(roomNameInput).toBeInTheDocument();
    expect(roomNameInput).toBeDisabled();

    const assetNameInput = screen.getByLabelText("Demirbaş Adı");
    expect(assetNameInput).toBeInTheDocument();
    expect(assetNameInput).toBeDisabled();

    expect(screen.getByLabelText("Miktar")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "KAYDET" })).toBeInTheDocument();
    expect(screen.getByText("Odadaki Demirbaşlar")).toBeInTheDocument();
    expect(screen.getByRole("table")).toBeInTheDocument();

    await waitFor(() => expect(listRooms).toHaveBeenCalled());
    await waitFor(() => expect(listFixedAssets).toHaveBeenCalled());
  });

  it("AC-2: selecting a room updates the room-name echo, selecting an asset updates the asset-name echo", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "2" } });
    expect(screen.getByLabelText("Oda Adı")).toHaveValue("Depo");

    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "6" } });
    expect(screen.getByLabelText("Demirbaş Adı")).toHaveValue("Masa");
  });

  it("AC-3: selecting a room calls listRoomAssetAssignments with that room's numeric id, and the returned rows render in the panel", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(listRoomAssetAssignments).mockResolvedValue(ASSIGNMENTS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "2" } });

    await waitFor(() => expect(listRoomAssetAssignments).toHaveBeenCalledWith(2));
    const table = screen.getByRole("table");
    expect(await within(table).findByText("Sandalye")).toBeInTheDocument();
    expect(within(table).getByText("3")).toBeInTheDocument();
  });

  it("AC-4: with only a room selected, KAYDET stays disabled and createAssetAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createAssetAssignment).not.toHaveBeenCalled();
  });

  it("AC-4: with room and asset selected but no quantity, KAYDET stays disabled and createAssetAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createAssetAssignment).not.toHaveBeenCalled();
  });

  it("AC-4: with nothing selected, KAYDET stays disabled and createAssetAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createAssetAssignment).not.toHaveBeenCalled();
  });

  it("AC-5: a quantity greater than the selected asset's known stock keeps KAYDET disabled and createAssetAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "6" } });

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createAssetAssignment).not.toHaveBeenCalled();
  });

  it("AC-5: a quantity exactly equal to the selected asset's known stock does NOT disable KAYDET (boundary case)", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "5" } });

    expect(screen.getByRole("button", { name: "KAYDET" })).not.toBeDisabled();
  });

  it("AC-6: with room, asset, and a valid quantity, clicking KAYDET creates the assignment, shows success, clears only the quantity field, and re-fetches assets and assignments", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(listRoomAssetAssignments).mockResolvedValue(ASSIGNMENTS);
    vi.mocked(createAssetAssignment).mockResolvedValue({
      id: 2,
      roomId: 1,
      assetId: 5,
      personnelId: 10,
      quantity: 3,
      remainingStock: 2,
    });

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "3" } });

    vi.mocked(listFixedAssets).mockClear();
    vi.mocked(listRoomAssetAssignments).mockClear();

    fireEvent.click(screen.getByRole("button", { name: "KAYDET" }));

    await waitFor(() => expect(createAssetAssignment).toHaveBeenCalledWith(1, 5, 3));

    expect(await screen.findByText("Odaya Demirbaş Atandı")).toBeInTheDocument();
    expect(screen.getByLabelText("Miktar")).toHaveValue("");
    expect(screen.getByLabelText("Oda")).toHaveValue("1");
    expect(screen.getByLabelText("Demirbaş")).toHaveValue("5");

    await waitFor(() => expect(listFixedAssets).toHaveBeenCalled());
    await waitFor(() => expect(listRoomAssetAssignments).toHaveBeenCalledWith(1));
  });

  it("shows the server's own message when createAssetAssignment rejects with an INSUFFICIENT_STOCK ApiError", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(createAssetAssignment).mockRejectedValue(
      new ApiError("Request failed", 409, {
        error: "INSUFFICIENT_STOCK",
        message: "some server message",
      }),
    );

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "3" } });

    fireEvent.click(screen.getByRole("button", { name: "KAYDET" }));

    expect(await screen.findByText("some server message")).toBeInTheDocument();
  });

  it("shows the generic failure message when createAssetAssignment rejects with a non-ApiError", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listFixedAssets).mockResolvedValue(ASSETS);
    vi.mocked(createAssetAssignment).mockRejectedValue(new Error("failed"));

    renderAssetAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const assetSelect = await screen.findByLabelText("Demirbaş");
    fireEvent.change(assetSelect, { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Miktar"), { target: { value: "3" } });

    fireEvent.click(screen.getByRole("button", { name: "KAYDET" }));

    expect(
      await screen.findByText("Demirbaş atanırken bir hata oluştu."),
    ).toBeInTheDocument();
  });

  it("clicking the back button navigates to /", async () => {
    renderAssetAssignment();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/");
  });
});
