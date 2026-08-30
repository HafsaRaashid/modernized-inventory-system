import { apiFetch } from "./client";

export interface FixedAsset {
  id: number;
  name: string;
  price: number;
  purchaseDate: string;
  assetTypeId: number;
  quantity: number;
}

/**
 * Calls POST /api/fixed-assets to create a new fixed asset (demirbaş) under
 * the given asset type. On success, resolves with the created fixed asset.
 * On failure (e.g. a duplicate asset name), apiFetch rejects with an
 * ApiError — the caller shows the appropriate failure message.
 */
export function createFixedAsset(
  name: string,
  price: number,
  purchaseDate: string,
  assetTypeId: number,
  quantity: number,
): Promise<FixedAsset> {
  return apiFetch<FixedAsset>("/fixed-assets", {
    method: "POST",
    body: { name, price, purchaseDate, assetTypeId, quantity },
  });
}

/**
 * Calls GET /api/fixed-assets to list all fixed assets (demirbaş). On
 * failure, apiFetch rejects with an ApiError — the caller decides how to
 * surface that.
 */
export function listFixedAssets(): Promise<FixedAsset[]> {
  return apiFetch<FixedAsset[]>("/fixed-assets");
}

/**
 * Calls PUT /api/fixed-assets to update an existing fixed asset (demirbaş).
 * On success, resolves with the updated fixed asset. On failure, apiFetch
 * rejects with an ApiError — the caller shows the appropriate failure
 * message.
 */
export function updateFixedAsset(
  id: number,
  name: string,
  price: number,
  purchaseDate: string,
  assetTypeId: number,
  quantity: number,
): Promise<FixedAsset> {
  return apiFetch<FixedAsset>("/fixed-assets", {
    method: "PUT",
    body: { id, name, price, purchaseDate, assetTypeId, quantity },
  });
}
