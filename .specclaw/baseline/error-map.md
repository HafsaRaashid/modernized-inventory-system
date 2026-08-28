# Error Map: Fixed Asset & Inventory Tracking System (YazılımSınamaProjesi / DemirbasTakip)

**Date created:** 2026-08-24
**Grounded in:** the legacy application's own source -- every entry cites the line that
raises the condition it names.

<!--
  THIS FILE IS PER-PROJECT DATA. It lives in the target repo at
  .specclaw/baseline/error-map.md and belongs to this project alone. Codes are permanent
  once assigned; a new condition gets a new code appended, an existing entry is only ever
  amended to fill in its Rebuild source or correct a citation. See CONTRACT.md (h).
-->

## Codes

### INVALID_LOGIN_CREDENTIALS

- **Condition:** the supplied username+password does not match any `tblKullanicilar` row
  (the `SELECT COUNT(*) ...` query returns 0).
- **Legacy source:** frmGiris.cs:43-53
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** no exception -- an in-band `if`/`else` branch;
  `MessageBox.Show("Hatalı giriş yaptınız.Lütfen tekrar giriniz!!!")` at frmGiris.cs:53.
- **Pinned by:** GM-012, GM-013

### ROOM_NAME_REQUIRED

- **Condition:** the room name field was left blank on Room Add.
- **Legacy source:** frmOdaEkle.cs:53
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** no exception -- an in-band `if`/`else` branch;
  `errorProvider1.SetError(txtOdaESGodaAdi, "Boş geçilmez")`.
- **Pinned by:** GM-020

### DEPARTMENT_NOT_SELECTED

- **Condition:** no department was selected on Room Add -- `txtDepartmanID.Text` is empty,
  which SQL Server cannot implicitly convert to `tblOda.DepartmanID`'s integer column,
  causing the INSERT to fail.
- **Legacy source:** frmOdaEkle.cs:58-60 (`komut.Parameters.AddWithValue("@ID",
  txtDepartmanID.Text)` with an empty string)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** a `System.Data.SqlClient.SqlException` (implicit
  varchar-to-int conversion failure), caught by frmOdaEkle.cs:70-74's bare `catch` --
  which shows "Kayıtlı Oda..." ("already registered"), a message that has nothing to do
  with the real cause. Scoped confidently to this one condition because this scenario's
  own arrange step is what deliberately produces it (see scenarios.md GM-022 and
  Tests/RoomAddTests.cs); the catch itself is shared and would swallow other SqlExceptions
  too, which is why this code is not assigned reflexively to every exception this catch
  could ever see.
- **Pinned by:** GM-022

### QUANTITY_EXCEEDS_STOCK

- **Condition:** the requested assignment quantity exceeds the asset's available stock
  (DR-001's guard).
- **Legacy source:** frmDemirbasIslem.cs:90 (`if (Alinanadet > stok)`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** no exception -- an in-band `if`/`else` branch;
  `MessageBox.Show("Girilen değer stok miktarından fazla.Daha az bir değer giriniz...")`
  at frmDemirbasIslem.cs:92.
- **Pinned by:** GM-034

### QUANTITY_REQUIRED

- **Condition:** the quantity field was left blank on Asset Assignment.
- **Legacy source:** frmDemirbasIslem.cs:85
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** no exception -- an in-band `if`/`else` branch;
  `errorProvider1.SetError(txtDIAdet, "Boş geçilmez")`.
- **Pinned by:** GM-038

### QUANTITY_NOT_NUMERIC

- **Condition:** the quantity text could not be parsed as an integer (a non-numeric value
  reached this handler by bypassing the KeyPress filter -- e.g. set directly rather than
  typed).
- **Legacy source:** frmDemirbasIslem.cs:89 (`Alinanadet = int.Parse(txtDIAdet.Text);`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** `System.FormatException`, caught by the outer catch-all at
  frmDemirbasIslem.cs:112-115 (shows "Hatalı İşlem Yaptınız.Tekrar deneyiniz..."). Scoped
  confidently to this one condition for the same reason as `DEPARTMENT_NOT_SELECTED`
  above: this scenario's own arrange step (a non-numeric quantity) is what deliberately
  produces it, and the exception's own observed `ExceptionType` (recorded as evidence per
  CONTRACT.md (b.2)) confirms it really is a `FormatException`, not some other condition
  this same bare `catch` could also swallow.
- **Pinned by:** GM-039

### SEARCH_TERM_REQUIRED

- **Condition:** the asset search term was left blank.
- **Legacy source:** frmAramalar.cs:117
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** no exception -- an in-band `if`/`else` branch;
  `errorProvider1.SetError(txtAramalarBilgiGiriniz, "Boş geçilmez")`.
- **Pinned by:** GM-041

### SEARCH_TEXT_CONTAINS_QUOTE

- **Condition:** personnel-search text containing a single quote breaks the concatenated
  SQL statement.
- **Legacy source:** frmAramalar.cs:92
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** `System.Data.SqlClient.SqlException` ("Incorrect syntax near
  ..."), caught by frmAramalar.cs:150-153's bare `catch` (shows "Hatalı İşlem!!"). Per
  CQ-010 (clarifications.md), this SQL-injection-vulnerable concatenation pattern is a
  confirmed DEFECT the rebuild will fix -- this code captures the legacy AS-IS behaviour
  for baseline parity; expect GM-045's fixture to legitimately DIVERGE at replay once the
  rebuild's parameterized query is in place.
- **Pinned by:** GM-045

### ROOM_NAME_CONTAINS_QUOTE

- **Condition:** a room name containing a single quote breaks the concatenated SQL
  statement in the Reporting query. This is the same systemic concatenation defect as
  `SEARCH_TEXT_CONTAINS_QUOTE` above (CQ-010), manifesting at a second, independent call
  site -- kept as its own code rather than merged into that one, since each code's
  "Legacy source" is a single citation and these are two distinct lines in two distinct
  methods.
- **Legacy source:** frmRapor.cs:59
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** `System.Data.SqlClient.SqlException`, **unhandled** --
  `RaporDoldur()` (frmRapor.cs:55-64) has no `try`/`catch` at all, the only read method
  among the eleven forms with none.
- **Pinned by:** GM-046

## Unmapped Conditions

1. **Room Delete when the room still has an associated `tblOdaDemirbasAtama` row**
   (GM-029) -- blocked by **PQ-010** (`.specclaw/analysis/pending-questions.md`, OPEN).
   Whether an FK constraint exists (blocking the delete with an unhandled `SqlException`,
   since `frmOdaSil.cs` has no `try`/`catch` at all) or does not (the delete succeeds and
   the child row's `OdaID` is silently orphaned) could not be confirmed from source alone
   -- no DDL/schema-dump file was in scope for this analysis. `error_code` is left `null`
   on this fixture regardless of which branch is actually observed when the harness runs;
   `scenarios.md`'s GM-029 already carries the `⚠ PROVISIONAL` marker authorizing this.

2. **`GuncelleAdet()` called directly with a quantity that would drive
   `tblDemirbas.Adet` negative** (GM-040), **only if it throws** -- blocked by **PQ-013**
   (raised this run). Whether `tblDemirbas.Adet` has a `CHECK` constraint preventing a
   negative value could not be confirmed or ruled out from source alone (same DDL gap as
   PQ-010). Unlike GM-029, `scenarios.md`'s GM-040 does **not** carry a `⚠ PROVISIONAL`
   marker, so a `REJECTED` fixture with a `null` code here would fail `record`'s own
   mechanical check (CONTRACT.md (h).2). `Tests/AssetAssignmentTests.cs` therefore reports
   `Inconclusive` instead of writing a fixture in this branch, and does write a normal
   `outcome: "OK"` fixture in the (expected, more likely) branch where no exception
   occurs. See PQ-013 for the follow-up this reveals.
