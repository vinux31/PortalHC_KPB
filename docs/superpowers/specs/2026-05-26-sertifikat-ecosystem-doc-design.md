# Sertifikat Ecosystem Documentation — Design Spec

**Tanggal:** 2026-05-26
**Topik:** Single-file HTML reference dokumentasi teknis ecosystem sertifikat Portal HC KPB
**Audience:** Developer (project Portal HC KPB)
**Deliverable:** `docs/sertifikat-ecosystem/index.html`

---

## 1. Tujuan

Membuat satu file HTML standalone yang mendokumentasikan secara teknis seluruh ecosystem sertifikat di Portal HC KPB — meliputi 13 endpoint/page utama, 9 tabel database terkait, state machine sertifikat, alur generasi & renewal, RBAC matrix, hasil static audit (bug & risiko), gap analysis (fungsi/sistem/logic), serta cross-check spec ↔ kode.

Pembaca target: developer baru, code reviewer, atau penanggung jawab teknis yang perlu memahami secara cepat keseluruhan domain sertifikat tanpa menjelajah controller satu per satu.

## 2. Lingkup (Scope)

### In-Scope

13 endpoint/page yang menyentuh data sertifikat (10 page utama + 3 endpoint kritis):

**Page Utama (10):**
1. `/CMP/Records` — Riwayat Pelatihan
2. `/CDP/CertificationManagement` — Manajemen Sertifikasi
3. `/CMP/BudgetTraining` — Budget Training
4. `/Admin/ManageAssessment` — Manage Assessment & Training (tab Assessment / Training / History)
5. `/Admin/RenewalCertificate` — Certificate Renewal
6. `/CMP/Certificate/{id}` — Generate Certificate (view)
7. `/CMP/CertificatePdf/{id}` — PDF Export
8. `/Admin/AddTraining` & `/Admin/EditTraining` — Upload Bukti Sertifikat
9. `/Notification/*` — Reminder Expiry
10. `/CMP/ExportRecords` + `/CDP/ExportSertifikatExcel` + `/CMP/ExportSertifikatExcel` — Export

**Endpoint Kritis Tambahan (3):**
11. `/CMP/SubmitExam` — trigger cert generation
12. Grade Essay endpoint (lokasi TBD saat audit) — transisi `PendingGrading` → `Completed`
13. CDP Coaching Dashboard (L5 view sertifikat coachee)

**Data Model (9 tabel):**
`TrainingRecord`, `AssessmentSession`, `AssessmentCategory`, `BudgetItem`, `ApplicationUser`, `Notification`, `CoachCoacheeMappings`, `OrganizationUnit`, `UserPackageAssignment`.

### Out-of-Scope

- Modul Coaching murni (CoachingSession, OJT, BigMenu) — hanya menyinggung saat sertifikat ter-cross-cut.
- Modul Assessment authoring (Question Package, Item Bank) — kecuali bagian yang mempengaruhi generasi sertifikat.
- Penulisan ulang/refactor kode — file ini murni dokumentasi, bukan implementasi.
- Roadmap perbaikan — gap & bug dilaporkan tanpa rencana eksekusi.

## 3. Stack & Konvensi Teknis

- **Single-file standalone HTML.** Tidak ada split, tidak ada build step.
- **Bootstrap 5.3** via CDN, **Mermaid 10.x** via CDN (diagram), **highlight.js** via CDN (code block).
- **Tidak ada framework JS** (no React/Vue). Plain HTML + minimal vanilla JS untuk: sticky TOC scroll-spy, dark/light toggle (optional), collapsible card.
- **Bahasa:** Bahasa Indonesia untuk narasi & section title. Istilah teknis & code identifier tetap bahasa asli (route, controller, FK, dsb).
- **Sticky sidebar TOC** kiri (desktop) — collapse jadi off-canvas drawer di mobile.
- **Self-contained.** Tidak depend asset internal (gambar/screenshot). Jika perlu visual, pakai Mermaid atau SVG inline.
- **File size budget:** ~3000–4000 baris HTML. Single file tetap dibuka cepat di browser.

## 4. Struktur Section (18 section)

| # | Section | Konten Inti |
|---|---------|-------------|
| 0 | Header & Meta | Title, scope, audience, last-updated, generated-by, commit hash |
| 1 | Executive Summary | TL;DR ecosystem; badge stats (10 page, 9 tabel, 5 state) |
| 2 | Data Model | Mermaid `erDiagram` 9 tabel + relasi FK + dual-FK renewal; tabel kolom kunci |
| 3 | State Machine Sertifikat | Mermaid `stateDiagram-v2` 7 state: Draft/InProgress → PendingGrading → Failed / NoCert / Issued → Aktif → AkanExpired → Expired → Renewed → Permanent (terminal) |
| 4 | Core Flow Diagrams | 8 flow sequence (Submit→Cert, Upload Manual, Renewal, Status Derivation, Export, Budget Import, Notification Dispatch, Cert# Generation Race) |
| 5 | Per-Page Deep Dive | 10 page utama + 3 endpoint kritis; tiap card: route, controller `path:line`, view, ViewModel, read/write sources, mini-sequence, RBAC |
| 6 | RBAC Matrix Global | Tabel 13 endpoint × 6 role + 4 dimensi (full / section-scoped / dual-mode / own-data) |
| 7 | Status Derivation Logic | Code `DeriveCertificateStatus()` + truth table (CertificateType × ValidUntil → Status) + edge case |
| 8 | Renewal Chain Algorithm | Dual-FK explained, Union-Find pseudocode, contoh chain visualisasi, cycle detection |
| 9 | Bugs & Issues (Static Audit) | Per finding: severity (🔴/🟡/🟢), location `path:line`, problem, suggested fix |
| 10 | Gap Analysis | 3 sub-tabel: Gap Fungsi / Gap Sistem / Gap Logic |
| 11 | Spec ↔ Code Cross-Check | Tabel: klaim spec vs kode aktual + mismatch + lokasi |
| 12 | Glossary | PROTON, CMP, CDP, BP, KKJ, IHT, OJT, NSO, OTS, KPB, dll |
| 13 | Migration Timeline | Phase 195 (signatory), 200 (renewal chain), 311–313, 320 (assessment power tools) |
| 14 | Test Coverage Map | Playwright test yang ada vs gap sertifikat flow |
| 15 | External Dependency | QuestPDF (PDF), ClosedXML (Excel), Bootstrap Icons CDN, version pin |
| 16 | Performance Hotspot | Renewal chain Union-Find per request, N+1 risk, index DB column `ValidUntil` |
| 17 | API/AJAX Endpoint Catalog | `/Notification/UnreadCount`, `/CMP/BudgetTrainingBulkUpdateRealisasi`, dll |
| 18 | Appendix | Route→Controller flat map; DB column→Page usage matrix; konstanta penting |

**Catatan dis-overlap:**
- §4 vs §5 sequence: §4 = high-level core flow lintas-page; §5 = mini-sequence per-page (tidak duplikasi).
- §3 vs §7: §3 = visual state machine; §7 = code + truth table.
- §4.3 vs §8: §4.3 = user-action flow renewal; §8 = algorithm internal Union-Find.

## 5. Metodologi Audit (untuk §9, §10, §11)

### 5.1 Static Code Audit (sumber §9 — bugs)

Scan target di seluruh controller/model terkait sertifikat. Pattern yang dicari:

1. Null-safety pada `ValidUntil`, `CertificateType`, `SertifikatUrl`.
2. RBAC bocor: L4 section scoping konsisten di semua endpoint?
3. Race condition cert number generation (`KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}`) — sequence allocation atomicity.
4. N+1 query di renewal chain dashboard.
5. Mismatch UTC vs WIB (`DateTime.UtcNow` vs `DateTime.Now`) — derivasi status.
6. Renewal cycle detection (A renews B, B renews A) — risiko infinite loop Union-Find.
7. Status derivation edge case: Permanent + null `ValidUntil`, AkanExpired tepat = 30 hari (boundary `<=` vs `<`).
8. File upload validation: mime type, size cap, path traversal pada `SertifikatUrl`.
9. Authorization bypass pada `CertificatePdf` (direct id enumeration).
10. Orphan FK setelah delete (`RenewsTrainingId` → record dihapus).
11. Concurrency `BudgetItem` bulk update (lost update).
12. Soft-delete vs hard-delete impact integritas renewal chain.

### 5.2 Spec ↔ Code Cross-Check (sumber §11)

Sumber dokumentasi yang dibandingkan terhadap kode aktual:

- `CLAUDE.md` (project instructions)
- `docs/DEV_WORKFLOW.md`
- `docs/SEED_WORKFLOW.md`
- `docs/SEED_JOURNAL.md`
- `docs/superpowers/specs/2026-05-21-merge-sosialisasi-aplikasi-to-internal-hc-design.md`
- Spec sosialisasi-internal-hc v2.2 (Phase 200 renewal claim, dll)
- Spec PCP HCPortal §3.4 v3.0
- EF Core migration files Phase 195/200/311–313/320

Output: tabel klaim vs realita + lokasi mismatch.

### 5.3 Gap Analysis (sumber §10)

Tiga dimensi gap:

- **Gap Fungsi** — fitur yang user/HC kemungkinan harapkan tapi belum ada (bulk renewal action, auto-email reminder, cert revocation, cert verification publik via QR, dll).
- **Gap Sistem** — infra/architecture (no background job untuk reminder, no audit log table, no caching, no rate-limit pada export, dll).
- **Gap Logic** — edge case derivasi (null `ValidUntil` ambiguity, cycle detection, timezone, monthly SEQ reset semantics, dll).

## 6. Konvensi Visual

- **Severity badge:** 🔴 high, 🟡 medium, 🟢 low.
- **Status badge:** Open, Known, Fixed, Won't-Fix.
- **Code reference:** format `path:line` di seluruh dokumen (clickable via `file://` di local — informasi only, tidak harus berfungsi sebagai link).
- **Diagram:** Mermaid lebih dipilih dari image static (bisa di-edit ulang text-based).
- **Collapsible card** untuk konten besar (tabel kolom lengkap, code block panjang).

## 7. Kriteria Selesai (Definition of Done)

1. File `docs/sertifikat-ecosystem/index.html` ada dan dibuka tanpa error console.
2. 18 section terisi semua — tidak ada placeholder `TBD`/`TODO`.
3. Semua diagram Mermaid render dengan benar.
4. 13 endpoint/page punya minimum: route, controller `path:line`, view, ViewModel, RBAC.
5. Section §9 (Bug) berisi minimum hasil scan ke-12 pattern di §5.1.
6. Section §10 (Gap) berisi minimum 3 item per dimensi (fungsi/sistem/logic).
7. Section §11 (Cross-Check) berisi minimum klaim dari 8 sumber di §5.2.
8. TOC sidebar navigable & scroll-spy berfungsi.
9. File dibuka mandiri tanpa koneksi internet? — tidak (CDN dipakai). Catat dependency CDN di §15.
10. Commit dengan message konvensi proyek (lihat git log).

## 8. Risiko & Mitigasi

| Risiko | Mitigasi |
|--------|----------|
| File terlalu besar (>4000 baris) sulit di-load editor | Collapsible card; jika tetap besar, pisah ke sub-file (revisit) |
| Mermaid CDN block di lingkungan offline | Catat di §15; opsi fallback PNG export jika prod offline |
| Audit miss finding karena scope luas | Section §9 explicit listed 12 pattern; bisa di-extend nanti |
| Klaim cross-check obsolete saat kode berubah | `last-updated` di header + commit hash; dokumen tetap snapshot, bukan live |
| Mismatch path `path:line` setelah refactor | Sertakan keterangan "snapshot tanggal X"; tidak warranty live |

## 9. Out-of-Scope (eksplisit)

- Tidak ada perbaikan kode dari hasil audit — murni dokumentasi.
- Tidak ada migrasi data atau seed baru.
- Tidak ada test baru.
- Tidak ada modifikasi UI Portal HC aktif.
- Tidak ada integrasi CI untuk regen dokumen otomatis.

## 10. Hasil & Hand-off

Setelah file selesai:
1. Commit ke `main` (lokal) dengan tag opsional `sertifikat-ecosystem-doc-v1.0`.
2. Push origin/main (manual oleh user, sesuai DEV_WORKFLOW).
3. Update `MEMORY.md` dengan entri proyek baru.
4. Tidak ada deploy — dokumen statis lokal.
