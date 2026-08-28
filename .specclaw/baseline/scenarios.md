# Baseline Scenarios: Fixed Asset & Inventory Tracking System (YazılımSınamaProjesi / DemirbasTakip)

**Date generated:** 2026-08-24
**Grounded in:** .specclaw/analysis/domain-model.md's numbered Business Rules, codebase-report.md, architecture.md, functional-spec.md, module-map.md, clarifications.md, pending-questions.md, and direct `Read` of every `.cs`/`.Designer.cs` file this run.

<!--
  Module tagging note (read before the scenario list): module-map.md's own
  "Business rules:" prose and "## Unassigned" section state that DR-004,
  DR-005, and DR-006 are cross-cutting and owned by NO single module. The
  collected module_map JSON facts this run were handed, however, carry
  DR-004 under MOD-002's rules[] and DR-006 under MOD-004's rules[] (DR-005
  appears in neither). Per this agent's instructions, the JSON's
  module_map.modules[].rules[] is the mechanical ownership index scenario
  Modules tags are derived from — never re-derived from a module's own
  prose or from "which screen exercises it." I have followed the JSON
  exactly (DR-004 -> MOD-002, DR-006 -> MOD-004, DR-005 -> no module), even
  though this reads oddly for, e.g., a DR-004 scenario captured on the
  Login screen (MOD-001) or the Asset Assignment screen (MOD-003) being
  tagged MOD-002. This conflict between the JSON index and module-map.md's
  own "Unassigned" section is worth a human's attention when module-map.md
  moves from PROPOSED to confirmed — flagged in this run's final response,
  not resolved here.

  Second consequence of the same mechanism: a scenario that pins NO
  numbered DR-### rule (most of Room CRUD, Room Assignment, Login's core
  credential match, Search's core filtering, Reporting's core query) has
  NO module it can mechanically derive a tag from, and so carries no
  Modules field at all -- not because it doesn't belong to a module in
  spirit, but because module tagging in this framework rides strictly on
  the DR-### rule chain (CONTRACT.md (l): MOD -> BL -> DR -> GM), and this
  legacy app's numbered business rules are concentrated in MOD-001 and
  MOD-003. This means a `--module MOD-002` or `--module MOD-005` replay
  run will select very few of this document's scenarios even though many
  scenarios exercise those modules' screens -- a genuine, evidenced
  property of this module map as currently proposed, not an omission.
-->

## Scenarios

### GM-001 — FiyatDogruMu classifies an all-digit string as valid (1)

- **Seam:** Test1.FiyatDogruMu(string s)
- **Seam layer:** pure-function
- **Business rules pinned:** rule 7 (DR-007)
- **Arrange:** none — pure function, no fixture setup required.
- **Act:** call `new Test1().FiyatDogruMu("120")`.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `classification: 1`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-002 — FiyatDogruMu classifies a single space as the documented edge case (0)

- **Seam:** Test1.FiyatDogruMu(string s)
- **Seam layer:** pure-function
- **Business rules pinned:** rule 7 (DR-007)
- **Arrange:** none.
- **Act:** call `new Test1().FiyatDogruMu(" ")` (exactly one space character).
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `classification: 0`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-003 — FiyatDogruMu classifies a non-numeric, non-space string as invalid (2)

- **Seam:** Test1.FiyatDogruMu(string s)
- **Seam layer:** pure-function
- **Business rules pinned:** rule 7 (DR-007)
- **Arrange:** none.
- **Act:** call `new Test1().FiyatDogruMu("asdasd")` (matches `UnitTest1.IsInvalid`'s own fixture value, for continuity with the one existing legacy test).
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `classification: 2`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-004 — FiyatDogruMu classifies an empty string as valid (1) — undocumented edge case

- **Seam:** Test1.FiyatDogruMu(string s)
- **Seam layer:** pure-function
- **Business rules pinned:** rule 7 (DR-007)
- **Arrange:** none.
- **Act:** call `new Test1().FiyatDogruMu("")`.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `classification: 1`. Note in the fixture/harness comment why: `IsNumeric("")`'s `foreach` never executes over zero characters, so it returns `true` vacuously — this is not exercised by `UnitTestProject1/UnitTest1.cs`'s three existing tests and is a genuine, previously-unrecorded behavior of this method.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-005 — Numeric keypress filter (Asset Assignment quantity field) accepts digits/comma/backspace, rejects letters

- **Seam:** frmDemirbasIslem.txtDIAdet_KeyPress
- **Seam layer:** pure-function
- **Business rules pinned:** rule 5 (DR-005)
- **Arrange:** instantiate `frmDemirbasIslem` headlessly (no `.Show()`); locate the private `txtDIAdet_KeyPress` method via reflection.
- **Act:** invoke the handler once per test character: `'5'` (digit), `','` (comma, char 44), `(char)8` (backspace), `'a'` (letter) — each with a fresh `KeyPressEventArgs`.
- **Assert (shape):** per-character array `[{char: "5", handled: false}, {char: ",", handled: false}, {char: "\b", handled: false}, {char: "a", handled: true}]`. For the `'a'` case only, the harness must also suppress/observe the `MessageBox.Show("Sadece Sayı Girişi...")` call per seams.md's capture caveat — record whether it fired as a boolean, not by letting it block.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-006 — Numeric keypress filter (Stock Add) — same boundary, independently duplicated code

- **Seam:** frmStokEkleme.SayiGirisiKontrol
- **Seam layer:** pure-function
- **Business rules pinned:** rule 5 (DR-005)
- **Arrange:** instantiate `frmStokEkleme` headlessly; the method is `public`, callable directly.
- **Act:** invoke `SayiGirisiKontrol(e)` for `'7'`, `','`, `(char)8`, `'z'`.
- **Assert (shape):** same per-character array shape as GM-005.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-007 — Numeric keypress filter (Stock Update) — same boundary, independently duplicated code

- **Seam:** frmStokGuncelleme.SayiGirisiKontrol
- **Seam layer:** pure-function
- **Business rules pinned:** rule 5 (DR-005)
- **Arrange:** instantiate `frmStokGuncelleme` headlessly.
- **Act:** invoke `SayiGirisiKontrol(e)` for `'3'`, `','`, `(char)8`, `'Q'`.
- **Assert (shape):** same per-character array shape as GM-005.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-008 — Letter keypress filter (Stock Update asset-name field) accepts letters/comma/backspace, rejects digits

- **Seam:** frmStokGuncelleme.HarfGirisiKontrol
- **Seam layer:** pure-function
- **Modules:** MOD-004
- **Business rules pinned:** rule 6 (DR-006)
- **Arrange:** instantiate `frmStokGuncelleme` headlessly.
- **Act:** invoke `HarfGirisiKontrol(e)` for `'A'`, `','`, `(char)8`, `'9'`.
- **Assert (shape):** per-character array `[{char:"A",handled:false},{char:",",handled:false},{char:"\b",handled:false},{char:"9",handled:true}]`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-009 — Letter keypress filter (Search personnel-name fields) — same boundary

- **Seam:** frmAramalar.HarfGirisiKontrol
- **Seam layer:** pure-function
- **Modules:** MOD-004
- **Business rules pinned:** rule 6 (DR-006)
- **Arrange:** instantiate `frmAramalar` headlessly.
- **Act:** invoke `HarfGirisiKontrol(e)` for `'z'`, `','`, `(char)8`, `'1'`.
- **Assert (shape):** same per-character array shape as GM-008.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-010 — Stock Add's own letter filter classifies correctly when called directly, despite being unwired (Named Gap)

- **Seam:** frmStokEkleme.HarfGirisiKontrol
- **Seam layer:** pure-function
- **Modules:** MOD-004
- **Business rules pinned:** rule 6 (DR-006)
- **Arrange:** instantiate `frmStokEkleme` headlessly.
- **Act:** invoke `HarfGirisiKontrol(e)` directly for `'B'` and `'4'` (the method is `public` and fully functional even though no `KeyPress` handler in this form ever calls it — functional-spec.md Named Gap 6, CQ-015).
- **Assert (shape):** `[{char:"B",handled:false},{char:"4",handled:true}]`. The scenario's own note must record that `txtSEdemirbasAdi` (Stock Add's asset-name field) has **no** `KeyPress` handler wired to this method at all in the actual form — this scenario proves the method's own logic parity with its siblings, not that Stock Add's name field is filtered (it is not).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-011 — Login succeeds with matching username and password

- **Seam:** GİRİŞ_EKRANI.button1_Click (Login credential match)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (functional-spec.md's "Login" workflow)
- **Arrange:** insert one `tblKullanicilar` row (`KullaniciAdi='testuser'`, `Sifre='testpass'`, any `YetkiID`). Set `txtUsername.Text`/`txtPassword.Text` to the same values via reflection.
- **Act:** invoke the private `button1_Click` handler via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`; `GİRİŞ_EKRANI.kAdi`/`.sifre` (public static fields) equal the arranged credentials; a `frmAnaMenu` instance was constructed (navigation occurred).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-012 — Login fails with non-matching credentials

- **Seam:** GİRİŞ_EKRANI.button1_Click (Login credential match)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert one `tblKullanicilar` row with a *different* `KullaniciAdi`/`Sifre` than what will be entered. Set `txtUsername.Text`/`txtPassword.Text` to a non-matching pair.
- **Act:** invoke `button1_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null` (semantic condition: "credentials did not match any account" — no exception is raised on this path, the rejection is a normal `if`/`else` branch); `txtUsername.Text`/`txtPassword.Text` reset to the placeholder strings `"KULLANICI ADI"`/`"ŞİFRE"`; no `frmAnaMenu` constructed.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-013 — Login with both fields empty is not gated before the query runs

- **Seam:** GİRİŞ_EKRANI.button1_Click (Login credential match)
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** arrange the DB with no `tblKullanicilar` row whose `KullaniciAdi`/`Sifre` are both empty strings (a safe assumption for any real account). Set `txtUsername.Text = ""`, `txtPassword.Text = ""` via reflection.
- **Act:** invoke `button1_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null` — the query executes exactly as for any other non-matching input, because `button1_Click` (`frmGiris.cs:35-66`) contains **no** `Trim() != ""` re-check anywhere in its body (confirmed by reading the whole method) — unlike every other DR-004 screen in this codebase. Record `code_path_note: "no redundant non-empty gate exists in this handler; ErrorProvider is a fully separate mechanism, see GM-014"` as part of the fixture's own documentation, not as a comparable output field.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-014 — Login ErrorProvider fires independently of the actual login attempt

- **Seam:** GİRİŞ_EKRANI.txtUsername_Validated / txtPassword_Validated
- **Seam layer:** pure-function
- **Modules:** MOD-002
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** instantiate `GİRİŞ_EKRANI` headlessly; set `txtUsername.Text = ""`.
- **Act:** invoke the private `txtUsername_Validated` handler via reflection (simulating the control losing focus while empty).
- **Assert (shape):** `errorProvider1.GetError(txtUsername)` (read via reflection) equals `"Lütfen Kullanıcı Adınızı Giriniz"`. No DB call occurs at all in this seam — confirm no `SqlConnection` is opened.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-015 — Admin gate enables the Admin button on an exact "True" match

- **Seam:** frmAnaMenu.ANA_MENÜ_Load (Admin authorization gate)
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 3 (DR-003)
- **Arrange:** insert exactly one `tblKullanicilar` row (`KullaniciAdi='admin1'`, `Sifre='pw1'`, `YetkiID='True'`). Set `GİRİŞ_EKRANI.kAdi = "admin1"`, `GİRİŞ_EKRANI.sifre = "pw1"` (public static fields, no reflection needed).
- **Act:** construct `frmAnaMenu(new Form())` headlessly, then invoke the private `ANA_MENÜ_Load` handler via reflection.
- **Assert (shape):** `btnAdmin.Enabled == true` (read via reflection since the control is private).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-016 — Admin gate disables the Admin button when YetkiID is "False"

- **Seam:** frmAnaMenu.ANA_MENÜ_Load
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 3 (DR-003)
- **Arrange:** insert exactly one `tblKullanicilar` row with `YetkiID='False'`. Set matching static `kAdi`/`sifre`.
- **Act:** construct `frmAnaMenu(new Form())`, invoke `ANA_MENÜ_Load` via reflection.
- **Assert (shape):** `btnAdmin.Enabled == false`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-017 — Admin gate treats any non-exact-literal value as "False" (strict, case-sensitive match)

- **Seam:** frmAnaMenu.ANA_MENÜ_Load
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 3 (DR-003)
- **Arrange:** insert exactly one `tblKullanicilar` row with `YetkiID='TRUE'` (uppercase, not the literal `"True"` the code compares against — `frmAnaMenu.cs:48`: `if(yetki=="True")`). Set matching static `kAdi`/`sifre`.
- **Act:** construct `frmAnaMenu(new Form())`, invoke `ANA_MENÜ_Load` via reflection.
- **Assert (shape):** `btnAdmin.Enabled == false` — proves the comparison is an exact, case-sensitive string match, not a case-insensitive or truthy/boolean-style comparison.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-018 — Admin gate fails open (stays enabled) when zero rows match

- **Seam:** frmAnaMenu.ANA_MENÜ_Load
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 3 (DR-003)
- **Arrange:** arrange the DB with no `tblKullanicilar` row matching the static `kAdi`/`sifre` that will be set (e.g. a value that matches no row at all — reachable via this direct-invocation seam even though normal UI navigation could not reach `frmAnaMenu` without a successful login first).
- **Act:** construct `frmAnaMenu(new Form())`, invoke `ANA_MENÜ_Load` via reflection.
- **Assert (shape):** `btnAdmin.Enabled == true` — the `while (okuyucu.Read())` loop never executes on zero rows, so `btnAdmin.Enabled` is left at its Designer-generated default (`true` — confirmed: `frmAnaMenu.Designer.cs` never sets `.Enabled` on `btnAdmin` explicitly). This is a genuine fail-open edge case worth pinning exactly as-is.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-019 — Room Add succeeds with a valid name and department

- **Seam:** frmOdaEkle.btnOdaESGekle_Click (Room Add)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** ensure `tblDepartmanlar` has at least one row; set `txtOdaESGodaAdi.Text = "Room X"`, `txtDepartmanID.Text` = that department's id (both `private`, set via reflection).
- **Act:** invoke `btnOdaESGekle_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true`; a `tblOda` row with `OdaAdi='Room X'` and the arranged `DepartmanID` exists afterward. The new row's own `OdaID` (auto-increment) is not asserted directly — if captured at all, it goes under `normalized_fields` per CONTRACT.md (k).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-020 — Room Add rejects an empty room name via two independently-agreeing paths

- **Seam:** frmOdaEkle.btnOdaESGekle_Click
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** set `txtOdaESGodaAdi.Text = ""`.
- **Act:** invoke `btnOdaESGekle_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null` (semantic condition: "room name left empty"); `errorProvider1.GetError(txtOdaESGodaAdi) == "Boş geçilmez"`; no `tblOda` row created — confirming this form's `if (txtOdaESGodaAdi.Text.Trim() != "")` gate (`frmOdaEkle.cs:55`) and its `ErrorProvider` display genuinely agree, unlike Login (GM-013/GM-014).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-021 — Room Add allows a duplicate room name to succeed (no unique constraint exists)

- **Seam:** frmOdaEkle.btnOdaESGekle_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert one existing `tblOda` row with `OdaAdi='Room X'`. Set `txtOdaESGodaAdi.Text = "Room X"` (same name) with a valid department.
- **Act:** invoke `btnOdaESGekle_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true`, `duplicate_name_rows_after: 2` — the insert succeeds without exception, per CQ-018 (resolved via schema inspection: no unique constraint exists on `OdaAdi`). This directly contradicts what the screen's own `catch` block message ("Kayıtlı Oda...") implies about duplicate detection.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-022 — Room Add with no department selected fails with a misleading "duplicate" message

- **Seam:** frmOdaEkle.btnOdaESGekle_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** set `txtOdaESGodaAdi.Text = "Room Y"` (valid, non-empty), leave `txtDepartmanID.Text = ""` (no department ever selected — `DR-004`'s non-empty check does not cover this field at all, confirmed: functional-spec.md "Non-empty checks (DR-004) on room name only").
- **Act:** invoke `btnOdaESGekle_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: true` (an exception is genuinely raised and caught here, unlike GM-020); semantic condition: "no department selected — parameter value could not be converted to the DepartmentID column's type." Record the raw `ExceptionType`/`ExceptionMessage` as evidence (CONTRACT.md b.2); leave `error_code: null` — this run cannot confidently map the exact SqlException condition without running it (harness mode's error-map.md job, not design mode's). No `tblOda` row created.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-023 — Room Update succeeds, keyed by the room's current name

- **Seam:** frmOdaGuncelle.btnOdaESGguncelle_Click (Room Update)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-004: name-keyed update is intentional, preserve as-is)
- **Arrange:** insert one `tblOda` row with `OdaAdi='Old Name'`. Set `cboOdaESGodaAdiGuncel.Text = "Old Name"`, `txtOdaESGyeniOdaAdi.Text = "New Name"`.
- **Act:** invoke `btnOdaESGguncelle_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 1`; the row's `OdaAdi` is now `"New Name"`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-024 — Room Update reports success even when the old name matches no row

- **Seam:** frmOdaGuncelle.btnOdaESGguncelle_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** ensure no `tblOda` row has `OdaAdi='Nonexistent'`. Set `cboOdaESGodaAdiGuncel.Text = "Nonexistent"`, `txtOdaESGyeniOdaAdi.Text = "New Name"`.
- **Act:** invoke `btnOdaESGguncelle_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 0` — `UPDATE ... WHERE OdaAdi=@EodaAdi` matching zero rows does not throw in SQL Server, so the handler still shows "Kayıt Başarıyla Güncellendi." even though nothing changed. This is a genuine silent no-op the golden master must pin exactly as observed.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-025 — Room Update renames every room sharing the old name (multi-row side effect of name-keying)

- **Seam:** frmOdaGuncelle.btnOdaESGguncelle_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (demonstrates the "name treated as a key, but not enforced unique" mismatch — cite CQ-004, CQ-018)
- **Arrange:** insert two `tblOda` rows that both have `OdaAdi='Shared Name'` (reachable per GM-021, since no unique constraint prevents this). Set `cboOdaESGodaAdiGuncel.Text = "Shared Name"`, `txtOdaESGyeniOdaAdi.Text = "Renamed"`.
- **Act:** invoke `btnOdaESGguncelle_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 2` — both rows are renamed to `"Renamed"` in one call, because the `UPDATE`'s `WHERE OdaAdi=@EodaAdi` has no row-count limit and the app never had an `OdaID` in hand to disambiguate (functional-spec.md Named Gap 4).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-026 — Room Delete succeeds with no confirmation dialog

- **Seam:** frmOdaSil.btnOdaESGsil_Click (Room Delete)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-017: reproduce as-is, no confirmation)
- **Arrange:** insert one `tblOda` row with `OdaAdi='Room Z'` and **no** associated `tblOdaDemirbasAtama` rows. Set `cboOdaESGodaAdiSil.Text = "Room Z"`.
- **Act:** invoke `btnOdaESGsil_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 1`; the row no longer exists afterward.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-027 — Room Delete reports success even when the name matches no row

- **Seam:** frmOdaSil.btnOdaESGsil_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** ensure no `tblOda` row has `OdaAdi='Nonexistent'`. Set `cboOdaESGodaAdiSil.Text = "Nonexistent"`.
- **Act:** invoke `btnOdaESGsil_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 0` — same silent-no-op class as GM-024, still shows "Kayıt Başarıyla Silindi."
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-028 — Room Delete removes every room sharing the deleted name (multi-row side effect)

- **Seam:** frmOdaSil.btnOdaESGsil_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-004, CQ-018)
- **Arrange:** insert two `tblOda` rows both with `OdaAdi='Shared Name'`, neither with associated `tblOdaDemirbasAtama` rows. Set `cboOdaESGodaAdiSil.Text = "Shared Name"`.
- **Act:** invoke `btnOdaESGsil_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `rows_affected: 2` — both rows are deleted in one call.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-029 — Room Delete when the room still has an associated asset-assignment row ⚠ PROVISIONAL — pending PQ-010 (proposed default: succeeds, orphaning the child row)

- **Seam:** frmOdaSil.btnOdaESGsil_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule ⚠ PROVISIONAL — pending PQ-010 (proposed default: succeeds, orphaning the child row)
- **Arrange:** insert one `tblOda` row and one `tblOdaDemirbasAtama` row whose `OdaID` references it (either a room-responsibility row or an asset-issue row — either shape references `OdaID` per CQ-003). Set `cboOdaESGodaAdiSil.Text` to that room's name.
- **Act:** invoke `btnOdaESGsil_Click` via reflection.
- **Assert (shape):** record whichever actually happens, faithfully: **either** `outcome: "REJECTED"`, `threw: true` (an unhandled `SqlException` propagates, since `frmOdaSil.cs` has no `try/catch` at all — `error_code: null`, since this run cannot determine the semantic condition without knowing whether a real FK constraint exists), **or** `outcome: "OK"`, `threw: false`, `rows_affected: 1`, plus the child `tblOdaDemirbasAtama` row's `OdaID` is now `NULL`/orphaned. This scenario's own outcome is genuinely unknown until PQ-010 is answered or a human runs the harness and observes it directly — the harness must not assume either branch; it must record what the database actually does.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-030 — Room⇄Personnel Assignment succeeds when both a room and a personnel row are selected

- **Seam:** frmOdaTanimlama.btnOTodaKaydet_Click (Room Assignment)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert one `tblOda` row and one `tblPersonel` row. Set the private `Odaid`/`Personelid` fields via reflection (simulating both grid `RowEnter` events having already fired).
- **Act:** invoke `btnOTodaKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true`; a `tblOdaDemirbasAtama` row exists with the arranged `OdaID`/`PersonelID` and `NULL` `DemirbasID`/`AlinanAdet` (the room-responsibility row shape — CQ-003).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-031 — Room Assignment inserts a NULL OdaID silently when no room row was ever entered

- **Seam:** frmOdaTanimlama.btnOTodaKaydet_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-005: defect, confirmed via schema inspection that `OdaID` is nullable)
- **Arrange:** insert one `tblPersonel` row; set the private `Personelid` field via reflection; leave `Odaid` at its default (`null` — never set, since `dGWOda_RowEnter` never fired).
- **Act:** invoke `btnOTodaKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true` — the insert succeeds with `OdaID = NULL`, since there is no guard and the schema permits it. No crash occurs despite the complete absence of a `try/catch` in this handler.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-032 — Room Assignment inserts both fields NULL when neither grid row was ever entered

- **Seam:** frmOdaTanimlama.btnOTodaKaydet_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-005)
- **Arrange:** leave both private `Odaid`/`Personelid` fields at their default `null` (no reflection-set at all — the exact scenario CQ-005 discusses: the button clicked before any row was entered).
- **Act:** invoke `btnOTodaKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true` — a fully orphaned `tblOdaDemirbasAtama` row (`OdaID = NULL`, `PersonelID = NULL`) is created, still with no crash.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-033 — Asset Assignment's room grid shows both a room-responsibility row and an asset-issue row for the same room, undistinguished

- **Seam:** frmDemirbasIslem.dgwOdaDoldur() (Room-selection read)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-003, functional-spec.md Named Gap 12)
- **Arrange:** insert one `tblOda` row and one `tblPersonel` row; insert **two** `tblOdaDemirbasAtama` rows referencing the same `OdaID`/`PersonelID` pair — one shaped as a room-responsibility row (`DemirbasID`/`AlinanAdet` both `NULL`), one shaped as an asset-issue row (both populated, referencing an arranged `tblDemirbas` row).
- **Act:** invoke `dgwOdaDoldur()` (public method, no reflection needed for the call itself, only for reading the private `dgwOdalar` control's bound `DataSource` afterward).
- **Assert (shape):** the resulting row set contains **two** rows for the same `OdaID`/room name/personnel — the query (`frmDemirbasIslem.cs:47`) has no filter distinguishing the two `tblOdaDemirbasAtama` row kinds, so both surface identically in this grid. `row_count: 2` for this arranged room.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-034 — DR-001 guard rejects a quantity exceeding stock, no database write occurs

- **Seam:** frmDemirbasIslem.btnDemirbasIslemKaydet_Click (Asset Assignment composite flow)
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 1 (DR-001)
- **Arrange:** arrange a `tblDemirbas` row with `Adet=5`; arrange a `tblOda` row and a `tblPersonel` row (with an existing room-responsibility `tblOdaDemirbasAtama` row so `personelID` is available). Set private fields `demirbasID`, `odaID`, `personelID`, `stok=5` via reflection; set `txtDIAdet.Text = "6"` (exceeds stock by 1 — boundary).
- **Act:** invoke `btnDemirbasIslemKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null` (semantic condition: "requested quantity exceeds available stock"); no new `tblOdaDemirbasAtama` row created; `tblDemirbas.Adet` unchanged at `5`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-035 — DR-001 guard passes when the requested quantity exactly equals stock

- **Seam:** frmDemirbasIslem.btnDemirbasIslemKaydet_Click
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 1 (DR-001)
- **Arrange:** same as GM-034 but `stok=5`, `txtDIAdet.Text = "5"` (exact equality — the guard is `if (Alinanadet > stok)`, so equality does **not** trigger rejection).
- **Act:** invoke `btnDemirbasIslemKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true`; `tblDemirbas.Adet == 0` afterward (see GM-037 for this as its own decrement boundary).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-036 — DR-001/DR-002 composite flow: partial issue succeeds and decrements stock correctly

- **Seam:** frmDemirbasIslem.btnDemirbasIslemKaydet_Click + GuncelleAdet()
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 1, rule 2 (DR-001, DR-002)
- **Arrange:** `tblDemirbas.Adet=10`; set `stok=10`, `txtDIAdet.Text = "3"` (below stock).
- **Act:** invoke `btnDemirbasIslemKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `row_created: true` (new `tblOdaDemirbasAtama` row with `AlinanAdet=3`); `tblDemirbas.Adet == 7` afterward (10 - 3).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-037 — DR-002 boundary: issuing the exact remaining stock decrements to exactly zero

- **Seam:** frmDemirbasIslem.GuncelleAdet()
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 2 (DR-002)
- **Arrange:** `tblDemirbas.Adet=4`; set private fields `stok=4`, `Alinanadet=4`, `demirbasID` (via reflection) — calling `GuncelleAdet()` directly, independent of the click handler.
- **Act:** invoke the public `GuncelleAdet()` method directly.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`; `tblDemirbas.Adet == 0` afterward — the 0%-remaining boundary of this computed decrement.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-038 — DR-001 empty-quantity input rejected via two independently-agreeing paths

- **Seam:** frmDemirbasIslem.btnDemirbasIslemKaydet_Click
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** arrange valid room/asset/personnel state; set `txtDIAdet.Text = ""`.
- **Act:** invoke `btnDemirbasIslemKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null` (semantic condition: "quantity left empty"); `errorProvider1.GetError(txtDIAdet) == "Boş geçilmez"`; no DB write. Confirms `frmDemirbasIslem.cs:85-89`'s `if (txtDIAdet.Text.Trim() != "")` gate and its `ErrorProvider` display agree here (same class as Room Add, GM-020 — not Login's GM-013).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-039 — Non-numeric quantity bypassing the keypress filter causes a caught FormatException

- **Seam:** frmDemirbasIslem.btnDemirbasIslemKaydet_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (demonstrates a DR-005 bypass: the keypress filter (GM-005) only constrains real keystrokes, not a value set directly)
- **Arrange:** arrange valid room/asset/personnel state; set `txtDIAdet.Text = "abc"` directly via reflection (bypassing the keypress filter entirely, since no real `KeyPress` events are raised by this arrange step).
- **Act:** invoke `btnDemirbasIslemKaydet_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: true` (semantic condition: "quantity text could not be parsed as an integer" — `int.Parse` throws `FormatException`, caught by the outer catch-all at `frmDemirbasIslem.cs:112-115`); record `ExceptionType: "FormatException"` as evidence; `error_code: null` (this run does not attempt to distinguish this catch-all's possible causes — see harness mode's error-map.md); no DB write.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-040 — GuncelleAdet() called directly bypasses the DR-001 guard entirely, allowing stock to go negative

- **Seam:** frmDemirbasIslem.GuncelleAdet()
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (demonstrates DR-001's guard is enforced only by the click handler, not by this method — see seams.md seam #6)
- **Arrange:** `tblDemirbas.Adet=2`; set private fields `stok=2`, `Alinanadet=5` (exceeds stock — the exact condition DR-001's guard exists to prevent), `demirbasID` via reflection.
- **Act:** invoke the public `GuncelleAdet()` method directly, **without** going through `btnDemirbasIslemKaydet_Click` at all.
- **Assert (shape):** record whichever actually happens, faithfully — `tblDemirbas.Adet == -3` if the `UPDATE` succeeds (no `CHECK` constraint on `Adet` was confirmed or ruled out by any document read this run), or a thrown exception if one exists. Either way, the key finding this scenario pins is that **no rejection occurs from DR-001's guard at this call path** — `GuncelleAdet()` itself contains no such check.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-041 — Asset search by name rejects an empty search term via two independently-agreeing paths

- **Seam:** frmAramalar.btnDemirbasArama_Click (Search — asset filter)
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** ensure `rdbDemirbasAdi.Checked = true` (default branch); set `txtAramalarBilgiGiriniz.Text = ""`.
- **Act:** invoke `btnDemirbasArama_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: false`, `error_code: null`; `errorProvider1.GetError(txtAramalarBilgiGiriniz) == "Boş geçilmez"`; no query executes.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-042 — Asset search by name returns a matching row

- **Seam:** frmAramalar.btnDemirbasArama_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert a `tblDemirbas`/`tblDemirbasTurleri` pair with `DemirbasAdi='Laptop'`. Ensure `rdbDemirbasAdi.Checked=true`; set `txtAramalarBilgiGiriniz.Text = "Laptop"`.
- **Act:** invoke `btnDemirbasArama_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `results: [{DemirbasAdi: "Laptop", ...}]` (row order excluded from comparison per seams.md Capture Blocker #3 / PQ-012 — sort by a stable key before recording).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-043 — Asset search by purchase date swaps in the DateTimePicker, bypassing the text-box empty-check entirely

- **Seam:** frmAramalar.btnDemirbasArama_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (demonstrates DR-004's non-empty check is not applicable to this branch — functional-spec.md: "not applicable when the date picker is active")
- **Arrange:** insert a `tblDemirbas` row with a known `AlimTarihi`. Set `rdbAlimTarihi.Checked = true`; explicitly set `dtmBilgi.Value` (via reflection, per seams.md Capture Blocker #1 — never rely on the control's `DateTime.Now` load default) to that same date.
- **Act:** invoke `btnDemirbasArama_Click` via reflection.
- **Assert (shape):** record whichever actually happens, faithfully — the fixture must record whether the `LIKE '%yyyy-MM-dd%'` pattern (`frmAramalar.cs:136`) actually matches the arranged row, since this depends on SQL Server's implicit `datetime`→`varchar` conversion format (seams.md Capture Blocker #6, not independently verifiable from source). `outcome`/`results` reflect the true observed result, not an assumed match.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-044 — Personnel search with both name fields empty has no gate at all and returns zero rows

- **Seam:** frmAramalar.btnAramalarArama_Click (Search — personnel filter)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (demonstrates this branch has no DR-004 gate — confirmed by reading the whole method, unlike the asset-filter branch)
- **Arrange:** ensure no `tblPersonel` row has an empty `PersonelAdi`/`PersonelSoyadi` (a safe real-world assumption). Set `txtAramalarAd.Text = ""`, `txtAramalarSoyad.Text = ""`.
- **Act:** invoke `btnAramalarArama_Click` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `results: []` — the query runs unconditionally (no non-empty check exists on this branch at all) and returns zero rows because `WHERE PersonelAdi='' AND PersonelSoyadi=''` matches nothing.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-045 — Personnel search with an apostrophe in the input triggers a SQL syntax error, caught by the outer try/catch

- **Seam:** frmAramalar.btnAramalarArama_Click
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (cite CQ-010: this SQL-injection-vulnerable pattern is a confirmed DEFECT the rebuild will fix — this scenario captures the legacy AS-IS behavior for baseline parity regardless; expect this fixture to legitimately DIVERGE at replay once the rebuild's parameterized query is in place, per CQ-010's own resolution)
- **Arrange:** set `txtAramalarAd.Text = "O'Brien"` (a realistic personnel first name containing an apostrophe), `txtAramalarSoyad.Text = "Smith"`.
- **Act:** invoke `btnAramalarArama_Click` via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: true` (semantic condition: "search text containing a single quote breaks the concatenated SQL statement"); record `ExceptionType`/`ExceptionMessage` as evidence; `error_code: null` (defer to harness mode's error-map.md); caught by `frmAramalar.cs:150-153`'s catch, showing "Hatalı İşlem!!".
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-046 — Reporting throws an unhandled exception when the selected room name contains an apostrophe

- **Seam:** frmRapor.RaporDoldur() (Reporting query)
- **Seam layer:** service
- **Business rules pinned:** no numbered rule (RaporDoldur() has no try/catch at all — the only read method among the eleven forms with none; cite CQ-010 for the same underlying SQL-concatenation pattern)
- **Arrange:** insert a `tblOda` row with `OdaAdi = "O'Brien's Office"` (a realistic room name — this is reachable through completely normal usage, not malicious input: any room legitimately named with an apostrophe crashes this screen on load). Set `cmbRapor.Text` to that room name via reflection.
- **Act:** invoke the private `RaporDoldur()` method via reflection.
- **Assert (shape):** `outcome: "REJECTED"`, `threw: true` (semantic condition: "room name containing a single quote breaks the concatenated SQL statement — unhandled, since this method has no try/catch at all"); record `ExceptionType`/`ExceptionMessage` as evidence; `error_code: null`.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-047 — Reporting returns the joined report rows for a room with assignments

- **Seam:** frmRapor.RaporDoldur()
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert a `tblOda`, `tblPersonel`, `tblDemirbas` row, and a `tblOdaDemirbasAtama` row referencing all three (a full asset-issue row per CQ-003's confirmed row shape). Set `cmbRapor.Text` to the room's name.
- **Act:** invoke `RaporDoldur()` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`; one result row carrying `OdaAdi`, `PersonelAdi`, `PersonelSoyadi`, `DemirbasAdi`, `Fiyat`, `AlimTarihi`, `AlinanAdet` matching the arranged data.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-048 — Reporting returns an empty result set for a room with zero assignments

- **Seam:** frmRapor.RaporDoldur()
- **Seam layer:** service
- **Business rules pinned:** no numbered rule
- **Arrange:** insert a `tblOda` row with no associated `tblOdaDemirbasAtama` rows at all. Set `cmbRapor.Text` to that room's name.
- **Act:** invoke `RaporDoldur()` via reflection.
- **Assert (shape):** `outcome: "OK"`, `threw: false`, `error_code: null`, `results: []` — the `INNER JOIN` chain (`frmRapor.cs:59`) means a room with no assignment row produces zero output rows, not an error and not a row with null-filled columns.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

## No Legacy Behaviour Exists

1. **Creating, editing, or deleting a `tblKullanicilar` (User) row via the application.** No screen among the eleven opened this run performs any CRUD against this table — accounts must be provisioned directly against the database (domain-model.md User entity Named Gap; functional-spec.md Named Gap 1; CQ-012, resolved SCOPE: preserve the legacy assumption, no admin CRUD added in the rebuild either).
2. **Creating, editing, or deleting a `tblDepartmanlar` (Department) row via the application.** Read-only lookup everywhere it's touched (`frmOdaEkle.cs`) — no code path mutates it (functional-spec.md Named Gap 2; CQ-012).
3. **Creating, editing, or deleting a `tblPersonel` (Personnel) row via the application.** Read-only lookup everywhere (`frmOdaTanimlama.cs`, `frmDemirbasIslem.cs`, `frmAramalar.cs`, `frmRapor.cs`) — no code path mutates it (functional-spec.md Named Gap 2; CQ-012).
4. **Creating, editing, or deleting a `tblDemirbasTurleri` (AssetType) row via the application.** Read-only lookup everywhere (`frmStokEkleme.cs`, `frmStokGuncelleme.cs`) — no code path mutates it (functional-spec.md Named Gap 2; CQ-012).
5. **PDF or any other file export from the Reporting screen.** The `.csproj` references `itextsharp`, but the referenced `.dll` does not exist in this checkout and no `.cs` file anywhere in scope uses an `iTextSharp` namespace (codebase-report.md Dependencies/Risks; architecture.md L1) — there is no method to invoke and nothing to capture. The kind of thing that should become (and already has become, per CQ-021) a clarify TARGET-GAP question about whether the rebuild needs real file export, since the legacy app never actually had it despite the vestigial reference.
6. **Whatever screen `frmAdminsilinecek.resx` once belonged to.** No corresponding `.cs`/`.Designer.cs` file exists anywhere in this repository (functional-spec.md Named Gap 10; CQ-019, resolved: treat as vestigial, no rebuild action) — there is no class to instantiate and no method to call.
7. **A third, distinct `YetkiID` authorization tier beyond the binary True/else split.** `frmAnaMenu.cs:48`'s comparison (`if(yetki=="True") ... else ...`) only ever branches two ways — every value other than the exact literal `"True"` falls into the same `else` (disabled) branch; there is no third tier anywhere in scope (see GM-017 for the boundary this collapses to).

## Rule Coverage Check

- **DR-001 (Stock adequacy check before assignment):** covered by GM-034, GM-035, GM-036.
- **DR-002 (Stock decrement on assignment):** covered by GM-036, GM-037.
- **DR-003 (Admin authorization gate):** covered by GM-015, GM-016, GM-017, GM-018. The multi-row-match ambiguity (Capture Blocker #2 in seams.md) is deliberately **not** scenario'd — see PQ-011.
- **DR-004 (Required-field soft validation, cross-cutting):** covered by GM-013, GM-014 (Login — independent-path variant), GM-020 (Room Add — agreeing-path variant), GM-038 (Asset Assignment — agreeing-path variant), GM-041 (Search — agreeing-path variant). Not every one of the six screens domain-model.md cites for DR-004 gets its own scenario (Stock Add/Update's identical `Trim()!=""` + `ErrorProvider` shape is structurally identical to GM-020/GM-038 and is not separately scenario'd — same code shape, one representative pair suffices per screen family).
- **DR-005 (Numeric-only keypress filters, cross-cutting):** covered by GM-005, GM-006, GM-007 (one scenario per independently-duplicated implementation), plus GM-039 (what happens when this filter is bypassed).
- **DR-006 (Letter-only keypress filters, cross-cutting):** covered by GM-008, GM-009 (wired implementations), GM-010 (unwired implementation, Named Gap).
- **DR-007 (Price-string classifier, dead code):** covered by GM-001, GM-002, GM-003, GM-004.

**Provisional pending decision**

- **No numbered rule (Room Delete referenced by an existing asset-assignment row)** — GM-029, blocked by **PQ-010** (proposed default: succeeds, orphaning the child row).

No `DR-NNN` rule itself carries a `⚠ PROVISIONAL` marker in `domain-model.md`, and no `clarifications.md` CQ promoted from a PQ remains unanswered — every CQ in `clarifications.md` (CQ-001 through CQ-022) has a filled `Answer`/`Decided by`/`Date`. GM-029 is provisional only because of a **fresh** PQ (PQ-010) raised by this run itself, not because any existing rule or CQ is still open.
