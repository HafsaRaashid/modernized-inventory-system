# Screenshot Capture Checklist: Fixed Asset & Inventory Tracking System (YazılımSınamaProjesi / DemirbasTakip)

**Path analyzed:** C:\Users\MohamedRaashidBISTEC\OneDrive - BISTEC Global\Documents\specclaw project\InventoryTrackingSystem\InventoryTrackingSystem
**Date generated:** 2026-08-18
**Legacy commit at design time:** 66e5eb5
**Screens to capture:** 12 · **Rows (screen × state):** 33

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose — filling this template is a dumb
  global string replace and would corrupt the comment.

  THIS IS A HUMAN WORK ORDER. /specclaw:bf-ui designs it; a human runs the
  legacy application and captures the screenshots. No specclaw command
  ever runs the legacy app, ever takes a screenshot, ever simulates one,
  and ever writes, edits, moves, or deletes anything under screens/.

  HOW TO USE IT:
    1. Run the legacy application yourself.
    2. For each row below, reach the named screen in the named state
       (the Setup notes tell you how, with a citation to the code that
       evidences that state), and save a PNG at the exact Target file
       path shown.
    3. Run `/specclaw:bf-ui --record`. It hashes every PNG it finds,
       validates the filenames against this checklist, and writes
       .specclaw/ui/ui-manifest.json.

  FILENAME CONVENTION (validated mechanically by --record):
    screens/SCR-###.png              for the "default" state
    screens/SCR-###-<state>.png      for every other state
-->

## Capture Checklist

| SCR | Screen | State | Target file | Setup notes | Captured? |
|---|---|---|---|---|---|
| SCR-001 | Login | default | screens/SCR-001.png | Launch the application. Both fields show placeholder text (`"KULLANICI ADI"`/`"ŞİFRE"`) — `frmGiris.Designer.cs:64,77`. | ☐ |
| SCR-001 | Login | validation-error | screens/SCR-001-validation-error.png | Click into the username or password field, then click/tab away leaving it empty (or leave it at placeholder text) to trigger `errorProvider1` — `frmGiris.cs:83-93,95-105`. | ☐ |
| SCR-001 | Login | login-failure | screens/SCR-001-login-failure.png | Enter a wrong username/password and click GİRİŞ — `frmGiris.cs:51-58`. | ☐ |
| SCR-002 | Main Menu | admin-disabled | screens/SCR-002.png | Log in as an account whose `tblKullanicilar.YetkiID` value is not the literal string `"True"` — `frmAnaMenu.cs:50-53`. | ☐ |
| SCR-002 | Main Menu | admin-enabled | screens/SCR-002-admin-enabled.png | Log in as an account whose `tblKullanicilar.YetkiID` value is exactly `"True"` — `frmAnaMenu.cs:48-49`. Requires two seeded accounts (see Setup Prerequisites). | ☐ |
| SCR-003 | Search | default | screens/SCR-003.png | Navigate Main Menu → ARAMALAR. "Demirbaş Adı" criterion is pre-checked, free-text box visible, date picker hidden — `frmAramalar.Designer.cs:222-223`, `frmAramalar.cs:77`. | ☐ |
| SCR-003 | Search | date-criterion | screens/SCR-003-date-criterion.png | Click the "Alım Tarihi" radio button — the free-text box is replaced by a date picker in the same slot — `frmAramalar.cs:70-74,156-160`. | ☐ |
| SCR-003 | Search | validation-error | screens/SCR-003-validation-error.png | Leave "Bilgi Giriniz" empty and click the asset ARAMA button — `frmAramalar.cs:117-118`. | ☐ |
| SCR-004 | Asset Assignment | default | screens/SCR-004.png | Navigate Main Menu → ODA DEMİRBAŞ İŞLEMLERİ, before selecting a room/asset row — `frmDemirbasIslem.cs:72-79`. | ☐ |
| SCR-004 | Asset Assignment | validation-error | screens/SCR-004-validation-error.png | Select a room and an asset row, leave Adet empty, click KAYDET — `frmDemirbasIslem.cs:85-86`. | ☐ |
| SCR-004 | Asset Assignment | stock-exceeded | screens/SCR-004-stock-exceeded.png | Select an asset with a known on-hand quantity, enter a larger quantity in Adet, click KAYDET — `frmDemirbasIslem.cs:90-93`. | ☐ |
| SCR-004 | Asset Assignment | success | screens/SCR-004-success.png | Select a room and an asset, enter a valid quantity within stock, click KAYDET — `frmDemirbasIslem.cs:96-108`. | ☐ |
| SCR-005 | Report | default | screens/SCR-005.png | Navigate Main Menu → Rapor Çıktısı Al. First room is pre-selected and the grid is pre-populated — `frmRapor.cs:66-72`. | ☐ |
| SCR-006 | Room Assignment | default | screens/SCR-006.png | Navigate Main Menu → ODA TANIMLAMA — `frmOdaTanimlama.cs:42-59`. | ☐ |
| SCR-007 | Admin Panel | default | screens/SCR-007.png | Navigate Main Menu → ADMİN (requires an admin-enabled account per SCR-002) — `frmAnaMenu.cs:80-85`. | ☐ |
| SCR-008 | Stock Add | default | screens/SCR-008.png | Navigate Admin Panel → STOK EKLEME — `frmStokEkleme.cs:52-66`. | ☐ |
| SCR-008 | Stock Add | validation-error | screens/SCR-008-validation-error.png | Leave DEMİRBAŞ ADI, FİYAT, or ADET empty and click EKLE — `frmStokEkleme.cs:79-84`. | ☐ |
| SCR-008 | Stock Add | success | screens/SCR-008-success.png | Fill all fields with valid values and click EKLE — `frmStokEkleme.cs:96-100`. | ☐ |
| SCR-008 | Stock Add | duplicate-error | screens/SCR-008-duplicate-error.png | Attempt to add an asset that triggers the database's own `catch` (e.g. re-submit a name already inserted, if a uniqueness constraint exists — see functional-spec.md Named Gap 9); the exact trigger condition is not fully confirmed from app code alone — `frmStokEkleme.cs:103-106`. | ☐ |
| SCR-009 | Stock Update | default | screens/SCR-009.png | Navigate Admin Panel → STOK GÜNCELLE, before selecting a grid row — `frmStokGuncelleme.cs:64-80`. | ☐ |
| SCR-009 | Stock Update | row-selected | screens/SCR-009-row-selected.png | Click a row in the results grid — fields populate from the selection — `frmStokGuncelleme.cs:121-129`. | ☐ |
| SCR-009 | Stock Update | validation-error | screens/SCR-009-validation-error.png | With a row selected, clear DEMİRBAŞ ADI, FİYAT, or ADET and click GÜNCELLE — `frmStokGuncelleme.cs:85-90`. | ☐ |
| SCR-009 | Stock Update | success | screens/SCR-009-success.png | With a row selected and all required fields filled, click GÜNCELLE — `frmStokGuncelleme.cs:105-106`. | ☐ |
| SCR-009 | Stock Update | error | screens/SCR-009-error.png | With a row selected, leave the asset-type list unselected (so the type-ID echo field stays empty) before clicking GÜNCELLE, to attempt an update with a blank foreign key — `frmStokGuncelleme.cs:109-112`. | ☐ |
| SCR-010 | Room Add | default | screens/SCR-010.png | Navigate Admin Panel → ODA EKLE — `frmOdaEkle.cs:34-48`. | ☐ |
| SCR-010 | Room Add | validation-error | screens/SCR-010-validation-error.png | Leave ODA ADI empty and click EKLE — `frmOdaEkle.cs:53-54`. | ☐ |
| SCR-010 | Room Add | success | screens/SCR-010-success.png | Fill ODA ADI, select a department, click EKLE — note the room-name field is expected NOT to visibly clear despite the success message (see PQ-008) — `frmOdaEkle.cs:63-67`. | ☐ |
| SCR-010 | Room Add | duplicate-error | screens/SCR-010-duplicate-error.png | Attempt to add a room name that triggers the database's own `catch` (see functional-spec.md Named Gap 9 on the unconfirmed uniqueness constraint) — `frmOdaEkle.cs:70-74`. | ☐ |
| SCR-011 | Room Delete | default | screens/SCR-011.png | Navigate Admin Panel → ODA SİL — `frmOdaSil.cs:55-58`. | ☐ |
| SCR-011 | Room Delete | success | screens/SCR-011-success.png | Select a room from the selector and click SİL (no confirmation dialog appears) — `frmOdaSil.cs:59-70`. | ☐ |
| SCR-012 | Room Update | default | screens/SCR-012.png | Navigate Admin Panel → ODA GÜNCELLE — `frmOdaGuncelle.cs:35-54`. | ☐ |
| SCR-012 | Room Update | success | screens/SCR-012-success.png | Select an existing room, enter a new name, click GÜNCELLE — `frmOdaGuncelle.cs:67-71`. | ☐ |
| SCR-012 | Room Update | error | screens/SCR-012-error.png | Attempt an update that triggers the generic `catch` (e.g. select no existing room before clicking GÜNCELLE) — `frmOdaGuncelle.cs:73-76`. | ☐ |

## Setup Prerequisites

- A running SQL Server instance reachable at the hardcoded connection string used identically across every data-access form (`server=localhost,1433;Initial Catalog=DemirbasTakip;User Id=sa;Password=DemirbasDev!2026;TrustServerCertificate=True` — e.g. `frmGiris.cs:25`), with the `DemirbasTakip` database and its tables (`tblKullanicilar`, `tblOda`, `tblDepartmanlar`, `tblPersonel`, `tblDemirbas`, `tblDemirbasTurleri`, `tblOdaDemirbasAtama`) populated with at least a few rows each.
- **Two seeded `tblKullanicilar` accounts** are required to capture both SCR-002 states: one row with `YetkiID` exactly equal to the string `"True"`, and one row with any other value — `frmAnaMenu.cs:42-53`.
- At least one room with an existing responsible-personnel pairing already inserted via `frmOdaTanimlama` (so SCR-004's default view and SCR-005/SCR-003 result grids render non-empty rows rather than an empty grid).
- At least one `tblDemirbas` row with a known, non-zero `Adet` value, to reliably trigger SCR-004's stock-exceeded state by entering a quantity larger than that value.
- Window size/DPI is not evidenced as fixed in code beyond each form's own `ClientSize` (e.g. `frmGiris.Designer.cs:110`, `645×567`) — capture each screen at its own default (un-resized) launch size.

## Not Capturable

- **SCR-005 print-error** — `frmRapor.cs:89-104`'s `catch` wraps both the `Bitmap` render (`DrawToBitmap`) and the OS print dialog invocation (`PpdDialog.ShowDialog()`); no code path in scope deterministically triggers a failure here from ordinary UI interaction. Reproducing it reliably would require environment-level interference (e.g., no printer subsystem available) rather than a normal capture step, so it is not issued as a row above.
