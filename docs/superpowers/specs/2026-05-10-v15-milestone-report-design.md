# Design — Laporan Milestone v15.0 (HTML)

**Tanggal:** 2026-05-10
**Author:** Rino + Claude (brainstorming session)
**Status:** Approved, siap masuk implementation plan

## Tujuan

Menghasilkan satu file HTML `docs/v15.0-MILESTONE-REPORT.html` yang menjelaskan ke Tim IT (audiens utama) dan manajer (audiens sekunder) seluruh pekerjaan yang dilakukan pada milestone v15.0 *Audit Findings 27 April 2026* — sebagai serah-terima dari developer ke IT untuk redeploy ke server Development.

Konteks: kode terakhir yang dikirim ke IT adalah commit `381b36cd` (2026-04-15 09:42). Sejak itu sudah ada ~190 commit, 12 phase selesai, dan 1 EF migration DB baru. Laporan ini menggantikan email/chat ad-hoc dengan dokumen formal.

## Audiens & format

- **Audiens utama:** Tim IT (yang akan redeploy ke server Dev `10.55.3.3`).
- **Audiens sekunder:** Manajer / non-teknis yang ingin tahu status milestone.
- **Pendekatan:** Hybrid single-document — section eksekutif di atas (1 layar scroll) untuk audiens sekunder, detail teknis di bawah untuk IT.

## Bentuk output

| Atribut | Pilihan |
|---------|---------|
| File | `docs/v15.0-MILESTONE-REPORT.html` |
| Format | Single self-contained HTML, embed CSS, **tanpa JS** |
| Style | Static print-friendly, single column max-width 900px |
| Bahasa | Indonesia |
| Periode | 2026-04-15 (`381b36cd`) → 2026-05-08 (`551339c5`) |
| Dependency external | Nol (no CDN, no web font) |

## Struktur dokumen

```
1. HEADER             — Title, periode, status milestone, last commit hash
2. EXECUTIVE SUMMARY  — 4 KPI box + paragraf 3-4 kalimat + status box
3. AKSI IT            — DB migration + redeploy checklist + smoke test (highlight)
4. RINGKASAN PER WAVE — Tabel 5 wave × phase × status
5. DETAIL PER PHASE   — 12 card phase (medium detail)
6. DEFERRED           — EPRV-01 + 7 carry-over v14.0
7. REFERENSI          — Commit range, .planning/ pointer
```

### Section 1 — Header

- Title: "Laporan Milestone v15.0 — Audit Findings 27 April 2026"
- Subtitle: "Serah-Terima Pengembangan ke Tim IT untuk Redeploy"
- Periode: 2026-04-15 — 2026-05-08
- Status badge: ✅ SIAP REDEPLOY
- Commit reference: HEAD `551339c5`

### Section 2 — Executive Summary

KPI box (grid 2×2 atau 1×4):
- **Phase:** 12/12 (100%)
- **Plan:** 28/28 (100%)
- **Requirement:** 14/14 (100%)
- **Temuan Audit:** 15/15 (11 audit 27 April + 4 audit 29 April)

Paragraf eksekutif (3-4 kalimat):
> Milestone v15.0 menyelesaikan seluruh 15 temuan audit (27 dan 29 April 2026) tanpa migrasi DB skala besar. Pekerjaan dipecah menjadi 12 phase di 5 wave (UI label → UI behavior → defensive fix → performance → audit susulan), seluruhnya selesai dalam ~24 hari kalender (28 April – 8 Mei 2026). Semua phase telah lolos UAT lokal; siap redeploy ke server Development. Satu deferred item (`EPRV-01` Preview Essay rubrik) menunggu klarifikasi user dengan due 2026-05-12.

Status box: SIAP REDEPLOY (color: hijau).

### Section 3 — AKSI IT (HIGHLIGHT — orange/yellow border)

#### 3.1 DB Schema Diff

Sejak commit IT `381b36cd` (2026-04-15), DB schema berubah dengan **1 EF migration baru**:

| Migration | File | Phase | Dampak |
|-----------|------|-------|--------|
| `20260507073825_AddManageAssessmentPerfIndexes` | `Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs` | 311 (PERF-01) | Tambah 2 non-clustered index ke tabel `AssessmentSessions` |

Index yang ditambahkan:
- `IX_AssessmentSessions_LinkedGroupId`
- `IX_AssessmentSessions_ExamWindowCloseDate`

**Sifat:** Schema-only (tidak ubah data row), idempotent, reversible (`Down` migration tersedia).

#### 3.2 Opsi Apply Migration

**Opsi A (recommended):** Apply migration via EF Core Tools di server Dev setelah pull HEAD.
```bash
git pull origin main
dotnet ef database update --connection "Server=...;Database=HcPortalDB_Dev;..."
```

**Opsi B (alternatif):** Request DB snapshot lokal dari Rino, restore di server Dev.
- Gunakan jika ada concern data drift atau tidak yakin migration history server Dev sinkron.
- Kontak Rino untuk file `.bak`.

#### 3.3 Redeploy Checklist

1. Pull `551339c5` (atau HEAD `main`) ke server Dev
2. Apply migration (Opsi A atau B di atas)
3. Build & publish: `dotnet publish -c Release`
4. Restart IIS app pool `KPB-PortalHC`
5. Smoke test 3 area kritis (lihat 3.4)

#### 3.4 Smoke Test Pasca-Redeploy

| Area | Test |
|------|------|
| Login | Eye-icon password toggle berfungsi (Phase 304) |
| Wizard Create Assessment | Step 2 menampilkan list peserta, Step 3 label "(WIB)", PrePost mode tidak reset wizard (Phase 304/307/308) |
| Exam timer | Manual submit setelah waktu habis ditolak dengan banner Tier-1; auto-submit grace period 2 menit (Phase 313) |
| Regenerate token | Klik Regenerate Token di session Upcoming tidak 404 lagi (Phase 314) |
| ManageAssessment | Halaman load <2 detik via HTMX lazy load (Phase 311) |

#### 3.5 Rollback Note

Jika perlu rollback: `dotnet ef database update <previous-migration-name>` untuk revert index, lalu redeploy commit pre-`551339c5`. Index drop tidak mempengaruhi data.

### Section 4 — Ringkasan per Wave

Tabel:

| Wave | Tema | Phase | Tanggal Selesai | Status |
|------|------|-------|-----------------|--------|
| 1 | UI Label & Polish | 304, 305 | 2026-04-28 | ✅ |
| 2 | UI Behavior | 306, 307, 308 | 2026-04-28 → 04-29 | ✅ |
| 3 | Defensive + State | 309, 310 | 2026-05-01 → 05-05 | ✅ |
| 4 | Performance | 311 | 2026-05-07 | ✅ |
| 5 | Audit Susulan 29 April | 312, 313, 313.1, 314 | 2026-05-07 → 05-08 | ✅ |

### Section 5 — Detail per Phase (Card Medium)

Setiap card berisi:
- **Nomor + Judul phase**
- **REQ ID** (link ke daftar requirement)
- **Problem audit** (1 kalimat dari temuan audit)
- **Fix** (1-2 kalimat ringkasan teknis)
- **File utama** (path relatif, max 3 file)
- **Status UAT** (✅ verified / ⏸️ pending)
- **Commit anchor** (range hash pertama..terakhir per phase)

Total 12 card: 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 313.1, 314.

Sumber data card: `.planning/ROADMAP.md` § "v15.0 Audit Findings 27 April 2026" (success criteria sudah tertulis), cross-reference dengan `git log 381b36cd..HEAD` untuk commit hash.

### Section 6 — Deferred & Carry-Over

#### Deferred v15.0 (1 item)

| REQ | Item | Due | Aksi |
|-----|------|-----|------|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | 2026-05-12 | Smoke test save/load Rubrik dulu. Jalur B → defer v16.0 (tabrak goal "tanpa migrasi DB"). |

#### Carry-Over dari v14.0 (7 item, masih open)

| Kategori | Item | Status | Sumber |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | paused-at-checkpoint | HANDOFF.json (2026-04-10) |
| UAT | Phase 235 — 5 items butuh human browser verification | pending | STATE.md |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notif) | pending — overlap risk Phase 310 | STATE.md |
| Research gap | Phase 297 PrePost Renewal — keputusan 2 sesi baru otomatis | undecided | v14.0 planning |
| Research gap | Phase 298 essay max char limit nvarchar(max) vs nvarchar(2000) | undecided | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | undecided | v13.0 carry-over |
| Paused | Phase 281 + 285 (System Settings + Dedicated Impersonation) | paused | MILESTONES.md v11.2 |

### Section 7 — Referensi

- **Commit range:** `381b36cd..551339c5` (2026-04-15 → 2026-05-08)
- **Roadmap:** `.planning/ROADMAP.md` § "v15.0 Audit Findings 27 April 2026"
- **State:** `.planning/STATE.md`
- **Source audit:** Audit Findings 27 April 2026 + 29 April 2026 (internal doc)
- **Workflow developer:** `docs/DEV_WORKFLOW.md`
- **Seed data workflow:** `docs/SEED_WORKFLOW.md` + `docs/SEED_JOURNAL.md`

## Visual & Style

### Palette

| Role | Color |
|------|-------|
| Brand accent | `#e30613` (Pertamina red) |
| Neutral text | `#1f2937` (gray-800) |
| Background | `#ffffff` |
| Status: done | `#16a34a` (green-600) |
| Status: deferred | `#f59e0b` (amber-500) |
| Status: blocker | `#dc2626` (red-600) |
| Highlight (Aksi IT) | `#fef3c7` background + `#f59e0b` border |

### Typography

- Font: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`
- Heading scale: h1 28px, h2 22px, h3 18px, h4 16px
- Body 15px, line-height 1.6
- Monospace untuk commit hash & code: `Consolas, "Courier New", monospace`

### Layout

- Container max-width 900px, centered, padding 32px
- Section gap 48px
- Card detail phase: border 1px solid `#e5e7eb`, padding 20px, border-radius 8px
- Tabel: full-width, border-collapse, alternating row `#f9fafb`

### Print

- `@media print`: header background → border, hide hover state, page-break-inside: avoid pada card phase, page-break-before: always pada section "Detail per Phase".

## Out of Scope

- Code snippet / diff (IT punya commit hash, bisa lihat sendiri)
- Screenshot UI (butuh environment akses, tambah kalau IT minta)
- Performance metric grafik Phase 311 (sudah dokumentasi internal di `.planning/phases/311-*/`)
- Phase non-v15.0 (sudah archived, tidak relevan ke redeploy ini)
- Login user/password test (sensitive, sudah ada di `MEMORY.md` referensi developer)

## Sukses Kriteria

1. File `docs/v15.0-MILESTONE-REPORT.html` tergenerate, valid HTML5, dapat dibuka offline (no CDN dependency).
2. Print preview di Chrome menampilkan layout yang rapi (no overflow, page-break logical).
3. Section 3 (Aksi IT) menampilkan migration name, opsi A/B, checklist 5-step, dan smoke test 3+ area.
4. 12 card phase lengkap dengan REQ, problem, fix, file, status, commit hash.
5. Deferred section list 1 v15.0 + 7 carry-over.
6. Tidak ada placeholder "TODO"/"TBD" di file output.

## Sumber Data

| Section | Source |
|---------|--------|
| Header / KPI | `.planning/STATE.md` (progress block) |
| Wave summary | `.planning/ROADMAP.md` (wave structure) |
| Card phase | `.planning/ROADMAP.md` (success criteria) |
| Commit hash per phase | `git log 381b36cd..HEAD --grep "<phase-num>"` |
| Migration list | `git diff 381b36cd..HEAD --name-only -- "Migrations/"` |
| Deferred / carry-over | `.planning/STATE.md` (Deferred Items block) |

## Risiko & Mitigasi

| Risiko | Mitigasi |
|--------|----------|
| Commit hash per phase tidak akurat (commit silang phase) | Use `git log --grep` per phase number; manual verify di plan implementation |
| Migration diapply 2x di Dev (kalau IT pernah pull intermediate) | Migration EF Core idempotent by design (`__EFMigrationsHistory` tracking) |
| Manager butuh PDF — file HTML tidak otomatis PDF | Print-to-PDF dari Chrome cukup; layout sudah print-optimized |
| Rino pindah laptop → snapshot DB hilang | Mitigasi: Opsi A (`dotnet ef database update`) tidak butuh snapshot |
