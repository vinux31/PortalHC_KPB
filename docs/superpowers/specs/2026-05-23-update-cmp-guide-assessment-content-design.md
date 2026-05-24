# Update Page CMP Guide — Konten Assessment Terbaru

**Status:** READY — semua 5 section locked 2026-05-24. Siap invoke `writing-plans`.
**Trigger:** User minta update `/Home/GuideDetail?module=cmp` + `Panduan-Lengkap-Assessment.html` dengan data aktual baru. Tambah tutorial: cara buat assessment, upload package question, tipe assessment, tipe package question.

---

## Konteks awal

- Page CMP dirender dari `Views/Home/GuideDetail.cshtml` + data `Services/GuideContentProvider.cs`.
- Saat ini 6 accordion CMP + 1 PDF card public ("Panduan Lengkap Assessment").
- PDF admin sudah ada terpisah: `Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` — di-link di page Data (`module=data`), role-gated AdminHC.
- Pattern accordion role-gating sudah dipakai (contoh: "Monitoring Records Tim" Atasan+AdminHC).

## Decisions yang sudah disepakati

| # | Topik | Keputusan |
|---|-------|-----------|
| Q1 | Sumber konten | **B** — reverse-engineer dari source terbaru. Tidak ada draft user. |
| — | Visibility PDF coachee | **Public** — semua role lihat di page CMP. Konfirmasi: `GuideContentProvider.cs:802` `Roles: RoleGroup.All`. |
| Q2 | Pemisahan info admin vs coachee | **Pisah 2 dokumen** (PDF coachee public + PDF admin AdminHC). Tidak digabung jadi 1 PDF public. |
| — | Tambah tutorial via accordion | Pakai accordion existing di page CMP, role-gated. Pattern sudah ada. |
| Q3 | PDF admin posisi | **Opsi 2** — pindah dari page Data → page CMP (semua hal assessment terpusat di CMP). |
| Q4 (proton) | Coachee tahu Proton Tahun 1-3? | **b** — Tidak. Cuma Pre/Post/Regular saja. Tidak sebut Proton sama sekali di accordion publik. |
| Q4 (depth) | Detail accordion baru | **B** — Full step-by-step 4-6 step (mirror PDF). Self-contained, user gak perlu buka PDF. |
| Q5 | PDF slot CMP — admin lihat 2 PDF? | **A** (locked 2026-05-24) — Refactor `GetPdf` → `GetPdfs` list. View loop. Admin lihat 2 PDF card. |

## Pending decisions

(none — all locked)

## Penemuan dari audit kode (data aktual)

### Tipe Assessment (`AssessmentSession`)

- `AssessmentType`: "PreTest" / "PostTest" / null (regular)
- `Category` legacy: "Assessment OJ", "IHT", "Licencor", "OTS", "Mandatory HSSE Training"
- `Category` Proton: "Assessment Proton" (Phase 53)
- Sub-kategori hierarchy via `AssessmentCategory.ParentId` (Phase 195)
- Per-kategori signatory: `SignatoryUserId` (Phase 195)
- `TahunKe` (Proton only): "Tahun 1" / "Tahun 2" (online MC) / "Tahun 3" (offline interview manual)
- `IsManualEntry`: HC upload sertifikat existing (bukan online ujian)
  - Field tambahan: `Penyelenggara`, `Kota`, `SubKategori`, `CertificateType` ("Kompetensi"/"Profesi"/"Pelatihan")

### Tipe Package Question (`PackageQuestion.QuestionType`)

- "MultipleChoice" (default, backward compat null = MC)
- "MultipleAnswer" (Phase 296)
- "Essay" — `Rubrik` (kunci jawaban referensi HC) + `MaxCharacters` default 2000, grading manual oleh HC

### Fitur Admin baru belum di PDF lama

- Manage Categories (CRUD + signatory + sub-category)
- Multi-package per assessment (Paket A/B/C, `PackageNumber`)
- Per-user shuffle (display-only letters A/B/C/D, grading pakai `PackageOption.Id`)
- Pre-Post linking: `LinkedSessionId`, `LinkedGroupId`, `SamePackage` flag
- Extra Time per session: `ExtraTimeMinutes` (Phase 302) — accessibility
- Edit peserta answers + `AssessmentEditLog` audit
- AkhiriUjian / AkhiriSemuaUjian
- Renewal chain: `RenewsSessionId` / `RenewsTrainingId` (Phase 200)
- Real-time monitoring via SignalR (`Hubs/AssessmentHub.cs`)
- Audit Log (separate route)
- Reshuffle package per-session atau bulk
- Token akses: shared per batch (intentional design)
- Manual Entry sertifikat (HC upload existing certificate, bukan online ujian)

### Existing PDF status

- **Panduan-Lengkap-Assessment.html** (629 baris, 17 section): cover, TOC, alur, step 1-13 (akses CMP → daftar → token → mulai → kerjakan → auto-save → resume → review → submit → auto-submit → hasil → riwayat → sertifikat), tips, FAQ. **Terakhir update April 2026 (commit ee228193).**
- **Panduan-Admin-Buat-Assessment-dan-Input-Soal.html** (268 baris): alur, A. Buat Assessment (4 langkah), B. Buat Paket Soal, C. Import Soal (Excel upload + paste), D. Preview & Verifikasi, Tips.

---

## Draft plan execution (Opsi 2 + C hybrid)

| # | Aksi | File |
|---|------|------|
| 1 | Pindah PDF admin Module `Data` → `Cmp` | `Services/GuideContentProvider.cs:814` |
| 2 | (Pending Q5) Refactor `GetPdf` jadi list ATAU skip card ATAU URL hardcoded | depends Q5 |
| 3 | Refresh PDF coachee: tambah Tipe Assessment (Pre/Post/Regular), Pre-Post flow, Essay UI, Extra Time notice | `Panduan-Lengkap-Assessment.html` |
| 4 | Refresh PDF admin: tambah Manage Categories, Essay/MA types + rubrik, Pre-Post linking + SamePackage, Renewal, Extra Time, Manual Entry, Monitoring SignalR, Audit Log, Reshuffle, Edit peserta answers, Akhiri ujian | `Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` |
| 5 | Tambah accordion CMP role All: "Tipe-tipe Assessment" (PreTest, PostTest, Regular) — full 4-6 step | `GuideContentProvider.cs` |
| 6 | Tambah accordion CMP role AdminHC: "Tipe Package Question" (MC/MA/Essay) | `GuideContentProvider.cs` |
| 7 | Tambah accordion CMP role AdminHC: "Cara Buat Assessment" (full step) | `GuideContentProvider.cs` |
| 8 | Tambah accordion CMP role AdminHC: "Cara Upload Package Question" (full step) | `GuideContentProvider.cs` |
| 9 | Tambah accordion CMP role AdminHC: "Cara Manage Kategori Assessment" (full step) | `GuideContentProvider.cs` |

## Sections design — ALL LOCKED 2026-05-24

- ✅ Section 1/5: Architecture — Q5=A refactor `GetPdf` → `GetPdfs` list
- ✅ Section 2/5: 6 accordion baru
- ✅ Section 3/5: PDF coachee refresh — 19 section, insert posisi natural
- ✅ Section 4/5: PDF admin refresh — 11 section A-J keep
- ✅ Section 5/5: Verification 6 layer + 4 commit atomic

---

## Section 1 final — Architecture

Refactor `GetPdf(GuideModule, string) → GuidePdfLink?` jadi `GetPdfs(GuideModule, string) → IReadOnlyList<GuidePdfLink>`.

5 file touch:
1. `Services/GuideContentProvider.cs:856` — method rename + body `.Where(...).ToList()`
2. `Services/GuideContentProvider.cs:883` — `GetModuleCards` ItemCount: `+ GetPdfs(m.Module, userRole).Count`
3. `Models/Guide/GuideDetailViewModel.cs` — field `Pdf: GuidePdfLink?` → `Pdfs: IReadOnlyList<GuidePdfLink>`
4. `Controllers/HomeController.cs:406` — `Pdf: GetPdf(...)` → `Pdfs: GetPdfs(...)`
5. `Views/Home/GuideDetail.cshtml:33` — `@if (Model.Pdf != null)` → `@foreach (var pdf in Model.Pdfs)`

PDF admin row pindah: `Module: GuideModule.Data` → `Module: GuideModule.Cmp` (line 814).

---

## Section 2 final — 6 Accordion Baru

| ID | Title | Role | Steps |
|----|-------|------|-------|
| Acc-5 | Tipe-tipe Assessment | All | 3 (PreTest, PostTest, Regular — no Proton mention) |
| Acc-6 | Tipe Package Question | AdminHC | 4 (MC, MA, Essay, kapan pakai apa) |
| Acc-7 | Cara Buat Assessment | AdminHC | 6 (akses → kategori → detail → link Pre-Post → peserta → publish) |
| Acc-8 | Cara Upload Package Question | AdminHC | 5 (manage paket → metode input → format Excel → preview → shuffle) |
| Acc-9 | Cara Manage Kategori Assessment | AdminHC | 4 (buka → parent + signatory → sub-kategori → edit/hapus) |
| Acc-10 | Fitur Khusus Admin | AdminHC | 6 (Manual Entry, Extra Time, Reshuffle, Akhiri Ujian, Edit Jawaban, Renewal) |

Detail step content: lihat conversation log dispassign session, atau regenerate dari source code via reverse-engineer (Q1=B).

Q4(proton)=b → no Proton/TahunKe mention di Acc-5.
Q4(depth)=B → full 4-6 step per accordion (self-contained, no PDF dependency).

Coachee total view: 6 existing + Acc-5 = 7 accordion + 1 PDF.
AdminHC total view: 6 existing + Acc-5..10 = 12 accordion + 2 PDF.

---

## Section 3 final — PDF Coachee Refresh

File `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html` (629 baris → est ~780 baris).

Existing 17 section → new 19 section. Renumber posisi natural (Opsi A).

Changes:
- **C1** INSERT new sec 1.5 "Tipe-tipe Assessment" (Pre/Post/Regular, mirror Acc-5 public)
- **C2** INSERT new sec 1.6 "Pre-Test vs Post-Test" (SamePackage info, kapan paket sama vs beda)
- **C3** AUGMENT sec "Memulai Ujian" → append warning box "Catatan Extra Time" (timer auto-tambah, gak perlu lapor)
- **C4** AUGMENT sec "Mengerjakan Soal" → INSERT subsection "Tipe Soal yang Akan Dijumpai" (MC, MA, Essay UI)
- **C5** AUGMENT FAQ → tambah 2 Q (kapan hasil Essay keluar; kenapa paket Post-Test sama dgn Pre-Test)

Existing anchor `#step1..#step13` preserved (renumber section heading number, anchor ID tetap).

Bump header timestamp ke Mei 2026.

---

## Section 4 final — PDF Admin Refresh

File `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` (268 baris → est ~600 baris).

Letter rescheme A→J. Existing 5 section → 11 section.

| Letter | Section | Status | Konten ringkas |
|--------|---------|--------|----------------|
| Alur | Alur Proses | UPDATE | Note: Manage Kategori dulu kalau kategori baru |
| **A** | Manage Kategori Assessment | NEW | CRUD kategori, sub-kategori (`ParentId`), signatory (`SignatoryUserId`) |
| B (was A) | Buat Assessment Baru | UPDATE | Langkah 3 Settings tambah: Pre-Post linking, SamePackage, Extra Time, Renewal |
| C (was B) | Buat Paket Soal | UPDATE | Multi-package (Paket A/B/C, `PackageNumber`) |
| D (was C) | Import Soal | UPDATE | Excel kolom: `QuestionType`, `Rubrik`, `MaxCharacters` |
| E (was D) | Preview & Verifikasi | UPDATE | Preview Essay & MA |
| **F** | Tipe Soal Detail | NEW | MC, MA, Essay + rubrik internal, grading manual |
| **G** | Manual Entry Sertifikat | NEW | Upload existing tanpa ujian: Penyelenggara, Kota, SubKategori, CertificateType, `IsManualEntry=true` |
| **H** | Operasional Sesi Berjalan | NEW | SignalR `AssessmentHub`, Reshuffle, Akhiri Ujian, Edit jawaban peserta |
| **I** | Renewal Chain | NEW | `RenewsSessionId`, `RenewsTrainingId`, link sertifikat baru ke lama |
| **J** | Audit Log | NEW | `AssessmentEditLog` + audit log umum |
| Tips | Tips Penting | UPDATE | Essay grading workflow + signatory inheritance |

---

## Section 5 final — Verification

6 layer:
1. **Build** — `dotnet build` pass no warning baru
2. **Code** — `GetPdfs` returns expected counts per role/module (admin Cmp=2, coachee Cmp=1, Data=0)
3. **Browser localhost:5277** — manual scenario admin@pertamina.com + coachee + atasan + coach across /GuideDetail?module=cmp|data
4. **PDF render** — 19 section coachee + 11 section admin, TOC link, anchor preserved
5. **Playwright** — automated assert PDF card count per role + accordion content
6. **UAT pak Rino** — visual confirm

4 commit atomic:
- **Commit 1:** Refactor `GetPdf` → `GetPdfs` + pindah PDF admin Module Data→Cmp (5 file)
- **Commit 2:** Add 6 accordion (Acc-5..10) di `GuideContentProvider.cs`
- **Commit 3:** PDF coachee refresh (`Panduan-Lengkap-Assessment.html`)
- **Commit 4:** PDF admin refresh (`Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`)

Rollback per-commit kalau ada masalah specific layer.

---

## Next steps

1. Invoke `superpowers:writing-plans` → generate PLAN.md executable
2. Execute via subagent-driven-development atau manual per-commit
3. UAT pak Rino di browser localhost
4. Commit + push, IT promo Dev/Prod

## File refs (paths verified)

- `Services/GuideContentProvider.cs` (930 baris)
- `Views/Home/GuideDetail.cshtml` (93 baris)
- `Controllers/AssessmentAdminController.cs` (6167 baris)
- `Controllers/CMPController.cs` (4697 baris)
- `Models/AssessmentSession.cs` (193 baris)
- `Models/AssessmentPackage.cs` (81 baris — termasuk PackageQuestion + PackageOption)
- `Models/AssessmentCategory.cs` (30 baris)
- `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html` (629 baris)
- `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html` (268 baris)
- `Hubs/AssessmentHub.cs` (real-time monitoring)
