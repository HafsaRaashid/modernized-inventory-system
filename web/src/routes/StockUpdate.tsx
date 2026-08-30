import { useEffect, useState, type FormEvent, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./StockUpdate.css";
import { listAssetTypes, type AssetType } from "../api/assetTypes";
import { listFixedAssets, updateFixedAsset, type FixedAsset } from "../api/fixedAssets";

const SUCCESS_MESSAGE = "Demirbaş başarıyla güncellendi.";
const FAILURE_MESSAGE = "Güncellenirken hata oluştu...";

const ALLOWED_CONTROL_KEYS = new Set([
  "Backspace",
  "Delete",
  "Tab",
  "Escape",
  "Enter",
  "ArrowLeft",
  "ArrowRight",
  "ArrowUp",
  "ArrowDown",
  "Home",
  "End",
]);

/**
 * Blocks any keypress that isn't a digit, a comma, a navigation/editing
 * key, or a modifier-combo (copy/paste/select-all/cut), used by the Price
 * and Quantity fields. Parsing is deferred to submit time.
 */
function handleDigitsAndCommaKeyDown(event: KeyboardEvent<HTMLInputElement>) {
  if (event.ctrlKey || event.metaKey || ALLOWED_CONTROL_KEYS.has(event.key)) {
    return;
  }
  if (!/^[0-9,]$/.test(event.key)) {
    event.preventDefault();
  }
}

/**
 * Blocks any keypress that isn't a letter (including Turkish letters like
 * ı, ş, ğ, ü, ö, ç and their uppercase forms), a comma, a navigation/editing
 * key, or a modifier-combo (copy/paste/select-all/cut), used by the
 * asset-name field. Unlike StockAdd's asset-name field (deliberately
 * unfiltered — CQ-015 legacy defect), Stock Update's name field is filtered
 * to letters only.
 */
function handleLettersAndCommaKeyDown(event: KeyboardEvent<HTMLInputElement>) {
  if (event.ctrlKey || event.metaKey || ALLOWED_CONTROL_KEYS.has(event.key)) {
    return;
  }
  if (!/^[\p{L},]$/u.test(event.key)) {
    event.preventDefault();
  }
}

/**
 * Stock Update screen (SCR-009, "DEMİRBAŞ GÜNCELLEME"). A bordered section
 * holding an existing-asset selector (populated via listFixedAssets()), and
 * asset-name/price/purchase-date/asset-type/quantity fields that are filled
 * in from the selected asset (looked up in the already-fetched list — no
 * extra API call per selection), and a centered "GÜNCELLE" submit button. A
 * back link returns to /admin, mirroring RoomUpdate's useNavigate() pattern.
 *
 * Submission is blocked client-side until an asset is selected and the name
 * (after trim), price, and quantity are all non-empty. Price and quantity
 * accept only digits and a comma while typing; the asset-name field accepts
 * only letters and a comma while typing (the opposite of StockAdd's
 * deliberately-unfiltered name field). At submit time any comma in price/
 * quantity is replaced with a "." before parsing to a number. On success,
 * the form resets, the selector is re-populated (matching RoomUpdate's
 * "combo re-populated" behavior), and the legacy success message is shown.
 * Any failure — not-found or otherwise — maps to the SAME generic message;
 * there is no per-status branching.
 */
export function StockUpdate() {
  const navigate = useNavigate();

  const [assets, setAssets] = useState<FixedAsset[]>([]);
  const [assetTypes, setAssetTypes] = useState<AssetType[]>([]);
  const [selectedAssetId, setSelectedAssetId] = useState("");
  const [assetName, setAssetName] = useState("");
  const [price, setPrice] = useState("");
  const [purchaseDate, setPurchaseDate] = useState("");
  const [assetTypeId, setAssetTypeId] = useState("");
  const [quantity, setQuantity] = useState("");
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  function loadAssets() {
    listFixedAssets()
      .then(setAssets)
      .catch(() => {
        // Leave the selector empty; the form simply can't be submitted.
      });
  }

  useEffect(() => {
    loadAssets();
    listAssetTypes()
      .then(setAssetTypes)
      .catch(() => {
        // Leave the picker empty; the form simply can't be submitted.
      });
  }, []);

  function handleAssetSelect(id: string) {
    setSelectedAssetId(id);
    const selected = assets.find((asset) => String(asset.id) === id);
    if (selected) {
      setAssetName(selected.name);
      setPrice(String(selected.price));
      setPurchaseDate(selected.purchaseDate);
      setAssetTypeId(String(selected.assetTypeId));
      setQuantity(String(selected.quantity));
    } else {
      setAssetName("");
      setPrice("");
      setPurchaseDate("");
      setAssetTypeId("");
      setQuantity("");
    }
  }

  const canSubmit =
    selectedAssetId !== "" &&
    assetName.trim() !== "" &&
    price !== "" &&
    quantity !== "";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSubmit) {
      return;
    }

    try {
      const parsedPrice = Number(price.replace(",", "."));
      const parsedQuantity = Number(quantity.replace(",", "."));
      await updateFixedAsset(
        Number(selectedAssetId),
        assetName.trim(),
        parsedPrice,
        purchaseDate,
        Number(assetTypeId),
        parsedQuantity,
      );
      setSelectedAssetId("");
      setAssetName("");
      setPrice("");
      setPurchaseDate("");
      setAssetTypeId("");
      setQuantity("");
      loadAssets();
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch {
      setMessage({ text: FAILURE_MESSAGE, kind: "error" });
    }
  }

  return (
    <div className="stock-update">
      <form className="stock-update__section" onSubmit={handleSubmit}>
        <h2 className="stock-update__title">DEMİRBAŞ GÜNCELLEME</h2>
        <button
          type="button"
          className="stock-update__back"
          onClick={() => navigate("/admin")}
        >
          Geri
        </button>
        <div className="stock-update__field">
          <label htmlFor="asset-select">Demirbaş</label>
          <select
            id="asset-select"
            name="asset-select"
            className="stock-update__select"
            value={selectedAssetId}
            onChange={(event) => handleAssetSelect(event.target.value)}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {assets.map((asset) => (
              <option key={asset.id} value={asset.id}>
                {asset.name}
              </option>
            ))}
          </select>
        </div>
        <div className="stock-update__field">
          <label htmlFor="asset-name">Demirbaş Adı</label>
          <input
            id="asset-name"
            name="asset-name"
            type="text"
            className="stock-update__input"
            value={assetName}
            onKeyDown={handleLettersAndCommaKeyDown}
            onChange={(event) => setAssetName(event.target.value)}
          />
        </div>
        <div className="stock-update__field">
          <label htmlFor="price">Fiyat</label>
          <input
            id="price"
            name="price"
            type="text"
            className="stock-update__input"
            value={price}
            onKeyDown={handleDigitsAndCommaKeyDown}
            onChange={(event) => setPrice(event.target.value)}
          />
        </div>
        <div className="stock-update__field">
          <label htmlFor="purchase-date">Alım Tarihi</label>
          <input
            id="purchase-date"
            name="purchase-date"
            type="date"
            className="stock-update__input"
            value={purchaseDate}
            onChange={(event) => setPurchaseDate(event.target.value)}
          />
        </div>
        <div className="stock-update__field">
          <label htmlFor="asset-type">Demirbaş Türü</label>
          <select
            id="asset-type"
            name="asset-type"
            className="stock-update__select"
            value={assetTypeId}
            onChange={(event) => setAssetTypeId(event.target.value)}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {assetTypes.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>
        </div>
        <div className="stock-update__field">
          <label htmlFor="asset-type-id">Demirbaş Türü ID</label>
          <input
            id="asset-type-id"
            name="asset-type-id"
            type="text"
            className="stock-update__input"
            value={assetTypeId}
            disabled
            readOnly
          />
        </div>
        <div className="stock-update__field">
          <label htmlFor="quantity">Miktar</label>
          <input
            id="quantity"
            name="quantity"
            type="text"
            className="stock-update__input"
            value={quantity}
            onKeyDown={handleDigitsAndCommaKeyDown}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        {message && (
          <span
            className={`stock-update__message stock-update__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button type="submit" className="stock-update__button" disabled={!canSubmit}>
          GÜNCELLE
        </button>
      </form>
    </div>
  );
}
