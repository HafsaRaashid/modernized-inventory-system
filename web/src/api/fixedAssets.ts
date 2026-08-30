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
