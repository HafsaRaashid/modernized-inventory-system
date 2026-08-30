import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RoomAdd } from "../src/routes/RoomAdd";
import { ApiError } from "../src/api/client";
import { listDepartments } from "../src/api/departments";
import { createRoom } from "../src/api/rooms";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock("../src/api/departments", () => ({
  listDepartments: vi.fn(),
}));

vi.mock("../src/api/rooms", () => ({
  createRoom: vi.fn(),
}));

function renderRoomAdd() {
  return render(
    <MemoryRouter>
      <RoomAdd />
    </MemoryRouter>,
  );
}

const DEPARTMENTS = [
  { id: 1, name: "İnsan Kaynakları" },
  { id: 2, name: "Muhasebe" },
];

describe("RoomAdd", () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    vi.mocked(listDepartments).mockReset();
    vi.mocked(createRoom).mockReset();
    vi.mocked(listDepartments).mockResolvedValue([]);
  });

  it("AC-1: renders the room-name input, department picker, disabled department-ID echo input, and EKLE button", async () => {
    renderRoomAdd();

    expect(screen.getByLabelText("Oda Adı")).toBeInTheDocument();
    expect(screen.getByLabelText("Departman")).toBeInTheDocument();
    const departmentIdInput = screen.getByLabelText("Departman ID");
    expect(departmentIdInput).toBeInTheDocument();
    expect(departmentIdInput).toBeDisabled();
    expect(screen.getByRole("button", { name: "EKLE" })).toBeInTheDocument();

    await waitFor(() => expect(listDepartments).toHaveBeenCalled());
  });

  it("AC-2: selecting a department option updates the disabled echo input's value to that department's id", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);

    renderRoomAdd();

    const select = await screen.findByLabelText("Departman");
    fireEvent.change(select, { target: { value: "2" } });

    expect(screen.getByLabelText("Departman ID")).toHaveValue("2");
  });

  it("AC-3: submitting with a room name and department calls createRoom and shows success, resetting fields", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);
    vi.mocked(createRoom).mockResolvedValue({ id: 10, name: "Toplantı Odası", departmentId: 2 });

    renderRoomAdd();

    const select = await screen.findByLabelText("Departman");
    fireEvent.change(select, { target: { value: "2" } });
    fireEvent.change(screen.getByLabelText("Oda Adı"), {
      target: { value: "  Toplantı Odası  " },
    });

    fireEvent.click(screen.getByRole("button", { name: "EKLE" }));

    await waitFor(() =>
      expect(createRoom).toHaveBeenCalledWith("Toplantı Odası", 2),
    );

    expect(await screen.findByText("Oda başarıyla eklendi.")).toBeInTheDocument();
    expect(screen.getByLabelText("Oda Adı")).toHaveValue("");
    expect(screen.getByLabelText("Departman ID")).toHaveValue("");
  });

  it("AC-4: leaving the room name empty keeps EKLE disabled and never calls createRoom", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);

    renderRoomAdd();

    const select = await screen.findByLabelText("Departman");
    fireEvent.change(select, { target: { value: "1" } });

    const button = screen.getByRole("button", { name: "EKLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createRoom).not.toHaveBeenCalled();
  });

  it("AC-4: a whitespace-only room name keeps EKLE disabled and never calls createRoom", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);

    renderRoomAdd();

    const select = await screen.findByLabelText("Departman");
    fireEvent.change(select, { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Oda Adı"), { target: { value: "   " } });

    const button = screen.getByRole("button", { name: "EKLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createRoom).not.toHaveBeenCalled();
  });

  it("AC-11: clicking the back button calls navigate with /admin", async () => {
    renderRoomAdd();
    await waitFor(() => expect(listDepartments).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Geri" }));
    expect(mockNavigate).toHaveBeenCalledWith("/admin");
  });

  it("AC-13: leaving no department selected keeps EKLE disabled and never calls createRoom", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);

    renderRoomAdd();
    await waitFor(() => expect(listDepartments).toHaveBeenCalled());

    fireEvent.change(screen.getByLabelText("Oda Adı"), {
      target: { value: "Yeni Oda" },
    });

    const button = screen.getByRole("button", { name: "EKLE" });
    expect(button).toBeDisabled();

    fireEvent.click(button);
    expect(createRoom).not.toHaveBeenCalled();
  });

  it("shows the duplicate-room message when createRoom rejects with a 409 ApiError", async () => {
    vi.mocked(listDepartments).mockResolvedValue(DEPARTMENTS);
    vi.mocked(createRoom).mockRejectedValue(new ApiError("conflict", 409));

    renderRoomAdd();

    const select = await screen.findByLabelText("Departman");
    fireEvent.change(select, { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Oda Adı"), {
      target: { value: "Mevcut Oda" },
    });

    fireEvent.click(screen.getByRole("button", { name: "EKLE" }));

    expect(await screen.findByText("Kayıtlı Oda...")).toBeInTheDocument();
  });
});
