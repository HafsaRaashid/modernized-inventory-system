import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RoomAssignment } from "../src/routes/RoomAssignment";
import { listRooms } from "../src/api/rooms";
import { listPersonnel } from "../src/api/personnel";
import { createRoomAssignment } from "../src/api/roomAssignments";

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

vi.mock("../src/api/personnel", () => ({
  listPersonnel: vi.fn(),
}));

vi.mock("../src/api/roomAssignments", () => ({
  createRoomAssignment: vi.fn(),
}));

function renderRoomAssignment() {
  return render(
    <MemoryRouter>
      <RoomAssignment />
    </MemoryRouter>,
  );
}

const ROOMS = [
  { id: 1, name: "Toplantı Odası", departmentId: 1 },
  { id: 2, name: "Depo", departmentId: 2 },
];

const PERSONNEL = [
  { id: 10, firstName: "Ali", lastName: "Yılmaz" },
  { id: 20, firstName: "Ayşe", lastName: "Kaya" },
];

describe("RoomAssignment", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listRooms).mockReset();
    vi.mocked(listPersonnel).mockReset();
    vi.mocked(createRoomAssignment).mockReset();
    vi.mocked(listRooms).mockResolvedValue([]);
    vi.mocked(listPersonnel).mockResolvedValue([]);
  });

  it("AC-1: renders both selects, both disabled echo inputs, and the KAYDET button", async () => {
    renderRoomAssignment();

    expect(screen.getByLabelText("Oda")).toBeInTheDocument();
    expect(screen.getByLabelText("Personel")).toBeInTheDocument();

    const roomNameInput = screen.getByLabelText("Oda Adı");
    expect(roomNameInput).toBeInTheDocument();
    expect(roomNameInput).toBeDisabled();

    const personnelNameInput = screen.getByLabelText("Personel Adı");
    expect(personnelNameInput).toBeInTheDocument();
    expect(personnelNameInput).toBeDisabled();

    expect(screen.getByRole("button", { name: "KAYDET" })).toBeInTheDocument();

    await waitFor(() => expect(listRooms).toHaveBeenCalled());
    await waitFor(() => expect(listPersonnel).toHaveBeenCalled());
  });

  it("AC-3: selecting a room updates the room-name echo, selecting a personnel updates the personnel-name echo", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listPersonnel).mockResolvedValue(PERSONNEL);

    renderRoomAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "2" } });
    expect(screen.getByLabelText("Oda Adı")).toHaveValue("Depo");

    const personnelSelect = await screen.findByLabelText("Personel");
    fireEvent.change(personnelSelect, { target: { value: "20" } });
    expect(screen.getByLabelText("Personel Adı")).toHaveValue("Ayşe Kaya");
  });

  it("AC-4: selecting a room and personnel then clicking KAYDET creates the assignment, shows success, and resets", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listPersonnel).mockResolvedValue(PERSONNEL);
    vi.mocked(createRoomAssignment).mockResolvedValue({ id: 1, roomId: 2, personnelId: 20 });

    renderRoomAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "2" } });
    const personnelSelect = await screen.findByLabelText("Personel");
    fireEvent.change(personnelSelect, { target: { value: "20" } });

    fireEvent.click(screen.getByRole("button", { name: "KAYDET" }));

    await waitFor(() => expect(createRoomAssignment).toHaveBeenCalledWith(2, 20));

    expect(await screen.findByText("Atama başarıyla kaydedildi.")).toBeInTheDocument();
    expect(screen.getByLabelText("Oda")).toHaveValue("");
    expect(screen.getByLabelText("Personel")).toHaveValue("");
    expect(screen.getByLabelText("Oda Adı")).toHaveValue("");
    expect(screen.getByLabelText("Personel Adı")).toHaveValue("");
  });

  it("AC-5: with only a room selected, KAYDET stays disabled and createRoomAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listPersonnel).mockResolvedValue(PERSONNEL);

    renderRoomAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createRoomAssignment).not.toHaveBeenCalled();
  });

  it("AC-5: with neither selected, KAYDET stays disabled and createRoomAssignment is never called", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listPersonnel).mockResolvedValue(PERSONNEL);

    renderRoomAssignment();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    const button = screen.getByRole("button", { name: "KAYDET" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createRoomAssignment).not.toHaveBeenCalled();
  });

  it("AC-10: clicking the back button calls navigate with /", async () => {
    renderRoomAssignment();
    await waitFor(() => expect(listRooms).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/");
  });

  it("shows the generic failure message when createRoomAssignment rejects", async () => {
    vi.mocked(listRooms).mockResolvedValue(ROOMS);
    vi.mocked(listPersonnel).mockResolvedValue(PERSONNEL);
    vi.mocked(createRoomAssignment).mockRejectedValue(new Error("failed"));

    renderRoomAssignment();

    const roomSelect = await screen.findByLabelText("Oda");
    fireEvent.change(roomSelect, { target: { value: "1" } });
    const personnelSelect = await screen.findByLabelText("Personel");
    fireEvent.change(personnelSelect, { target: { value: "10" } });

    fireEvent.click(screen.getByRole("button", { name: "KAYDET" }));

    expect(
      await screen.findByText("Atama kaydedilirken bir hata oluştu."),
    ).toBeInTheDocument();
  });
});
