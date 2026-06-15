# Phase 362 — PROTON CDP Polish (Design Spec)

**Tanggal:** 2026-06-10
**Status:** Design disetujui. Siap writing-plans.
**Branch:** ITHandoff
**Milestone:** v25.0 (Proton Kelulusan & Bypass) — Phase 362 (append setelah 358-361)
**Asal:** Gap analysis PROTON (`docs/proton-gap-analysis/index.html`, spec `2026-06-09-proton-gap-analysis-design.md`)

## Tujuan

Tutup 6 gap UI / navigasi / role PROTON yang ketemu dari gap-analysis dan TIDAK tercakup phase kelulusan/bypass (358-361). Semua gap = perbaikan front-end / controller-attribute. **Tidak ada perubahan DB.**

## Konteks & keputusan terkunci

| # | Keputusan | Nilai |
|---|-----------|-------|
| 1 | Scope | 6 gap solid (G-01, G-04, G-05, G-09, G-10, G-12) |
| 2 | Penempatan | Append milestone v25.0 sebagai Phase 362 |
| 3 | Migration | FALSE (zero DB change) |
| 4 | Struktur | 3 plan per-cluster (Dashboard / Navigasi / Role) |
| 5 | G-12 | Bug → fix (Coach harus bisa export Histori) |
| 6 | ROADMAP entry | DITUNDA sampai sesi Phase 359 (paralel) selesai — hindari bentrok file ROADMAP.md |

### Gap yang DIBATALKAN saat re-check (jangan dikerjakan)
- **G-02** (StatusData kosong) — BUKAN bug. Tab Status = baris bagian collapsible (header sengaja kosong); expand → unit/track punya icon `bi-check-circle-fill` (silabus) + `bi-exclamation-triangle-fill` (guidance). Render benar. Temuan awal = misread a11y snapshot.
- **G-06** (HistoriProtonDetail "Tahun 1" dobel) — TAK BISA REPRODUCE; data assignment berubah (DB shared dimodifikasi sesi v25.0). Kode render 1 node per assignment dengan benar; "dobel" = data, bukan bug kode. Kalau muncul lagi → investigasi DATA, bukan ubah view.
- **G-03, G-08** — sudah dialamatkan v25.0 Phase 358/359 (jangan dobel).
- **G-07, G-11** — false-positive re-check sebelumnya (handler/konfirmasi ada).

## Scope detail — 6 gap

### Plan 01 — Dashboard (G-01, G-04, G-10)

**File utama:** `Views/CDP/Dashboard.cshtml`, `Controllers/CDPController.cs` (action `Dashboard` + export baru).

- **G-01 (High) — Chart.js race.** Gejala live: console `ReferenceError: Chart is not defined` @ `Dashboard.cshtml:560`; canvas `protonStatusChart` (chart "Deliverable Status") tak digambar. AKAR: chart.js (`cdn.jsdelivr.net/npm/chart.js`) ter-load, TAPI inline init @:560 jalan SEBELUM chart.js selesai parse. Pembanding: `Views/Admin/CoachWorkload.cshtml` pakai Chart.js + canvas render NORMAL (0 error) → pola yang benar ada di file itu.
  **Fix:** pastikan script chart.js dimuat sebelum init, ATAU bungkus init dalam `DOMContentLoaded` / `window.addEventListener('load', ...)` / cek `typeof Chart !== 'undefined'`. Ikuti pola CoachWorkload.cshtml.
- **G-04 (Med) — Dashboard tanpa export.** Page monitoring lain (CoachingProton, HistoriProton, CertificationManagement, CoachWorkload) punya Export Excel; Dashboard tidak. **Fix:** tambah tombol Export Excel + action controller (reuse pola `ExportHistoriProton` / ClosedXML yang sudah dipakai di proyek). Export = isi tabel "Team Deliverable Progress" sesuai filter aktif (section/unit/kategori/track).
- **G-10 (Low) — Dashboard tanpa search.** CoachingProton/HistoriProton/CoachCoacheeMapping punya search box; Dashboard tidak. **Fix:** tambah search box client-side filter tabel "Team Deliverable Progress" (by Name/Track). Konsisten pola search page lain.

### Plan 02 — Navigasi (G-05, G-09)

- **G-05 (Med) — Deliverable balik salah.** `Views/CDP/Deliverable.cshtml`: breadcrumb "IDP Plan / Deliverable" + tombol "Kembali ke IDP Plan" → `/CDP/PlanIdp`. Padahal entry dari CoachingProton ("Lihat Detail") atau Dashboard drill. **Fix:** breadcrumb + tombol balik mengarah ke page asal (pakai referrer / query param `returnUrl` aman/whitelisted, fallback ke CoachingProton). JANGAN hardcode PlanIdp.
- **G-09 (Low) — CertificationManagement breadcrumb.** `Views/CDP/CertificationManagement.cshtml`: dicapai dari Admin/Home, tapi breadcrumb "CDP / Certification Management" + tombol "Kembali ke CDP" → `/CDP/Index` (hub yang tak punya kartu Cert). **Fix:** sesuaikan breadcrumb + tombol balik ke entry sebenarnya (Admin/Home). Opsi alternatif (kalau lebih cocok): tambah kartu Cert di hub CDP — putuskan saat eksekusi, default = arahkan balik ke Admin.

### Plan 03 — Role (G-12)

- **G-12 (Low) — Export gating tak seragam.** `Controllers/CDPController.cs` action `ExportHistoriProton` (~L3269) = `[Authorize(Roles = UserRoles.RolesReviewerAndAbove)]`, sementara export Coaching (`ExportProgressExcel`/`ExportCoachingTracking`/Bottleneck) = `RolesCoachAndAbove`. Coach bisa export coaching tapi tidak histori coachee-nya. **Fix (user-confirmed = bug):** turunkan `ExportHistoriProton` ke `[Authorize(Roles = UserRoles.RolesCoachAndAbove)]`. Pastikan scoping data di action tetap benar (Coach cuma lihat coachee ter-map — verifikasi query scope tidak bocor lintas-coach).

## Success Criteria (what must be TRUE)

1. Dashboard: chart "Deliverable Status" RENDER tanpa console error (G-01).
2. Dashboard: tombol Export Excel ADA + menghasilkan file sesuai filter (G-04).
3. Dashboard: search box memfilter tabel Team Deliverable Progress (G-10).
4. Deliverable: tombol/breadcrumb balik mengarah ke page asal (CoachingProton/Dashboard), bukan PlanIdp (G-05).
5. CertificationManagement: breadcrumb/tombol balik konsisten dengan entry Admin (G-09).
6. Coach (impersonate) bisa akses `ExportHistoriProton`; scope data tetap terbatas coachee ter-map (G-12).
7. `dotnet build` 0 error + `dotnet test` hijau + UAT lokal `localhost:5277` (CLAUDE.md Develop Workflow). Migration = none.

## Testing

- **xUnit:** G-12 role-attribute (reflection cek `[Authorize]` di `ExportHistoriProton` = RolesCoachAndAbove) — pola test reflection sudah ada di proyek (Phase 341/344).
- **Playwright (live @5277):** G-01 (chart render + 0 console error), G-04 (export klik), G-10 (search filter), G-05 (klik dari CoachingProton → Deliverable → balik ke CoachingProton), G-09 (nav cert), G-12 (impersonate Coach → ExportHistoriProton 200).
- Data DB shared berubah-ubah (sesi v25.0 paralel) → siapkan data UAT / impersonate saat eksekusi; jangan andalkan ID hardcode.

## Out of scope

- G-02, G-06 (dibatalkan — lihat atas).
- G-03, G-08 (v25.0 358/359).
- Modul Renewal, perubahan logika kelulusan/bypass.
- Refactor besar Dashboard di luar 3 gap-nya.

## Eksekusi

- Inline / subagent (putuskan di writing-plans). Aman vs sesi paralel: file yang disentuh (Dashboard.cshtml, Deliverable.cshtml, CertificationManagement.cshtml, CDPController.cs) BERBEDA dari file sesi 359 (AssessmentAdminController.cs, ProtonYearGate). **Cek konflik sebelum commit** (CDPController.cs perlu perhatian kalau sesi lain juga sentuh).
- ROADMAP.md Phase 362 entry ditambah HANYA setelah sesi 359 commit-nya settle.

## File rujukan terverifikasi

- `Views/CDP/Dashboard.cshtml` (chart init ~L560, canvas `protonStatusChart`)
- `Views/Admin/CoachWorkload.cshtml` (pola Chart.js benar, canvas `workloadChart`)
- `Views/CDP/Deliverable.cshtml` (breadcrumb/tombol balik PlanIdp)
- `Views/CDP/CertificationManagement.cshtml` (breadcrumb CDP)
- `Controllers/CDPController.cs` (`Dashboard`, `ExportHistoriProton` ~L3269, `ExportProgressExcel`/`ExportCoachingTracking` pola export ClosedXML)
- `Models/UserRoles.cs` (`RolesCoachAndAbove`, `RolesReviewerAndAbove`)
