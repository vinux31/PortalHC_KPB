# Phase 352: Data Foundation + Image-Only Upload - Context

**Gathered:** 2026-06-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Pondasi data + validasi upload gambar untuk fitur "gambar di soal assessment". Tiga deliverable:
1. **Migration** 4 kolom nullable: `PackageQuestions.ImagePath`, `PackageQuestions.ImageAlt`, `PackageOptions.ImagePath`, `PackageOptions.ImageAlt`.
2. **Entity** — tambah properti gambar di `Models/AssessmentPackage.cs` (`PackageQuestion` L27, `PackageOption` L63).
3. **Image-only upload validation** (REQ IMG-04) — varian validasi khusus gambar (JPG/PNG, tolak PDF/non-image, magic-byte) di `Helpers/FileUploadHelper.cs` + konstanta di `Models/AssessmentConstants.cs`.

TIDAK termasuk di phase ini: form admin, controller wiring, render, sync. Itu phase 353-355.
</domain>

<decisions>
## Implementation Decisions

### Format Gambar
- **D-01:** Hanya **JPG/PNG** yang diterima (termasuk `.jpeg`). TIDAK terima WebP/GIF/HEIC. Alasan: paling kompatibel, magic-byte sudah ada, admin export PPT→PNG. Konsekuensi: screenshot HP format WebP/HEIC harus dikonversi admin dulu (accepted).
- **D-02:** Allowed extension set image-only = `{ .jpg, .jpeg, .png }` — mirror cert set TANPA `.pdf`. Magic-byte reuse entri jpg/jpeg/png yang sudah ada di `AssessmentConstants.MagicBytes` (tak perlu byte baru).

### Batas Ukuran
- **D-03:** Maksimal **5MB** per gambar. **OVERRIDE spec §4/§6 + REQ IMG-01/02 yang tertulis "≤2MB"** — keputusan user 2026-06-06 naik ke 5MB (akomodasi diagram teknis hi-res + foto kamera HP tanpa kompres). Planner WAJIB pakai 5MB; teks REQUIREMENTS.md IMG-01/02 sudah disesuaikan ke 5MB. Tambah konstanta baru `MaxImageFileSizeBytes = 5 * 1024 * 1024` (jangan pakai `MaxCertificateFileSizeBytes` 10MB).

### Kompresi/Resize
- **D-04:** **Simpan apa adanya** — TIDAK ada server-side resize/re-encode. Reuse `FileUploadHelper.SaveFileAsync` 100% (format-agnostic, sudah auto-create folder). Andalkan `img-fluid` + `loading="lazy"` di browser (phase 354). Catatan: SkiaSharp ADA di proyek (Phase 320 spider chart) tapi sengaja TIDAK dipakai untuk kompres di sini — prioritas simpel.

### Validasi API (Claude's Discretion — planner putuskan bentuk)
- **D-05:** Bentuk method image-only diserahkan planner: opsi (a) method baru `ValidateImageFile(IFormFile?)` mirror `ValidateCertificateFile`, atau (b) parameterize `ValidateCertificateFile` dengan allowed-set + max-size argument. Rekomendasi: method baru terpisah (lebih jelas, tak ubah call-site cert existing). Magic-byte check + `read < 3` guard + reset `stream.Position=0` HARUS dipertahankan identik.

### Folder & Kolom (locked dari spec, planner detail)
- **D-06:** Folder upload `/uploads/questions/{packageId}/` (per-package, dari spec §6). `ImageAlt` = `nvarchar(255)` (cukup untuk alt ≤100 char best-practice). `ImagePath` = `nvarchar(max)` null.

### Claude's Discretion
- Nama persis method/konstanta, urutan kolom migration, apakah test pakai fixture byte inline atau file — planner/executor putuskan.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec utama (source of truth fitur)
- `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md` — desain lengkap 13 section. **§4** (data design 4 kolom), **§6** (upload + Gap 4 helper image-only), **§10** (keamanan). ⚠️ §4/§6 sebut 2MB — DIOVERRIDE ke 5MB per D-03.

### Pola existing yang di-reuse / di-mirror
- `Helpers/FileUploadHelper.cs` — `ValidateCertificateFile` L13-39 (pola validasi magic-byte yang di-mirror untuk image-only) + `SaveFileAsync` L46-79 (reuse apa adanya) + `DeleteFile` L85-93.
- `Models/AssessmentConstants.cs` §FileValidation L30-74 — `AllowedCertificateExtensions` L37, `MaxCertificateFileSizeBytes` L32, `MagicBytes` dict L45-51, `MatchesMagicByte` L59-73 (semua di-reuse/extend untuk image-only).
- `Models/AssessmentPackage.cs` — `PackageQuestion` L27, `PackageOption` L63 (target tambah properti).
- `Data/ApplicationDbContext.cs` L52-56 + L454-474 (DbSet + relationship PackageQuestion/PackageOption).

### Workflow wajib
- `CLAUDE.md` Develop Workflow — verifikasi lokal `dotnet build` + `dotnet ef database update` + localhost:5277 sebelum commit. ❌ tak ada edit Dev/Prod. Flag migration ke IT saat shipped.
- `docs/SEED_WORKFLOW.md` — TIDAK relevan (ini kolom DDL, bukan seed data). Snapshot DB lokal sebelum apply migration tetap kebiasaan baik.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `FileUploadHelper.SaveFileAsync` — pakai langsung untuk simpan gambar; sudah handle path-traversal strip + unique filename + folder create.
- `AssessmentConstants.MagicBytes` — sudah punya signature jpg/jpeg/png; image-only validation tinggal pakai.
- `MatchesMagicByte` helper — reuse untuk cek header.

### Established Patterns
- Validasi = method statik return `(bool IsValid, string? Error)` pesan Bahasa Indonesia (pola `ValidateCertificateFile`).
- Konstanta validasi terpusat di `AssessmentConstants.FileValidation`.
- Migration EF Core di `Migrations/` — wajib commit, flag IT.

### Integration Points
- Helper image-only akan dipanggil di Phase 353 (CreateQuestion/EditQuestion controller). Phase 352 hanya menyediakan method + konstanta + entity + migration; belum ada call-site.
</code_context>

<specifics>
## Specific Ideas

- 5MB cap eksplisit dipilih user di atas 2MB best-practice karena konten = diagram teknis kilang resolusi tinggi.
- No-compress dipilih demi kesederhanaan; trade-off berat load diterima.
</specifics>

<deferred>
## Deferred Ideas

- **Thumbnail / auto-resize** — bila storage/load jadi masalah setelah dipakai, pertimbangkan generate thumbnail atau server resize (SkiaSharp/ImageSharp). Bukan sekarang.
- **Dukungan WebP/HEIC** — bila admin sering upload dari HP, tambah format. Out untuk sekarang.
- **Kompres kualitas otomatis** — ditolak (risiko diagram/teks buram).

None lain — diskusi tetap dalam scope phase.
</deferred>

---

*Phase: 352-data-foundation-image-only-upload*
*Context gathered: 2026-06-06*
