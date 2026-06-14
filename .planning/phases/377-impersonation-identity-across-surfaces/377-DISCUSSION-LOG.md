# Phase 377: Impersonation Identity Across Surfaces - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-14
**Phase:** 377-impersonation-identity-across-surfaces
**Areas discussed:** Model Fidelity Impersonasi, Mode "role" tanpa user, Deliverable Audit (SC1), Fallback target-user null, Unifikasi resolusi role-level, Boundary surface in-scope

---

## Model Fidelity Impersonasi

| Option | Description | Selected |
|--------|-------------|----------|
| Full-fidelity (effective user = X) | Semua jalur read pakai X: data + authz/ownership + scope. Batasan akses X ikut. | ✓ |
| Self-data only | Hanya surface data-diri swap ke X; authz/team-view/scope tetap admin. | |
| Hybrid (data=X, fitur=role-eff) | Data & ownership X; gating fitur by effective role-level. | |

**User's choice:** Full-fidelity (effective user = X)
**Notes:** Paling jujur dgn banner, drift minimal, konsisten effective role-level + user-id.

### Follow-up: Batas full-fidelity

| Option | Description | Selected |
|--------|-------------|----------|
| Worker-data read surfaces saja | Resolusi identitas X ke read worker-data; `[Authorize]` gating tetap principal asli (out of scope). | ✓ |
| Semua call-site GetUserAsync(User) | Swap X di setiap call-site read lintas controller. | |

**User's choice:** Worker-data read surfaces saja
**Notes:** `[Authorize(Roles=...)]` ASP.NET tetap principal asli — out of scope 377.

---

## Mode "role" tanpa user spesifik

| Option | Description | Selected |
|--------|-------------|----------|
| Kosong + hint | effective user = null → surface kosong (0 record) + hint pilih user spesifik. | ✓ |
| Tetap data admin (status quo) | Biarkan data admin saat mode role. | |
| Sembunyikan/redirect surface | Worker-data tak bisa diakses di mode role; redirect dashboard. | |

**User's choice:** Kosong + hint
**Notes:** Jujur & konsisten dgn full-fidelity (tak ada user → tak ada data). Mode role tetap preview UI/fitur per role-level.

---

## Deliverable Audit (SC1)

| Option | Description | Selected |
|--------|-------------|----------|
| 377-AUDIT.md di phase dir | Doc markdown tabel call-site (file:line, surface, jenis read, aware?, in-scope?, fix). | ✓ |
| Inline di PLAN.md | Peta jadi bagian task breakdown PLAN. | |
| Komentar kode di tiap call-site | Tandai langsung di source. | |

**User's choice:** 377-AUDIT.md di phase dir
**Notes:** Audit trail mandiri, input planner, sesuai pola fase 328.

---

## Fallback target-user null/terhapus

| Option | Description | Selected |
|--------|-------------|----------|
| Stop impersonasi + redirect | Auto-Stop() + redirect /Admin/Index + pesan "user tidak ditemukan". | ✓ |
| Tampil kosong (treat as null) | Perlakukan seperti mode-role (kosong + hint), sesi tetap jalan. | |
| Fallback ke data admin | Kembali tampil data admin asli. | |

**User's choice:** Stop impersonasi + redirect
**Notes:** Konsisten pola auto-expire middleware; aman, tak bocor.

---

## Unifikasi resolusi role-level

| Option | Description | Selected |
|--------|-------------|----------|
| Satukan jadi 1 sumber | GetCurrentUserRoleLevelAsync() impersonation-aware (effective user + role-level); konsolidasi GetEffectiveRoleLevel call-site. | ✓ |
| Biarkan dua jalur | Hanya tambah resolusi USER di worker-data; role-level dibiarkan split. | |

**User's choice:** Satukan jadi 1 sumber
**Notes:** Bunuh split-brain (Home:53/CMP:88 vs role-level asli), pola "shared core kill drift".

---

## Boundary surface in-scope

| Option | Description | Selected |
|--------|-------------|----------|
| Semua read worker-data-diri | Records + RecordsWorkerDetail(own) + Results/Certificate/CertificatePdf + Home progress/events + exam StartExam + sisanya audit. | ✓ |
| Hanya 3 surface roadmap | Records, Assessment, Home progress saja. | |
| Tentukan setelah audit | Boundary tak dikunci; planner putuskan per-call-site pasca audit SC1. | |

**User's choice:** Semua read worker-data-diri
**Notes:** Kriteria = read yang resolve identitas worker data-diri. Audit SC1 = enumerator otoritatif.

---

## Claude's Discretion

- Arsitektur fix konkret (bentuk helper terpusat GetEffectiveUserId/Async, konsolidasi CMP+CDP GetCurrentUserRoleLevelAsync).
- Lokasi fallback D-04 (middleware vs helper).
- Bentuk/penempatan hint mode-role.
- Strategi test SC4 (no-regression normal vs impersonate).

## Deferred Ideas

- Copy/UX banner & hint (minor).
- Full sandbox login-as-X (swap semua call-site + override [Authorize]) — DITOLAK untuk 377; fase tersendiri bila perlu.
- Todo cleanup DB test lokal pasca 367 (match false-positive, tak terkait).
