# Phase 426: Audit-Log EditOrganizationUnit - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Tambah jejak `AuditLog` pada `OrganizationController.EditOrganizationUnit` (rename/reparent unit) — mirror pola yang sudah ada di `DeleteOrganizationUnit`. Menutup asimetri traceability pre-existing (Delete menulis audit, Edit tidak). Murni aditif: tidak mengubah perilaku cascade, validasi, authz, atau CSRF. migration=FALSE. Requirement: **AUDIT-01**.

Out of scope: mengubah logika cascade ph403, menambah kolom/tabel, mengubah surface lain (CMPController/StartExam = fase 427/428).
</domain>

<decisions>
## Implementation Decisions

### Pemicu Audit
- **D-01:** Audit ditulis **hanya saat ada perubahan nyata** — yaitu `oldName != name.Trim()` **ATAU** `oldParentId != parentId`. No-op edit (POST dengan nilai identik yang tetap commit) **tidak** menulis baris audit. Selaras kata "rename atau reparent" di Success Criteria; hindari noise audit.

### Format Deskripsi
- **D-02:** **Satu baris audit gabungan per Edit** (mirror `DeleteOrganizationUnit` yang 1 baris/aksi). Satu Edit yang sekaligus rename + reparent tetap menghasilkan **satu** baris `AuditLog`, bukan dua. Description = single human-readable string mencakup: nama lama→baru, parent lama→baru, dan cascade counts (`cascadedUsers` / `cascadedMappings` / `cascadedUserUnits`).

### Representasi Parent
- **D-03:** Parent ditulis sebagai **ID mentah** (`oldParentId` → `parentId`) — persis bunyi SC. **Tanpa** query DB tambahan untuk resolve nama parent di dalam blok swallow (jaga blok audit ringan + tak menambah titik gagal). `null` parent boleh ditulis apa adanya (mis. "null" / kosong).

### Penempatan & Keandalan (terkunci oleh SC, bukan gray area)
- ActionType = `"EditOrganizationUnit"` (mirror `"DeleteOrganizationUnit"`).
- Blok audit `try { _auditLog.LogAsync(...) } catch { /* swallow */ }` ditempatkan **SETELAH `await tx.CommitAsync();`** (OrganizationController.cs:308), **SEBELUM** konstruksi `msg` + `return`. Kegagalan audit TIDAK memblokir respons sukses.
- Return-early branch (unit null / nama kosong / duplikat / circular / split-block reparent) terjadi **sebelum** commit → tidak ada perubahan ter-persist → **tidak** menulis audit. Benar by-design.
- Actor: `_userManager.GetUserAsync(User)` → `actorName` = `"{NIP} - {FullName}"` (fallback FullName/"Unknown"), persis idiom Delete (line 541-544).
- `entityId = unit.Id`, `entityType = "OrganizationUnit"`.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Pola yang di-mirror (WAJIB baca)
- `Controllers/OrganizationController.cs` §`DeleteOrganizationUnit` (line ~468-555) — pola audit kanonik: `try { var currentUser = await _userManager.GetUserAsync(User); ... await _auditLog.LogAsync(userId, actorName, actionType, description, entityId, entityType); } catch { }` (line 539-549). TIRU signature + swallow + actorName format.
- `Controllers/OrganizationController.cs` §`EditOrganizationUnit` (line 129-317) — target. Variabel cascade sudah dihitung: `cascadedUsers`/`cascadedMappings`/`cascadedUserUnits` (line 198-200). `oldName` (139), `oldParentId` (140). Commit di line 308. Sisipkan blok audit setelah line 308.

### Requirement
- `.planning/REQUIREMENTS.md` AUDIT-01 (line 13) — sumber: backlog 999.11 + 403-REVIEW WR-01 (traceability gap).
- `.planning/ROADMAP.md` §"Phase 426" — Success Criteria 1-4.

Tidak ada ADR/spec eksternal — keputusan tertangkap penuh di atas.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_auditLog.LogAsync(string userId, string actorName, string actionType, string description, int entityId, string entityType)` — service injected di controller, sudah dipakai `DeleteOrganizationUnit`. Reuse langsung, no ctor change.
- `_userManager.GetUserAsync(User)` — sudah dipakai di Delete; reuse untuk actor.
- Variabel `cascadedUsers`, `cascadedMappings`, `cascadedUserUnits` sudah dihitung dalam transaksi Edit (line 198-200, 209-242) → langsung dipakai untuk deskripsi audit.

### Established Patterns
- Audit ditulis SETELAH commit, swallow-on-failure (`catch { }` kosong). Konsisten across controller.
- Transaksi cascade Edit (line 182-308): return-early branch = dispose tanpa commit = rollback. Audit harus DI LUAR/SETELAH commit.

### Integration Points
- Satu titik sisip: setelah `await tx.CommitAsync();` (line 308), sebelum `var msg = ...` (line 310).
- Guard D-01: bungkus blok audit dengan `if (oldName != name.Trim() || oldParentId != parentId) { ... }`.
</code_context>

<specifics>
## Specific Ideas

Deskripsi audit usulan (format final diserahkan planner, asal memuat semua field D-02):
`$"Edited organization unit '{oldName}'→'{name.Trim()}' [ID={unit.Id}] parent {oldParentId?.ToString() ?? "null"}→{parentId?.ToString() ?? "null"} (cascade: {cascadedUsers} users, {cascadedMappings} mappings, {cascadedUserUnits} UserUnits)"`
</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope fase. (Resolve nama parent ke string kebaca ditolak demi jaga blok swallow ringan — D-03.)
</deferred>

---

*Phase: 426-audit-log-editorganizationunit*
*Context gathered: 2026-06-24*
