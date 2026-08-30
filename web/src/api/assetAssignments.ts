import { apiFetch } from "./client";

export interface AssetAssignment {
  id: number;
  roomId: number;
  assetId: number;
  personnelId: number;
  quantity: number;
  remainingStock: number;
}

export interface RoomAssetAssignmentRow {
  id: number;
  assetId: number;
  assetName: string;
  quantity: number;
}

/**
 * Calls POST /api/asset-assignments to issue a quantity of a fixed asset
 * (demirbaş) to a room. The room must already have a responsible-personnel
 * assignment (see roomAssignments.ts) — the server resolves that room's
 * most-recently-created responsibility row to attribute the issued
 * quantity to a personnelId. On success, resolves with the created
 * assignment, including the asset's remainingStock after the decrement. On
 * failure (e.g. a missing selection, an invalid reference, or insufficient
 * stock), apiFetch rejects with an ApiError — the caller shows the
 * appropriate failure message.
 */
export function createAssetAssignment(
  roomId: number,
  assetId: number,
  quantity: number,
): Promise<AssetAssignment> {
  return apiFetch<AssetAssignment>("/asset-assignments", {
    method: "POST",
    body: { roomId, assetId, quantity },
  });
}

/**
 * Calls GET /api/asset-assignments?roomId= to list the fixed assets
 * currently assigned to a room (rows with a non-null assetId only — a
 * room's responsibility row is excluded). Used to populate the Asset
 * Assignment screen's current-assignments panel.
 */
export function listRoomAssetAssignments(roomId: number): Promise<RoomAssetAssignmentRow[]> {
  return apiFetch<RoomAssetAssignmentRow[]>(`/asset-assignments?roomId=${roomId}`);
}
