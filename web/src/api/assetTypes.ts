import { apiFetch } from "./client";

export interface AssetType {
  id: number;
  name: string;
}

/**
 * Calls GET /api/asset-types to list the asset types a fixed asset can be
 * assigned to. On failure, apiFetch rejects with an ApiError — the caller
 * decides how to surface that.
 */
export function listAssetTypes(): Promise<AssetType[]> {
  return apiFetch<AssetType[]>("/asset-types");
}
