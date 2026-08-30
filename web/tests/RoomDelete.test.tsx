import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RoomDelete } from "../src/routes/RoomDelete";
import { listRooms, deleteRoom } from "../src/api/rooms";

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
  deleteRoom: vi.fn(),
}));

function renderRoomDelete() {
  return render(
    <MemoryRouter>
      <RoomDelete />
    </MemoryRouter>,
  );
}

const ROOMS = [
  { id: 1, name: "Toplantı Odası", departmentId: 1 },
  { id: 2, name: "Depo", departmentId: 2 },
];

describe("RoomDelete", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listRooms).mockReset();
    vi.mocked(deleteRoom).mockReset();
    vi.mocked(listRooms).mockResolvedValue([]);
  });

  it("AC-1: renders the room selector and SİL button", async () => {
    renderRoomDelete();

    expect(screen.getByLabelText("Oda")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "SİL" })).toBeInTheDocument();

    await waitFor(() => expect(listRooms).toHaveBeenCalled());
  });

  it("AC-2: the selector's options come from listRooms()'s resolved rooms", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);

    renderRoomDelete();

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

  it("AC-3: selecting a room and clicking SİL immediately calls deleteRoom, shows success, resets selection, and reloads the list", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(deleteRoom).mockResolvedValue({ id: 1, name: "Toplantı Odası", departmentId: 1 });

    renderRoomDelete();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Toplantı Odası" } });

    fireEvent.click(screen.getByRole("button", { name: "SİL" }));

    await waitFor(() => expect(deleteRoom).toHaveBeenCalledWith("Toplantı Odası"));

    expect(await screen.findByText("Oda başarıyla silindi.")).toBeInTheDocument();
    expect(screen.getByLabelText("Oda")).toHaveValue("");
    expect(listRooms).toHaveBeenCalledTimes(2);
  });

  it("SİL is disabled when no room is selected and never calls deleteRoom", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);

    renderRoomDelete();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    const button = screen.getByRole("button", { name: "SİL" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(deleteRoom).not.toHaveBeenCalled();
  });

  it("AC-9: clicking the back button calls navigate with /admin", async () => {
    renderRoomDelete();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });

  it("shows the generic failure message when deleteRoom rejects for any reason", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(deleteRoom).mockRejectedValue(new Error("boom"));

    renderRoomDelete();

    const select = await screen.findByLabelText("Oda");
    fireEvent.change(select, { target: { value: "Depo" } });

    fireEvent.click(screen.getByRole("button", { name: "SİL" }));

    expect(await screen.findByText("Hatalı İşlem...")).toBeInTheDocument();
  });
});
