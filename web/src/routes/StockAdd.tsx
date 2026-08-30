import { useEffect, useState, type FormEvent, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./StockAdd.css";
import { ApiError } from "../api/client";
import { listAssetTypes, type AssetType } from "../api/assetTypes";
import { createFixedAsset } from "../api/fixedAssets";

const SUCCESS_MESSAGE = "Demirbaş başarıyla eklendi.";
const DUPLICATE_MESSAGE = "Kayıtlı Demirbaş...";
const GENERIC_FAILURE_MESSAGE = "Demirbaş eklenirken bir hata oluştu.";

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
 * Stock Add screen ("DEMİRBAŞ EKLEME"). A bordered section holding an
 * asset-name input (deliberately unfiltered — CQ-015 legacy defect, not an
 * oversight), a price input, a native date input for the purchase date, an
 * asset-type picker (a single <select> stands in for the legacy paired
 * ID/name list — picking a name option inherently picks its ID), a disabled
 * input echoing the selected asset type's numeric ID, a quantity input, and
 * a centered "EKLE" submit button. A back link returns to /admin, mirroring
 * RoomAdd's useNavigate() pattern.
 *
 * Submission is blocked client-side until the asset name (after trim), the
 * price, the quantity, and an asset-type selection are all present. Price
 * and quantity accept only digits and a comma while typing; at submit time
 * any comma is replaced with a "." before parsing to a number. On success,
 * the form resets and shows the legacy success message; on a
 * duplicate-name conflict (409), shows the legacy "Kayıtlı Demirbaş..."
 * message; any other failure shows a generic message without crashing the
 * UI.
 */
export function StockAdd() {
  const navigate = useNavigate();

  const [assetTypes, setAssetTypes] = useState<AssetType[]>([]);
  const [assetName, setAssetName] = useState("");
  const [price, setPrice] = useState("");
  const [purchaseDate, setPurchaseDate] = useState("");
  const [assetTypeId, setAssetTypeId] = useState("");
  const [quantity, setQuantity] = useState("");
  const [message, setMessage] = useState<{ text: string; kind: "success" | "error" } | null>(null);

  useEffect(() => {
    let cancelled = false;
    listAssetTypes()
      .then((result) => {
        if (!cancelled) {
          setAssetTypes(result);
        }
      })
      .catch(() => {
        // Leave the picker empty; the form simply can't be submitted.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const canSubmit =
    assetName.trim() !== "" && price !== "" && quantity !== "" && assetTypeId !== "";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!canSubmit) {
      return;
    }

    try {
      const parsedPrice = Number(price.replace(",", "."));
      const parsedQuantity = Number(quantity.replace(",", "."));
      await createFixedAsset(
        assetName.trim(),
        parsedPrice,
        purchaseDate,
        Number(assetTypeId),
        parsedQuantity,
      );
      setAssetName("");
      setPrice("");
      setPurchaseDate("");
      setAssetTypeId("");
      setQuantity("");
      setMessage({ text: SUCCESS_MESSAGE, kind: "success" });
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setMessage({ text: DUPLICATE_MESSAGE, kind: "error" });
      } else {
        setMessage({ text: GENERIC_FAILURE_MESSAGE, kind: "error" });
      }
    }
  }

  return (
    <div className="stock-add">
      <form className="stock-add__section" onSubmit={handleSubmit}>
        <h2 className="stock-add__title">DEMİRBAŞ EKLEME</h2>
        <button
          type="button"
          className="stock-add__back"
          onClick={() => navigate("/admin")}
        >
          Geri
        </button>
        <div className="stock-add__field">
          <label htmlFor="asset-name">Demirbaş Adı</label>
          <input
            id="asset-name"
            name="asset-name"
            type="text"
            className="stock-add__input"
            value={assetName}
            onChange={(event) => setAssetName(event.target.value)}
          />
        </div>
        <div className="stock-add__field">
          <label htmlFor="price">Fiyat</label>
          <input
            id="price"
            name="price"
            type="text"
            className="stock-add__input"
            value={price}
            onKeyDown={handleDigitsAndCommaKeyDown}
            onChange={(event) => setPrice(event.target.value)}
          />
        </div>
        <div className="stock-add__field">
          <label htmlFor="purchase-date">Alım Tarihi</label>
          <input
            id="purchase-date"
            name="purchase-date"
            type="date"
            className="stock-add__input"
            value={purchaseDate}
            onChange={(event) => setPurchaseDate(event.target.value)}
          />
        </div>
        <div className="stock-add__field">
          <label htmlFor="asset-type">Demirbaş Türü</label>
          <select
            id="asset-type"
            name="asset-type"
            className="stock-add__select"
            value={assetTypeId}
            onChange={(event) => setAssetTypeId(event.target.value)}
          >
            <option value="" disabled>
              Seçiniz
            </option>
            {assetTypes.map((assetType) => (
              <option key={assetType.id} value={assetType.id}>
                {assetType.name}
              </option>
            ))}
          </select>
        </div>
        <div className="stock-add__field">
          <label htmlFor="asset-type-id">Demirbaş Türü ID</label>
          <input
            id="asset-type-id"
            name="asset-type-id"
            type="text"
            className="stock-add__input"
            value={assetTypeId}
            disabled
            readOnly
          />
        </div>
        <div className="stock-add__field">
          <label htmlFor="quantity">Miktar</label>
          <input
            id="quantity"
            name="quantity"
            type="text"
            className="stock-add__input"
            value={quantity}
            onKeyDown={handleDigitsAndCommaKeyDown}
            onChange={(event) => setQuantity(event.target.value)}
          />
        </div>
        {message && (
          <span
            className={`stock-add__message stock-add__message--${message.kind}`}
            role="alert"
          >
            {message.text}
          </span>
        )}
        <button type="submit" className="stock-add__button" disabled={!canSubmit}>
          EKLE
        </button>
      </form>
    </div>
  );
}
