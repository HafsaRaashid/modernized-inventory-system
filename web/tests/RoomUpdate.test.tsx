import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RoomUpdate } from "../src/routes/RoomUpdate";
import { listRooms, updateRoom } from "../src/api/rooms";

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
  updateRoom: vi.fn(),
}));

function renderRoomUpdate() {
  return render(
    <MemoryRouter>
      <RoomUpdate />
    </MemoryRouter>,
  );
}

const ROOMS = [
  { id: 1, name: "Toplantı Odası", departmentId: 1 },
  { id: 2, name: "Depo", departmentId: 2 },
];

describe("RoomUpdate", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listRooms).mockReset();
    vi.mocked(updateRoom).mockReset();
    vi.mocked(listRooms).mockResolvedValue([]);
  });

  it("AC-1: renders the room selector, new-name input, and GÜNCELLE button", async () => {
    renderRoomUpdate();

    expect(screen.getByLabelText("Oda")).toBeInTheDocument();
    expect(screen.getByLabelText("Yeni Ad")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "GÜNCELLE" })).toBeInTheDocument();

    await waitFor(() => expect(listRooms).toHaveBeenCalled());
  });

  it("AC-2: the selector's options come from listRooms()'s resolved rooms", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);

    renderRoomUpdate();

    const select = await screen.findByLabelText("Oda");
    const options = Array.from(select.querySelectorAll("option")).map((option) => ({
      value: option.getAttribute("value"),
      label: option.textContent,
    }));

    expect(options).toEqual(
      expect.arrayContaining([
        { value: "Toplantı Odası", label: "Toplantı Odası" },
        { value: "Depo", label: "Depo" },
      ]),
    );
  });

  it("AC-3: submitting a selected room and new name calls updateRoom, shows success, resets fields, and reloads the list", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(updateRoom).mockResolvedValue({ id: 1, name: "Yeni Oda", departmentId: 1 });

    renderRoomUpdate();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Toplantı Odası" } });
    fireEvent.change(screen.getByLabelText("Yeni Ad"), {
      target: { value: "  Yeni Oda  " },
    });

    fireEvent.click(screen.getByRole("button", { name: "GÜNCELLE" }));

    await waitFor(() =>
      expect(updateRoom).toHaveBeenCalledWith("Toplantı Odası", "Yeni Oda"),
    );

    expect(await screen.findByText("Oda başarıyla güncellendi.")).toBeInTheDocument();
    expect(screen.getByLabelText("Oda")).toHaveValue("");
    expect(screen.getByLabelText("Yeni Ad")).toHaveValue("");
    expect(listRooms).toHaveBeenCalledTimes(2);
  });

  it("AC-4: leaving the new name empty keeps GÜNCELLE disabled and never calls updateRoom", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);

    renderRoomUpdate();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Toplantı Odası" } });

    const button = screen.getByRole("button", { name: "GÜNCELLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(updateRoom).not.toHaveBeenCalled();
  });

  it("AC-4: a whitespace-only new name keeps GÜNCELLE disabled and never calls updateRoom", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);

    renderRoomUpdate();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Toplantı Odası" } });
    fireEvent.change(screen.getByLabelText("Yeni Ad"), { target: { value: "   " } });

    const button = screen.getByRole("button", { name: "GÜNCELLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(updateRoom).not.toHaveBeenCalled();
  });

  it("AC-11: clicking the back button calls navigate with /admin", async () => {
    renderRoomUpdate();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });

  it("shows the generic failure message when updateRoom rejects for any reason", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(updateRoom).mockRejectedValue(new Error("boom"));

    renderRoomUpdate();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Depo" } });
    fireEvent.change(screen.getByLabelText("Yeni Ad"), {
      target: { value: "Yeni Depo" },
    });

    fireEvent.click(screen.getByRole("button", { name: "GÜNCELLE" }));

    expect(await screen.findByText("Hatalı İşlem...")).toBeInTheDocument();
  });
});
