# UI Inventory: Fixed Asset & Inventory Tracking System (YazılımSınamaProjesi / DemirbasTakip)

**Path analyzed:** C:\Users\MohamedRaashidBISTEC\OneDrive - BISTEC Global\Documents\specclaw project\InventoryTrackingSystem\InventoryTrackingSystem
**Date analyzed:** 2026-08-18
**View technology identified:** Windows Forms (WinForms), .NET Framework 4.5. Each screen is a `System.Windows.Forms.Form` subclass built from a pair of files: a `frm*.Designer.cs` partial class whose `InitializeComponent()` method imperatively instantiates and positions `System.Windows.Forms.*` controls (`Button`, `TextBox`, `Label`, `DataGridView`, `ComboBox`, `ListBox`, `RadioButton`, `GroupBox`, `DateTimePicker`, `PictureBox`, `ErrorProvider`, `PrintDialog`) in C# (no markup language — there is no XAML, `.dfm`, HTML, or CSS anywhere in scope), and a `frm*.cs` code-behind file wiring event handlers directly to inline ADO.NET (`System.Data.SqlClient`) data access. Each form additionally has a `frm*.resx` resource file carrying embedded binary resources (three PNG icons) surfaced through the generated `Properties/Resources.Designer.cs` accessor class. Confirmed by direct `Read` of all 12 `frm*.Designer.cs`/`frm*.cs` pairs under `YazılımSınamaProjesi/` (e.g. `YazılımSınamaProjesi/frmGiris.Designer.cs`, `YazılımSınamaProjesi/frmAnaMenu.Designer.cs`). This agrees with `codebase-report.md`'s own Tech Stack section (WinForms, `.NETFramework,Version=v4.5`, `Application.Run()` bootstrap in `Program.cs`) — **no disagreement found** between the extension histogram (40 `.cs` + 14 `.resx`), `codebase-report.md`, and this run's direct file reads.

**Cross-referenced against:** `.specclaw/analysis/domain-model.md`, `.specclaw/analysis/functional-spec.md` (its own `## UI Inventory` section, lines 119–142)

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose (not even to describe it) — filling
  this template is a dumb global string replace, and a token mentioned here
  would get overwritten along with the real placeholder below, corrupting
  this comment. Refer to placeholders by section name instead.
-->

## Screens

### SCR-001 — Login (`GİRİŞ_EKRANI`)

**Purpose:** Inference: lets a user authenticate with a username and password before the rest of the application becomes reachable.
**Defined in:** `YazılımSınamaProjesi/frmGiris.Designer.cs:1-140` (class `GİRİŞ_EKRANI`); behavior in `YazılımSınamaProjesi/frmGiris.cs`
**Functional-spec UI Inventory line:** `functional-spec.md:125` (`GİRİŞ_EKRANI` row)
**Navigation in:** application entry point — `YazılımSınamaProjesi/Program.cs:19-20` (`GİRİŞ_EKRANI giris = new GİRİŞ_EKRANI(); giris.Show();`)
**Navigation out:** on successful login, opens Main Menu and closes this form — `YazılımSınamaProjesi/frmGiris.cs:47-49` (`frmAnaMenu anamenu = new frmAnaMenu(this); anamenu.Show();`); the login form is closed by the callee's own constructor, `YazılımSınamaProjesi/frmAnaMenu.cs:16-20` (`f.Close();`)

**Layout structure:**
- A top input block containing two stacked icon+field rows: the username row pairs a small non-interactive icon (`pbGirisEkraniUser`) at the left with a wider text field (`txtUsername`) to its right at the same vertical offset (`frmGiris.Designer.cs:95-103` icon, `:56-66` field).
- Directly below it, a second row repeats the same icon+field pairing for the password field, whose field is configured for masked entry (`frmGiris.Designer.cs:85-93` icon, `:68-79` field, `PasswordChar='*'` at `:74`).
- Below the input block, a single wide accent-colored primary action button spans roughly the same horizontal extent as the fields above it (`frmGiris.Designer.cs:43-54`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Kullanıcı Adı (username, placeholder-filled) | text input | User.KullaniciAdi | `frmGiris.Designer.cs:56-66` |
| Şifre (password, placeholder-filled) | text input (masked, `PasswordChar='*'`) | User.Sifre | `frmGiris.Designer.cs:68-79` |
| (username row icon) | label/static text (decorative image, non-interactive, `TabStop=false`) | — | `frmGiris.Designer.cs:95-103` |
| (password row icon) | label/static text (decorative image, non-interactive, `TabStop=false`) | — | `frmGiris.Designer.cs:85-93` |
| GİRİŞ | button | — | `frmGiris.Designer.cs:43-54` |

**States evidenced in code:** default (placeholder text pre-filled — `frmGiris.Designer.cs:64,77`), validation-error (empty field on blur — `frmGiris.cs:83-93,95-105`), login-failure (incorrect credentials — `frmGiris.cs:51-58`, fields reset to placeholder text)
**Token groups referenced:** TK-001

### SCR-002 — Main Menu (`frmAnaMenu`)

**Purpose:** Inference: central navigation hub routing to every other feature area; also the application's exit point.
**Defined in:** `YazılımSınamaProjesi/frmAnaMenu.Designer.cs:1-124`
**Functional-spec UI Inventory line:** `functional-spec.md:126`
**Navigation in:** from Login on success — `frmGiris.cs:47-49`
**Navigation out:** five edges — Search (`frmAnaMenu.cs:59-64`), Asset Assignment (`:66-71`), Room Assignment (`:73-78`), Admin Panel (`:80-85`), Report (`:92-97`); closing this form exits the whole application (`frmAnaMenu.cs:87-90`, `Application.Exit()`)

**Layout structure:**
- A 2×2 grid of large navigation buttons filling most of the form: top-left "ARAMALAR" (`frmAnaMenu.Designer.cs:38-47`), top-right "ODA DEMİRBAŞ İŞLEMLERİ" (`:49-58`), bottom-left "ODA TANIMLAMA" (`:60-69`), bottom-right "ADMİN" (`:71-80`).
- A full-width button occupying the horizontal band between the top and bottom button rows, holding the reporting entry point (`:82-91`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| ARAMALAR | button | — | `frmAnaMenu.Designer.cs:38-47` |
| ODA DEMİRBAŞ İŞLEMLERİ | button | — | `frmAnaMenu.Designer.cs:49-58` |
| ODA TANIMLAMA | button | — | `frmAnaMenu.Designer.cs:60-69` |
| ADMİN | button (conditionally enabled — DR-003) | User.YetkiID (gates this button only; see Widget Cross-Reference Findings) | `frmAnaMenu.Designer.cs:71-80` |
| Rapor Çıktısı Al | button | — | `frmAnaMenu.Designer.cs:82-91` |

**States evidenced in code:** admin-disabled (default; `btnAdmin.Enabled=false` — `frmAnaMenu.cs:50-53`), admin-enabled (`btnAdmin.Enabled=true` — `frmAnaMenu.cs:48-49`)
**Token groups referenced:** none

### SCR-003 — Search (`frmAramalar`)

**Purpose:** Inference: lets a user search fixed assets by one of five criteria, and independently search personnel by first/last name.
**Defined in:** `YazılımSınamaProjesi/frmAramalar.Designer.cs:1-337`
**Functional-spec UI Inventory line:** `functional-spec.md:135`
**Navigation in:** `frmAnaMenu.cs:59-64` (`btnArama_Click`)
**Navigation out:** `frmAramalar.cs:105-109` (`btnAramalarBack_Click` → Main Menu)

**Layout structure:**
- A back button pinned to the top-left corner (`frmAramalar.Designer.cs:271-282`).
- Two side-by-side bordered panels filling the rest of the form: a left panel for personnel search (`:60-75`) and a right panel for asset search (`:136-156`).
  - Left panel: a labeled first-name field and a labeled last-name field stacked vertically near the top (`:88-124`), a search button beside them (`:77-86`), and a results grid filling the panel below (`:126-134`).
  - Right panel: a label plus a row of five mutually-exclusive criterion radio buttons (`:176-231`) above a shared input slot that swaps between a free-text field and a date picker depending on which criterion is selected (`:158-163` date picker, `:243-249` text field, occupying the same position), a search button beside that row (`:165-174`), and a results grid below (`:233-241`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Ad (first name) | text input (letter-only keypress filter — DR-006) | Personnel.PersonelAdi | `frmAramalar.Designer.cs:117-124` |
| Soyad (last name) | text input (letter-only keypress filter — DR-006) | Personnel.PersonelSoyadi | `frmAramalar.Designer.cs:108-115` |
| ARAMA (personnel) | button | — | `frmAramalar.Designer.cs:77-86` |
| (personnel results) | grid/list | Personnel + RoomAssetAssignment + FixedAsset (joined) | `frmAramalar.Designer.cs:126-134` |
| Arama Türü (Demirbaş Adı / Demirbaş Türü / Fiyat / Alım Tarihi / Adet) | select/combo (mutually-exclusive radio-button group) | FixedAsset.DemirbasAdi / DemirbasTuruID / Fiyat / AlimTarihi / Adet (selects which is searched) | `frmAramalar.Designer.cs:220-231,209-218,198-207,187-196,176-185` |
| Bilgi Giriniz (search value, free-text) | text input | value for whichever FixedAsset field is selected above | `frmAramalar.Designer.cs:243-249` |
| (search value, date criterion only) | date picker | FixedAsset.AlimTarihi | `frmAramalar.Designer.cs:158-163` |
| ARAMA (asset) | button | — | `frmAramalar.Designer.cs:165-174` |
| (asset results) | grid/list | FixedAsset + AssetType (joined) | `frmAramalar.Designer.cs:233-241` |

**States evidenced in code:** default (Demirbaş Adı pre-checked, free-text field visible, date picker hidden — `frmAramalar.Designer.cs:222-223`, `frmAramalar.cs:77`), date-criterion (date picker visible, free-text field hidden — `frmAramalar.cs:70-74`), validation-error (empty free-text search value — `frmAramalar.cs:117-118`)
**Token groups referenced:** none

### SCR-004 — Asset Assignment (`frmDemirbasIslem`)

**Purpose:** Inference: issues a quantity of a fixed asset to a room, decrementing on-hand stock (Composite Flow — see functional-spec.md).
**Defined in:** `YazılımSınamaProjesi/frmDemirbasIslem.Designer.cs:1-228`
**Functional-spec UI Inventory line:** `functional-spec.md:134`
**Navigation in:** `frmAnaMenu.cs:66-71` (`btnOdaDemirbas_Click`)
**Navigation out:** `frmDemirbasIslem.cs:118-122` (`btnDemirbasBack_Click` → Main Menu)

**Layout structure:**
- A back button pinned to the top-left corner (`frmDemirbasIslem.Designer.cs:90-100`).
- A large bordered section labeled "Demirbaş ekle", containing: two side-by-side selection grids in its top half — rooms on the left, assets on the right (`:148-156` rooms, `:138-146` assets) — a row of two disabled echo fields for the selected room/asset name beneath the grids (`:158-172`), a labeled quantity field below that (`:72-79` label, `:102-108` field), and a save button beneath the quantity field (`:110-118`); the whole section is positioned at `:120-136`.
- A second, separate bordered section to the right of the first, labeled "Demirbaş listesi", holding one read-only grid listing everything currently assigned to the selected room (`:174-182` section, `:81-88` grid).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| (room selection) | grid/list | Room + Personnel (joined) | `frmDemirbasIslem.Designer.cs:148-156` |
| (asset selection) | grid/list | FixedAsset | `frmDemirbasIslem.Designer.cs:138-146` |
| Oda Adı (echo) | label/static text (disabled, read-only echo) | Room.OdaAdi | `frmDemirbasIslem.Designer.cs:166-172` |
| Demirbaş Adı (echo) | label/static text (disabled, read-only echo) | FixedAsset.DemirbasAdi | `frmDemirbasIslem.Designer.cs:158-164` |
| Adet | numeric input (digit-only keypress filter — DR-005) | RoomAssetAssignment.AlinanAdet | `frmDemirbasIslem.Designer.cs:102-108` |
| KAYDET | button | — | `frmDemirbasIslem.Designer.cs:110-118` |
| (assigned-to-room list) | grid/list | RoomAssetAssignment (joined) | `frmDemirbasIslem.Designer.cs:81-88` |

**States evidenced in code:** default (`frmDemirbasIslem.cs:72-79`), validation-error (empty quantity — `frmDemirbasIslem.cs:85-86`), stock-exceeded (DR-001 warning, no write — `frmDemirbasIslem.cs:90-93`), success (list refreshed after save — `frmDemirbasIslem.cs:103-108`)
**Token groups referenced:** none

### SCR-005 — Reporting & Print (`frmRapor`)

**Purpose:** Inference: shows a per-room asset-assignment report and lets the user print it.
**Defined in:** `YazılımSınamaProjesi/frmRapor.Designer.cs:1-143`
**Functional-spec UI Inventory line:** `functional-spec.md:136`
**Navigation in:** `frmAnaMenu.cs:92-97` (`button1_Click`)
**Navigation out:** `frmRapor.cs:78-82` (`btnAramalarBack_Click` → Main Menu)

**Layout structure:**
- A back button pinned to the top-left corner (`frmRapor.Designer.cs:42-54`).
- A room-selector row containing a label, a combo box, and a refresh button, left-to-right (`:83-90` label, `:75-81` combo, `:65-73` button).
- Below that row, a wide results grid (`:55-63`).
- Below the grid, a single centered print button (`:92-100`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Oda Adı (room selector) | select/combo | Room.OdaAdi | `frmRapor.Designer.cs:75-81` |
| Listele | button | — | `frmRapor.Designer.cs:65-73` |
| (report results) | grid/list | Room + Personnel + FixedAsset + RoomAssetAssignment (joined) | `frmRapor.Designer.cs:55-63` |
| Yazdır | button (renders grid to a `Bitmap`, opens the OS print dialog) | — | `frmRapor.Designer.cs:92-100` |

**States evidenced in code:** default (first room pre-selected, report pre-populated — `frmRapor.cs:66-72`), print-error (generic catch around render/print — `frmRapor.cs:100-103`; see Not Capturable)
**Token groups referenced:** none

### SCR-006 — Room Assignment (`frmOdaTanimlama`)

**Purpose:** Inference: pairs a room with its responsible staff member.
**Defined in:** `YazılımSınamaProjesi/frmOdaTanimlama.Designer.cs:1-163`
**Functional-spec UI Inventory line:** `functional-spec.md:128`
**Navigation in:** `frmAnaMenu.cs:73-78` (`btnOdaTanimlama_Click`)
**Navigation out:** `frmOdaTanimlama.cs:35-40` (`btnTanimlamaBack_Click` → Main Menu)

**Layout structure:**
- A back button pinned to the top-left corner (`frmOdaTanimlama.Designer.cs:54-65`).
- Two side-by-side selection grids below it, rooms on the left and personnel on the right, at the same vertical offset (`:105-114` rooms, `:116-125` personnel).
- Below the grids, a row of two labeled disabled echo fields (room name, room's assigned personnel), side by side (`:76-94` room-name label/field, `:86-103` personnel label/field).
- A save button beneath the echo-field row, roughly centered (`:43-52`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| (room selection) | grid/list | Room | `frmOdaTanimlama.Designer.cs:105-114` |
| (personnel selection) | grid/list | Personnel | `frmOdaTanimlama.Designer.cs:116-125` |
| Oda Adı (echo) | label/static text (disabled, read-only echo) | Room.OdaAdi | `frmOdaTanimlama.Designer.cs:67-74` |
| Oda Sorumlusu (echo) | label/static text (disabled, read-only echo) | Personnel.PersonelAdi | `frmOdaTanimlama.Designer.cs:96-103` |
| KAYDET | button | — | `frmOdaTanimlama.Designer.cs:43-52` |

**States evidenced in code:** none beyond the default view — no validation/error branch exists in code (see domain-model.md PQ-005) — `frmOdaTanimlama.cs:62-71`
**Token groups referenced:** none

### SCR-007 — Admin Panel (`frmAdmin`)

**Purpose:** Inference: router to the five admin-only maintenance screens; reachable only when the logged-in account has `YetkiID="True"` (DR-003).
**Defined in:** `YazılımSınamaProjesi/frmAdmin.Designer.cs:1-136`
**Functional-spec UI Inventory line:** `functional-spec.md:127`
**Navigation in:** `frmAnaMenu.cs:80-85` (`btnAdmin_Click`, gated by DR-003)
**Navigation out:** five edges — Stock Add (`frmAdmin.cs:26-31`), Stock Update (`:33-38`), Room Delete (`:40-45`), Room Add (`:47-52`), Room Update (`:54-59`); back edge to Main Menu (`:20-24`)

**Layout structure:**
- A back button pinned to the top-left corner (`frmAdmin.Designer.cs:39-50`).
- Two large side-by-side buttons in the upper portion for stock operations: "STOK EKLEME" on the left, "STOK GÜNCELLE" on the right (`:52-61,63-72`).
- Three smaller buttons in a row beneath them for room operations, left to right: "ODA EKLE", "ODA SİL", "ODA GÜNCELLE" (`:74-83,85-94,96-105`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| STOK EKLEME | button | — | `frmAdmin.Designer.cs:52-61` |
| STOK GÜNCELLE | button | — | `frmAdmin.Designer.cs:63-72` |
| ODA EKLE | button | — | `frmAdmin.Designer.cs:74-83` |
| ODA SİL | button | — | `frmAdmin.Designer.cs:85-94` |
| ODA GÜNCELLE | button | — | `frmAdmin.Designer.cs:96-105` |

**States evidenced in code:** none beyond the default view
**Token groups referenced:** none

### SCR-008 — Stock / Asset Add (`frmStokEkleme`)

**Purpose:** Inference: adds a new fixed-asset stock record.
**Defined in:** `YazılımSınamaProjesi/frmStokEkleme.Designer.cs:1-240`
**Functional-spec UI Inventory line:** `functional-spec.md:132`
**Navigation in:** `frmAdmin.cs:26-31` (`btnStokEkle_Click`)
**Navigation out:** `frmStokEkleme.cs:68-73` (`btnStokEklemeBack_Click` → Admin Panel)

**Layout structure:**
- A back button pinned to the top-left corner (`frmStokEkleme.Designer.cs:137-149`).
- A vertically-stacked column of four labeled fields on the left — asset name, price, purchase date, quantity — each label positioned directly left of its own field at the same row (`:50-58`/`:111-117` name, `:70-78`/`:119-126` price, `:80-88`/`:179-185` date, `:101-109`/`:128-135` quantity).
- To the right of the name/price rows, a paired asset-type id/name list (`:150-168`).
- A disabled asset-type-ID echo field beneath the lists, aligned with the quantity row (`:170-177`).
- A wide add button beneath the whole column (`:90-99`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| DEMİRBAŞ ADI | text input (no keypress filter — DR-006 Named Gap) | FixedAsset.DemirbasAdi | `frmStokEkleme.Designer.cs:111-117` |
| FİYAT | numeric input (digit/comma keypress filter — DR-005) | FixedAsset.Fiyat | `frmStokEkleme.Designer.cs:119-126` |
| ALIM TARİHİ | date picker | FixedAsset.AlimTarihi | `frmStokEkleme.Designer.cs:179-185` |
| DEMİRBAŞ TÜRÜ (selector) | select/combo (list-box pair) | AssetType.DemirbasTuruID / DemirbasTuruAdi | `frmStokEkleme.Designer.cs:150-168` |
| (asset-type ID echo) | label/static text (disabled, read-only echo) | AssetType.DemirbasTuruID | `frmStokEkleme.Designer.cs:170-177` |
| ADET | numeric input (digit/comma keypress filter — DR-005) | FixedAsset.Adet | `frmStokEkleme.Designer.cs:128-135` |
| EKLE | button | — | `frmStokEkleme.Designer.cs:90-99` |

**States evidenced in code:** default (`frmStokEkleme.cs:52-66`), validation-error (empty name/price/quantity — `frmStokEkleme.cs:79-84`), success (fields cleared — `frmStokEkleme.cs:96-100`), duplicate-error (generic catch, "Kayıtlı Demirbaş..." — `frmStokEkleme.cs:103-106`)
**Token groups referenced:** none

### SCR-009 — Stock / Asset Update (`frmStokGuncelleme`)

**Purpose:** Inference: updates an existing fixed-asset stock record, selected from a grid.
**Defined in:** `YazılımSınamaProjesi/frmStokGuncelleme.Designer.cs:1-259`
**Functional-spec UI Inventory line:** `functional-spec.md:133`
**Navigation in:** `frmAdmin.cs:33-38` (`btnStokGuncelle_Click`)
**Navigation out:** `frmStokGuncelleme.cs:114-119` (`btnStokGuncellemeBack_Click` → Admin Panel)

**Layout structure:**
- A back button pinned to the top-left corner (`frmStokGuncelleme.Designer.cs:52-63`).
- A wide selection grid spanning most of the form's width near the top (`:153-166`).
- Below the grid, a labeled purchase-date field (`:113-121` label, `:168-173` field).
- Below that, a vertically-stacked pair of labeled fields (asset name, price) on the left (`:143-151`/`:83-90` name, `:123-131`/`:74-81` price), a paired asset-type id/name list to their right (`:184-202`), and a disabled asset-type-ID echo field beneath the price field (`:175-182`).
- Below the name/price/type block, a labeled quantity field (`:92-99`/`:65-71`).
- A single wide update button spanning almost the full width at the bottom (`:102-111`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| (selection) | grid/list | FixedAsset + AssetType (joined) | `frmStokGuncelleme.Designer.cs:153-166` |
| DEMİRBAŞ ADI | text input (letter-only keypress filter — DR-006) | FixedAsset.DemirbasAdi | `frmStokGuncelleme.Designer.cs:83-90` |
| FİYAT | numeric input (digit-only keypress filter — DR-005) | FixedAsset.Fiyat | `frmStokGuncelleme.Designer.cs:74-81` |
| ALIM TARİHİ | date picker | FixedAsset.AlimTarihi | `frmStokGuncelleme.Designer.cs:168-173` |
| DEMİRBAŞ TÜRÜ (selector) | select/combo (list-box pair) | AssetType.DemirbasTuruID / DemirbasTuruAdi | `frmStokGuncelleme.Designer.cs:184-202` |
| (asset-type ID echo) | label/static text (disabled, read-only echo) | AssetType.DemirbasTuruID | `frmStokGuncelleme.Designer.cs:175-182` |
| ADET | numeric input (digit-only keypress filter — DR-005) | FixedAsset.Adet | `frmStokGuncelleme.Designer.cs:65-71` |
| GÜNCELLE | button | — | `frmStokGuncelleme.Designer.cs:102-111` |

**States evidenced in code:** default (grid populated, fields empty until a row is selected — `frmStokGuncelleme.cs:64-80`), row-selected (fields populated from selection — `frmStokGuncelleme.cs:121-129`), validation-error (empty name/price/quantity — `frmStokGuncelleme.cs:85-90`), success (`frmStokGuncelleme.cs:105-106`), error (generic catch, "Güncellerken hata oluştu..." — `frmStokGuncelleme.cs:109-112`)
**Token groups referenced:** none

### SCR-010 — Room Add (`frmOdaEkle`)

⚠ PROVISIONAL — pending PQ-008 (proposed default: treat the post-save field-clear as non-functional/defective, matching the code's actual behavior — the room-name field is never cleared)

**Purpose:** Inference: creates a new room under a department.
**Defined in:** `YazılımSınamaProjesi/frmOdaEkle.Designer.cs:1-181`
**Functional-spec UI Inventory line:** `functional-spec.md:129`
**Navigation in:** `frmAdmin.cs:47-52` (`btnOdaEkle_Click`)
**Navigation out:** `frmOdaEkle.cs:77-84` (`btnOdaEkleSilBack_Click` → Admin Panel)

**Layout structure:**
- A back button pinned to the top-left corner (`frmOdaEkle.Designer.cs:131-142`).
- A single bordered section below it labeled "ODA EKLEME", containing: a labeled room-name field near the top-left (`:46-62`), a paired department-ID/department-name list to the right of it (`:112-129`), a disabled department-ID echo field beneath the room-name field (`:93-100`), and a centered add button beneath the room-name field (`:64-73`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| ODA ADI | text input | Room.OdaAdi | `frmOdaEkle.Designer.cs:56-62` |
| DEPARTMAN (selector) | select/combo (list-box pair: ID list + name list) | Department.DepartmanID / DepartmanAdi | `frmOdaEkle.Designer.cs:112-129` |
| (department ID echo) | label/static text (disabled, read-only echo) | Department.DepartmanID | `frmOdaEkle.Designer.cs:93-100` |
| EKLE | button | — | `frmOdaEkle.Designer.cs:64-73` |

**States evidenced in code:** default (`frmOdaEkle.cs:34-48`), validation-error (empty room name — `frmOdaEkle.cs:53-54`), success (⚠ per PQ-008, `txtOdaESGodaAdi` is actually NOT cleared despite the loop at `frmOdaEkle.cs:63-66` — that loop iterates only `this.Controls`, which contains only `gbOdaEkleme`/`btnOdaEkleSilBack`, never a `TextBox` directly, since the real text field is nested inside `gbOdaEkleme` — `frmOdaEkle.cs:63-67`), duplicate-error (generic catch, "Kayıtlı Oda..." — `frmOdaEkle.cs:70-74`)
**Token groups referenced:** none

### SCR-011 — Room Delete (`frmOdaSil`)

⚠ PROVISIONAL — pending PQ-004 (proposed default: preserve name-keyed DELETE as-is for baseline fidelity — see domain-model.md)

**Purpose:** Inference: deletes an existing room, selected by its name, with no confirmation step.
**Defined in:** `YazılımSınamaProjesi/frmOdaSil.Designer.cs:1-123`
**Functional-spec UI Inventory line:** `functional-spec.md:131`
**Navigation in:** `frmAdmin.cs:40-45` (`btnOdaSil_Click`)
**Navigation out:** `frmOdaSil.cs:49-54` (`btnOdaEkleSilBack_Click` → Admin Panel)

**Layout structure:**
- A back button pinned to the top-left corner (`frmOdaSil.Designer.cs:83-94`).
- A single bordered section below it labeled "ODA SİLME", containing a labeled room selector and a delete button in a single row (`:39-47` label, `:74-81` selector, `:49-58` button).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| ODA ADI (room selector) | select/combo | Room.OdaAdi (used as the match key — see PQ-004) | `frmOdaSil.Designer.cs:74-81` |
| SİL | button (destructive, no confirmation dialog) | — | `frmOdaSil.Designer.cs:49-58` |

**States evidenced in code:** default (`frmOdaSil.cs:55-58`), success (message shown, selector cleared and re-populated — `frmOdaSil.cs:67-70`)
**Token groups referenced:** none

### SCR-012 — Room Update (`frmOdaGuncelle`)

⚠ PROVISIONAL — pending PQ-004 (same as SCR-011 — proposed default: preserve name-keyed UPDATE as-is for baseline fidelity)

**Purpose:** Inference: renames an existing room, selected by its current name.
**Defined in:** `YazılımSınamaProjesi/frmOdaGuncelle.Designer.cs:1-147`
**Functional-spec UI Inventory line:** `functional-spec.md:130`
**Navigation in:** `frmAdmin.cs:54-59` (`btnOdaGuncelle_Click`)
**Navigation out:** `frmOdaGuncelle.cs:79-84` (`btnOdaEkleSilBack_Click` → Admin Panel)

**Layout structure:**
- A back button pinned to the top-left corner (`frmOdaGuncelle.Designer.cs:105-116`).
- A single bordered section below it labeled "ODA GÜNCELLEME", containing two labeled fields side by side — an existing-room selector on the left, a new-name field on the right (`:59-67` selector label, `:96-103` selector, `:49-57` new-name label, `:41-47` new-name field) — and an update button beneath the new-name field, right-aligned (`:69-78`).

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| ODA ADI (existing room selector) | select/combo | Room.OdaAdi (used as the match key — see PQ-004) | `frmOdaGuncelle.Designer.cs:96-103` |
| YENİ ODA ADI (new name) | text input | Room.OdaAdi | `frmOdaGuncelle.Designer.cs:41-47` |
| GÜNCELLE | button | — | `frmOdaGuncelle.Designer.cs:69-78` |

**States evidenced in code:** default (combo populated from `tblOda` — `frmOdaGuncelle.cs:35-54`), success (message shown, both fields cleared, combo re-populated — `frmOdaGuncelle.cs:67-71`), error (generic catch, "Hatalı İşlem..." — `frmOdaGuncelle.cs:73-76`)
**Token groups referenced:** none

## Widget Cross-Reference Findings

1. **Domain-model field with no widget on any screen:** `User.YetkiID` (`tblKullanicilar`) — read at `frmAnaMenu.cs:47-53` purely as an internal string comparison (`if(yetki=="True") btnAdmin.Enabled = true; ...`) that toggles the `btnAdmin` button's `Enabled` property (SCR-002). No screen in scope ever displays this value in a control or lets a user edit it directly — it exists only as query-result logic, never a bound field. (Domain-model.md's own "User" entity Named Gap already notes no screen manages `tblKullanicilar` rows at all; this finding is the widget-side confirmation of that same gap.)

No widget-with-no-domain-field mismatches were found: every non-decorative, non-action-trigger widget across all 12 screens binds to a field, or a selector over a field, already documented in `domain-model.md`'s Entities section.

## Named Gaps

1. **Screen-count discrepancy in `functional-spec.md`.** Its own `## UI Inventory` header prose at `functional-spec.md:121` states "All eleven form entries below were fully parsed," but its own table (`functional-spec.md:123-136`) lists twelve rows, and this run's own direct `Read` of every `frm*.Designer.cs`/`frm*.cs` pair under `YazılımSınamaProjesi/` (via `Glob` — 12 `Designer.cs` files, 12 matching `.cs` files) confirms twelve distinct screens (SCR-001 through SCR-012 above). The "eleven" figure appears to be an internal miscount in that document's prose, not a difference in what's actually in scope — its own table is correct.
2. **`frmAdminsilinecek.resx`** (listed in the extension histogram's `.resx` samples) has no corresponding `.cs`/`.Designer.cs` file anywhere in scope. It is not treated as a screen here, consistent with `domain-model.md`'s own Named Gap on this orphaned resource.
3. **Room Add's post-save field-clear loop is a no-op.** `frmOdaEkle.cs:63-66` iterates `this.Controls` looking for `TextBox` instances to clear, but `frmOdaEkle.Designer.cs:154-155` shows only `gbOdaEkleme` (a `GroupBox`) and `btnOdaEkleSilBack` are added directly to `this.Controls` — the real text field, `txtOdaESGodaAdi`, is nested inside `gbOdaEkleme` (`frmOdaEkle.Designer.cs:81`) and is therefore never visited by the loop. `functional-spec.md:26` describes this behavior as "clears every `TextBox` child control," which the code does not actually do. See PQ-008.
4. **`frmDemirbasIslem`'s `AutoScaleDimensions` differs from every sibling screen.** `frmDemirbasIslem.Designer.cs:190` declares `new System.Drawing.SizeF(6F, 13F)`, while all other 11 forms declare `(8F, 16F)` (e.g. `frmGiris.Designer.cs:107`, `frmAnaMenu.Designer.cs:95`). These two values correspond to different legacy WinForms default-font design-time metrics, meaning this one screen's absolute pixel coordinates were authored under a different font-scaling assumption than its siblings. Which font actually renders at runtime — and therefore how this screen's proportions actually resolve relative to the other 11 — could not be determined from source. See PQ-009.
5. **No `Font` property is ever set on any of the 12 forms or their controls.** A direct search across every `.cs`/`.Designer.cs` file in `YazılımSınamaProjesi/` for `Font =`/`.Font` assignments (beyond the `AutoScaleMode`/`AutoScaleDimensions` scaling metadata already discussed) returned no matches — typography is entirely OS/.NET-default. See PQ-009; the corresponding typography tokens are recorded in `design-tokens.json`'s `omitted[]`, not `token_groups[]`.
6. **The three `SystemColors.*` values used on the Login screen are OS-theme-relative symbols, not fixed values.** `frmGiris.Designer.cs:45,46,109` declare `SystemColors.Highlight`, `SystemColors.ControlText`, and `SystemColors.Control` respectively — these are grounded as *declared symbolic references*, but the actual rendered RGB each one resolves to depends on the end user's Windows theme and cannot be determined from source. Recorded in `design-tokens.json` (TK-001) as the symbolic value only, per the corresponding note on each token.
7. **No spacing/sizing scale is evidenced anywhere in the codebase.** The recurring `Margin = new System.Windows.Forms.Padding(4)` seen on many controls (e.g. `frmGiris.Designer.cs:48,59,71,89,99`) is the Visual Studio WinForms designer's own default control margin, applied mechanically to nearly every control across all 12 forms — it is not evidence of an intentional, named spacing token, so `design-tokens.json` contains no spacing group.
