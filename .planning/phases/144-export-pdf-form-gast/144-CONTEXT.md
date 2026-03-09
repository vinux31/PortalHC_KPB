# Phase 144: Export PDF Form GAST - Context

**Gathered:** 2026-03-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign `DownloadEvidencePdf` di CDPController agar menghasilkan PDF sesuai layout Form Coaching GAST Pertamina. Menggantikan layout vertikal label-value saat ini dengan 3-column table landscape. Bergantung pada Phase 143 (data Acuan dari CoachingSession).

</domain>

<decisions>
## Implementation Decisions

### Orientasi & ukuran kertas
- Landscape A4 (bukan portrait seperti sekarang)
- Margin standar (~1.5-2cm)

### Header (di luar tabel)
- Kanan atas: Logo Pertamina KPI dari `psign-pertamina.png`
- Kiri atas: Tanggal coaching (format Indonesia)

### Layout tabel 3 kolom

**Kolom Kiri — ACUAN:**
- Urutan dari atas ke bawah: Kompetensi, Sub Kompetensi, Deliverable
- Label "Acuan" di bawah Deliverable
- 4 sub-field Acuan: Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen
- Masing-masing sub-field sebagai baris terpisah dengan label bold

**Kolom Tengah — CATATAN COACH (EXISTING):**
- Area besar untuk menampilkan free text `CatatanCoach` dari CoachingSession
- 1 cell besar (tidak dibagi-bagi)

**Kolom Kanan — KESIMPULAN DARI COACH:**
- Dibagi 3 bagian vertikal dari atas ke bawah:
  1. **Kesimpulan** — checkbox vertikal:
     - ☑/☐ Kompeten
     - ☑/☐ Perlu Pengembangan
     (centang sesuai value `Kesimpulan` di DB)
  2. **Result** — checkbox vertikal:
     - ☑/☐ Need Improvement
     - ☑/☐ Suitable
     - ☑/☐ Good
     - ☑/☐ Excellence
     (centang sesuai value `Result` di DB)
  3. **TTD Coach** — kotak P-Sign berisi:
     - Logo Pertamina (psign-pertamina.png)
     - Role + Position + Unit
     - FullName
     - NIP (tampilkan "-" jika kosong di DB)

### Footer branding
- Kiri: Red wave — di-generate pakai QuestPDF (bentuk trapezoid/shape merah melebar dari kiri) + teks putih "ptkpi.pertamina.com"
- Kanan: Logo Call Center 135 dari `logo-135.png`

### Proporsi kolom
- Claude's discretion — sesuaikan yang paling pas secara visual

### Penanganan data kosong
- Field kosong di Acuan: tampilkan "-"
- NIP coach kosong: tampilkan "-"
- CatatanCoach kosong: tampilkan "-"

### Claude's Discretion
- Proporsi lebar 3 kolom
- Exact font sizes dan spacing
- Red wave shape detail (trapezoid angle, ukuran)
- Border styling tabel
- Exact P-Sign box layout dalam kolom kanan

</decisions>

<specifics>
## Specific Ideas

- Referensi visual dari image Form GAST Pertamina yang di-upload user: header logo kanan atas, footer red wave kiri + Call Center 135 kanan
- Logo header dan P-Sign pakai file yang sama: `psign-pertamina.png` (KILANG PERTAMINA BALIKPAPAN)
- Red wave di footer NOT dari file image — di-generate pakai QuestPDF Canvas/shape API agar bisa melebar penuh dari kiri
- Checkbox pakai Unicode characters (☑ U+2611 / ☐ U+2610) dalam QuestPDF text

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `QuestPDF` package sudah terinstall — tidak perlu dependency baru
- `DownloadEvidencePdf` action di `CDPController.cs` (line ~2268) — action yang akan di-redesign
- `psign-pertamina.png` di `wwwroot/images/` — logo Pertamina KPI untuk header dan P-Sign
- `logo-135.png` di `wwwroot/images/` — logo Call Center 135 untuk footer
- `logo-ptkpi.png` di `wwwroot/images/` — backup jika QuestPDF red wave tidak memadai
- `PSignViewModel` di `Models/PSignViewModel.cs` — model untuk P-Sign data

### Established Patterns
- QuestPDF: `Document.Create → container.Page → page.Content().Column()` pattern
- `MemoryStream → File()` return untuk PDF download
- `ApplicationUser` punya field: FullName, NIP, Position, Section, Unit, RoleLevel
- `CoachingSession` punya: CatatanCoach, Kesimpulan ("Kompeten"/"Perlu Pengembangan"), Result ("Need Improvement"/"Suitable"/"Good"/"Excellence")
- Phase 143 menambahkan 4 field Acuan ke CoachingSession: Pedoman, TkoTkiTkpa, BestPractice, Dokumen

### Integration Points
- `CDPController.DownloadEvidencePdf(int progressId)` — method yang di-replace in-place
- Access control logic tetap sama (sudah ada)
- Data loading (progress, session, coach info) tetap sama, tambah load Acuan fields
- Download button di `Views/CDP/Deliverable.cshtml` sudah ada, tidak perlu diubah

</code_context>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase.

</deferred>

---

*Phase: 144-export-pdf-form-gast*
*Context gathered: 2026-03-09*
