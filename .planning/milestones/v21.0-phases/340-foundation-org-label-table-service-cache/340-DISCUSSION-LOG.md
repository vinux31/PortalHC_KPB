# Phase 340: Foundation — Tabel + Service + Cache — Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-02
**Phase:** 340-foundation-org-label-table-service-cache
**Areas discussed:** Seed strategy, Cache TTL, API endpoint auth, Audit log

---

## Cache TTL Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| No TTL, manual invalidate only | Per spec §4.2. Cache hidup sampai update label. Single-server Portal HC KPB → aman. Memory tidak akan numpuk (3-5 entry total). | ✓ |
| TTL 1 jam + manual invalidate | Belt-and-suspenders. Fallback bila ada bug invalidate. Tidak perlu untuk single-server. | |
| TTL 5 menit (sama dengan AssessmentAdminController pattern) | Konsisten dgn cache pattern lain. Tapi 5min terlalu agresif untuk label yang jarang berubah. | |

**User's choice:** No TTL, manual invalidate only
**Notes:** Label jarang berubah (0-2x/tahun). In-memory 3-5 entry trivial. Manual invalidate cukup.

---

## API Endpoint Auth Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Authenticated user | Per spec §4.5. Label = public display info, tidak sensitif. Dipakai JS di banyak page. Authorize tanpa role filter. | ✓ |
| Admin + HC role only | Lebih strict. Konsekuensi: user biasa di page CMP/CDP tidak bisa lihat label dynamic — perlu render server-side via @inject di view. | |
| Anonymous (no auth) | Label = static-ish info. Inconsistent dgn convention semua admin endpoint authenticated. | |

**User's choice:** Authenticated user
**Notes:** Label akan dipakai JS di banyak halaman. Strict role filter akan force SSR-only — friction tinggi.

---

## Audit Log Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Existing AuditLog table | Reuse Models/AuditLog.cs. Action='OrgLabel-Update'/'OrgLabel-Add'/'OrgLabel-Delete'. Konsisten dgn audit pattern AssessmentAdminController + TrainingAdminController. | ✓ |
| Dedicated OrgLabelHistory table | Schema bloat. Tidak ada query keperluan terpisah dari general audit timeline. | |

**User's choice:** Existing AuditLog table
**Notes:** Reuse pattern existing. Audit timeline unified.

---

## Seed Strategy

User initially asked for explanation of all 3 options. Then asked specifically to analyze with constraint: IT manages Dev/Prod data via DB_HANDOFF_IT_2026-05-26.html template. Workflow includes mandatory pre-migration backup .bak per Cilacap incident root cause.

**Expanded options after constraint analysis:**

| Option | Description | Selected |
|--------|-------------|----------|
| 1. SeedData.cs runtime `if (!Any())` | Idempotent. Match pattern existing SeedOrganizationUnitsAsync. Safe overwrite data HC custom. Migration body simple (CREATE TABLE only). | ✓ |
| 2. Migration HasData declarative | Rigid. Risk overwrite custom HC data saat re-apply migration / fresh dev / rollback restore .bak. Not match existing pattern. | |
| 3. Migration Up INSERT SQL raw | Not idempotent. Error-prone. Same risk as Opt 2. | |
| 4. Migration SQL `IF NOT EXISTS` guard | Idempotent atomic deploy. Tapi tidak runtime-guarded — bila tabel kosong setelah HC delete semua (edge), re-apply migration tidak re-seed. | |
| 5. No seed, IT manual insert post-deploy | Friction tinggi. Page Manage fallback "Level N" sampai HC manual buat. | |

**Edge case test scenarios analyzed:**
1. HC rename "Bagian" → "Direktorat" via UI, IT restart aplikasi → Opt 1 safe (tabel ada 3 row, !Any() false, skip). Custom preserved.
2. IT rollback restore .bak → tabel restored dgn custom rows. SeedData skip. Aligned dgn backup SOP.
3. Migration apply gagal di tengah tx → Opt 1 menang (CREATE TABLE small tx, low fail rate). Opt 4 risk full rollback.
4. Developer baru clone repo + fresh DB → Opt 1 + Opt 4 sama-sama functional.
5. HC delete level 2 lalu restart → Opt 1 skip (Any() true), Level 2 tidak re-seed. Sesuai spec §4.3 (delete hanya level tertinggi tidak dipakai).

**Weighted decision matrix:**

| Kriteria | Bobot | Opt 1 | Opt 4 |
|---|---|---|---|
| Safe HC data custom | HIGH | ✓ | ✓ |
| Match existing pattern | HIGH | ✓ | ✗ |
| Idempotent runtime | HIGH | ✓ | ✗ |
| Page functional out-of-box | MED | ✓ | ✓ |
| Migration body simple | MED | ✓ | partial |
| Atomic deploy | LOW | ✗ | ✓ |
| Rollback resilient (with SOP backup) | LOW | ✓ | partial |

**User's choice:** Opt 1 SeedData.cs runtime check
**Notes:** 4 kriteria HIGH semua menang. Match pola existing SeedOrganizationUnitsAsync. Migration transactional safety lebih baik. Atomic deploy benefit tidak relevan karena DB_HANDOFF SOP sudah cover backup+restore.

---

## Claude's Discretion

- Namespace organization (`Services/IOrgLabelService.cs` + `Services/OrgLabelService.cs` flat) — consistent dgn `INotificationService.cs` pattern.
- XML doc verbosity — standard summary per method.
- Test scope di Phase 340 vs Phase 344 — minimal unit test di 340 (happy path GetLabel), full integration + Playwright di 344.
- Controller class naming — NEW `OrgLabelController.cs` instead of attaching to `OrganizationController.cs` (avoid clutter, prep Phase 341 reuse).

## Deferred Ideas

- WebSocket push label refresh (multi-tab consistency) — out of scope.
- i18n multi-language label — out of scope.
- Multi-server cache invalidation — N/A single-server.
- Tom Select integration untuk dropdown induk besar — defer.
- Search box ManageOrganization tree (Innov #5) — defer.
- Drag-reparent across parents (Innov #6) — defer.
- Stats breakdown Aktif/Nonaktif (Innov #8) — defer.
