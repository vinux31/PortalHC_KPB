# Design: Judul Assessment Fleksibel + Cek Duplikat

**Tanggal:** 2026-06-15
**Status:** Approved (brainstorming) → menuju writing-plans
**Urgensi:** URGENT — safe but fast
**Proses:** Superpowers (writing-plans → executing-plans). BUKAN GSD phase (v29 CLOSED, tak ada milestone aktif; scope kecil 2 file, 0 migration).

---

## 1. Masalah

Validator regex di `Controllers/AssessmentAdminController.cs:871-879` (asal **Phase 339, milestone v20.0**) memaksa SEMUA judul assessment standard-mode ikut pola `^(Pre|Post)\s*Test\s+.+$`.

```csharp
// L871-879 — BIANG MASALAH
if (AssessmentTypeInput != "PrePostTest"
    && !string.IsNullOrEmpty(model.Title)
    && !System.Text.RegularExpressions.Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$"))
{
    ModelState.AddModelError("Title",
        "Title harus pola '{Stage} Test {Track} {Lokasi}' ...");
}
```

- **Asal aturan:** insiden "PreTest OJT GAST Cilacap hilang" — judul lama `OJT GAST - GTO & SRU RU IV` tak punya prefix Pre/Post + tak ada kota → search "Cilacap" 0 hit. Solusi waktu itu paksa pola.
- **Dampak buruk:** scope kekecilan. Judul sah seperti `training lisensor SRU` (kategori Training Licencor), CMP, IHT ketolak.
- **Gap nyata:** TIDAK ada cek judul kembar sama sekali → admin bisa bikin 2 assessment judul sama → 2 set sertifikat (double cert).

## 2. Fakta kode (terverifikasi)

| Fakta | Bukti |
|-------|-------|
| Validator cuma di 1 tempat (CreateAssessment POST) | grep `Regex.IsMatch(model.Title)` → hanya `AssessmentAdminController.cs:874` |
| Sistem grup assessment by `(Title, Category, Schedule.Date)` | `AssessmentAdminController.cs:179`, `:2870` |
| `AssessmentSession` = 1 baris **per peserta** | `Models/AssessmentSession.cs:10` (`UserId`) |
| PrePost pakai `Title = model.Title` **verbatim** untuk Pre & Post | `:1220`, `:1256` (dibedakan `AssessmentType` + `Schedule`, BUKAN judul) |
| Renewal **pre-fill** judul dari sumber | `:711`, `:740`, `:786`, `:814` (`model.Title = sourceX.Title`) |
| Flag `isRenewalModePost` tersedia di POST | `:977` |
| Auto-pair `TryAutoDetectCounterpartGroup` opportunistic | `:7082` — cuma set `LinkedGroupId` kalau ketemu; judul fleksibel = tinggal tak nyala, tak rusak |
| Normalizer kanonik sudah ada | `:6268` (+`CMPController.cs:1318`): `Regex.Replace(s.Trim(), @"\s+"," ").ToLowerInvariant()` |

**Konsekuensi penting:** karena PrePost pakai judul verbatim, dup-check standard + PrePost = logika identik (cek `model.Title`). Tak perlu antisipasi judul turunan.

## 3. Keputusan (dari brainstorming)

1. **Fleksibel = hapus validator total** (L871-879). Judul bebas apa saja.
2. **Cek duplikat = tombol manual + soft-block saat save** (bisa dikonfirmasi, bukan hard-lock).
3. **Lingkup "kembar" = judul persis sama** (case-insensitive + normalisasi whitespace) **lintas semua kategori**.
4. **Scope hard-guard v1 = standard + PrePost** (skip renewal). Manual entry & EditAssessment = residual (defer).

## 4. Perubahan (2 file, 0 migration, 0 threat surface baru)

### A. Fleksibel — `Controllers/AssessmentAdminController.cs`
- **Hapus** blok validator L871-879.
- Blok auto-pair L857-869 **dibiarkan** (fungsi beda, tak memblok save).

### B. Helper normalisasi (reuse) — `AssessmentAdminController.cs`
- Pakai/extract normalizer kanonik existing (`:6268`) untuk konsistensi:
  `NormalizeTitle(s) => Regex.Replace(s.Trim(), @"\s+"," ").ToLowerInvariant()`.

### C. Endpoint cek duplikat (baru) — `AssessmentAdminController.cs`
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CheckTitleAvailability(string title)
{
    if (string.IsNullOrWhiteSpace(title))
        return Json(new { exists = false, groupCount = 0, matches = new object[0] });

    var norm = NormalizeTitle(title);
    // GroupBy distinct (Title, Category, Schedule.Date) → ToList → normalisasi+banding di memory
    // (normalizer C#-only, tak EF-translatable; jumlah grup distinct kecil)
    var groups = await _context.AssessmentSessions
        .GroupBy(a => new { a.Title, a.Category, Date = a.Schedule.Date })
        .Select(g => new { g.Key.Title, g.Key.Category, g.Key.Date, Peserta = g.Count() })
        .ToListAsync();
    var matches = groups.Where(g => NormalizeTitle(g.Title) == norm)
        .Select(g => new { g.Category, tanggal = g.Date, g.Peserta })
        .ToList();

    return Json(new { exists = matches.Count > 0, groupCount = matches.Count, matches });
}
```
- GET read-only → tak butuh antiforgery. Pola `return Json(...)` sudah dipakai luas (`:2638`, `:3464`, dst).

### D. Tombol UI — `Views/Admin/CreateAssessment.cshtml` (Langkah 1, blok Title L183-194)
- Tombol **"Cek Judul"** di sebelah input Judul (`#Title`).
- JS `fetch('/AssessmentAdmin/CheckTitleAvailability?title=...')` → render inline di container baru:
  - Hijau: `✓ Aman — judul belum dipakai`
  - Kuning: `⚠ Judul dipakai di N assessment` + daftar (kategori · tanggal · jumlah peserta)
- Manual (klik). Tidak otomatis. Tidak ganggu navigasi wizard (`goToStep`/`validateStep`).

### E. Soft-block saat save — controller + view
- Param POST baru: `bool ConfirmDuplicateTitle = false`.
- **Penempatan:** SETELAH `isRenewalModePost` dihitung (`:977`) — supaya flag renewal kebaca. Berlaku standard + PrePost (TIDAK pakai guard `!isPrePostMode`).
```csharp
if (!string.IsNullOrWhiteSpace(model.Title)
    && !isRenewalModePost          // renewal sengaja reuse judul → skip
    && !ConfirmDuplicateTitle)
{
    var norm = NormalizeTitle(model.Title);
    var groups = await _context.AssessmentSessions
        .GroupBy(a => new { a.Title, a.Category, Date = a.Schedule.Date })
        .Select(g => new { g.Key.Title, g.Key.Category, g.Key.Date, Peserta = g.Count() })
        .ToListAsync();
    var matches = groups.Where(g => NormalizeTitle(g.Title) == norm).ToList();
    if (matches.Count > 0)
    {
        ModelState.AddModelError("Title",
            $"Judul '{model.Title}' sudah dipakai di {matches.Count} assessment. " +
            "Centang konfirmasi di bawah untuk tetap membuat dengan judul sama.");
        ViewBag.DuplicateTitleWarning = true;
    }
}
```
- **View:** kalau `ViewBag.DuplicateTitleWarning == true` → render checkbox di Langkah 1:
  `<input type="checkbox" name="ConfirmDuplicateTitle" value="true"> Tetap buat walau judul kembar`.
- Error ModelState otomatis balik render di Langkah 1 (terbukti dari screenshot insiden). Admin centang → submit ulang lolos.

## 5. Alur akhir (UX)

1. Admin isi judul bebas (mis. `training lisensor SRU`) → **lolos** (validator dihapus).
2. (Opsional) klik **Cek Judul** → lihat aman/dipakai sebelum lanjut.
3. Submit:
   - Judul belum dipakai → save normal.
   - Judul kembar + belum konfirmasi → soft-block, balik ke Langkah 1, muncul checkbox konfirmasi.
   - Centang konfirmasi → submit ulang → save (sengaja dibuat kembar, mis. batch beda).
   - Mode renewal → soft-block di-skip (judul memang reuse sertifikat asal).

## 6. Di luar scope (residual — defer)

- **Manual entry** (`IsManualEntry`) — kemungkinan action terpisah, juga bikin sertifikat. Tombol Cek tetap mendeteksi judulnya, tapi hard-guard save tak dipasang di sana (v1).
- **EditAssessment** — kalau judul bisa diedit jadi nabrak. Tak dicakup v1.
- **Double-space internal** pada judul: ditangani oleh `NormalizeTitle` (collapse `\s+`). Tidak residual.

## 7. Verifikasi (per DEV_WORKFLOW)

- `dotnet build` → 0 error.
- `dotnet test` → baseline 18/18 PASS (no regression).
- `dotnet run` → `http://localhost:5277`.
- Playwright UAT:
  1. Judul bebas non-Pre/Post (`training lisensor SRU`) → save sukses (validator hilang).
  2. Tombol Cek → hijau (judul baru) & kuning (judul existing, daftar benar).
  3. Save judul kembar tanpa konfirmasi → ke-block, balik Langkah 1, error + checkbox muncul.
  4. Centang konfirmasi → save lolos.
  5. PrePost mode judul kembar → ke-block juga (scope standard+PrePost).
  6. Renewal mode judul reuse → TIDAK ke-block (skip).
  7. Regression: auto-pair Pre/Post (`TryAutoDetectCounterpartGroup`) masih jalan saat judul match pola.

## 8. Keamanan

- Endpoint baru `[Authorize(Roles = "Admin, HC")]` (sama dgn CreateAssessment). GET read-only, tak ubah state → tak butuh antiforgery.
- Soft-block = defensive hardening (kurangi risiko double-cert), bukan nambah attack surface.
- 0 perubahan entity (`Models/AssessmentSession.cs` UNTOUCHED), 0 migration.
