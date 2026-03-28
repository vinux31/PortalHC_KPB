# Phase 270: Perbaiki Resume Exam - Research

**Researched:** 2026-03-28
**Domain:** ASP.NET Core MVC — JavaScript exam flow (StartExam.cshtml)
**Confidence:** HIGH

## Summary

Phase ini adalah perubahan kecil terfokus pada dua bagian di `StartExam.cshtml`:

1. **Modal `resumeConfirmModal`** — sederhanakan konten: hapus teks "soal no. X" dan elemen `#resumePageNum`, tinggalkan hanya tombol "Lanjutkan".
2. **Redirect saat resume** — ubah agar selalu navigate ke page 0 (halaman pertama) bukan ke `RESUME_PAGE`.

Tidak ada perubahan di controller, model, atau database. Hanya perubahan HTML dan JavaScript di satu file view.

**Primary recommendation:** Edit dua area di `StartExam.cshtml` — modal HTML (line ~182-202) dan blok JS resume (line ~794-812). Tidak perlu library baru, tidak perlu backend change.

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Modal resume disederhanakan — cukup tampilkan teks "Lanjutkan" tanpa info nomor soal atau detail lainnya. Hapus kalkulasi `resumeFromNum` dan elemen `resumePageNum`.
- **D-02:** Tombol tetap "Lanjutkan" dengan styling yang sudah ada.
- **D-03:** Saat worker klik "Lanjutkan" di modal resume, selalu redirect ke page 1 (index 0), bukan ke `LastActivePage`. Worker bisa navigasi sendiri ke soal yang belum dijawab.

### Claude's Discretion
- Styling/layout modal boleh disesuaikan selama tetap simpel dan konsisten dengan design system yang ada.

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada deferred ideas.
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap Modal | 5.x (sudah ada) | Modal dialog | Sudah dipakai di project |
| Vanilla JS | — | DOM manipulation | Sudah dipakai di StartExam.cshtml |

**Installation:** Tidak ada instalasi baru — semua sudah tersedia.

## Architecture Patterns

### File yang Berubah
```
Views/CMP/StartExam.cshtml   ← satu-satunya file yang diubah
```

### Pattern yang Dipakai
- Bootstrap Modal `data-bs-backdrop="static"` — modal tidak bisa ditutup klik luar (sudah ada, tetap dipertahankan)
- `document.getElementById('resumeConfirmBtn').addEventListener('click', ...)` — event listener on button click
- `currentPage` variable + `document.getElementById('page_' + currentPage)` — navigasi halaman exam

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Modal dialog | Custom overlay | Bootstrap Modal (sudah ada) | Konsisten dengan design system |

## Code Examples

### Modal HTML Saat Ini (line ~182-202)
```html
<!-- State saat ini — perlu disederhanakan -->
<div class="modal-body pt-2">
    <p class="mb-0">Lanjutkan dari soal no. <strong id="resumePageNum">--</strong>?</p>
</div>
```

### Modal HTML Target (setelah perubahan D-01)
```html
<!-- Hanya teks simpel, tanpa id="resumePageNum" -->
<div class="modal-body pt-2">
    <p class="mb-0">Anda memiliki ujian yang belum selesai. Klik Lanjutkan untuk melanjutkan.</p>
</div>
```

### JS Resume Logic Saat Ini (line ~794-812)
```javascript
// State saat ini — perlu diubah
} else if (IS_RESUME && RESUME_PAGE > 0) {
    var resumeFromNum = answeredQuestions.size > 0
        ? answeredQuestions.size + 1
        : RESUME_PAGE * QUESTIONS_PER_PAGE + 1;
    document.getElementById('resumePageNum').innerText = resumeFromNum;
    const resumeModal = new bootstrap.Modal(document.getElementById('resumeConfirmModal'));
    resumeModal.show();

    document.getElementById('resumeConfirmBtn').addEventListener('click', function() {
        resumeModal.hide();
        // Navigate to last active page
        document.getElementById('page_0').style.display = 'none';
        currentPage = RESUME_PAGE;
        document.getElementById('page_' + currentPage).style.display = 'block';
        updatePanel();
        window.scrollTo(0, 0);
    });
```

### JS Resume Logic Target (setelah D-01 + D-03)
```javascript
// Kondisi diperluas — tampilkan modal untuk SEMUA resume (termasuk RESUME_PAGE == 0)
} else if (IS_RESUME) {
    const resumeModal = new bootstrap.Modal(document.getElementById('resumeConfirmModal'));
    resumeModal.show();

    document.getElementById('resumeConfirmBtn').addEventListener('click', function() {
        resumeModal.hide();
        // Selalu ke page 0 (halaman pertama)
        updatePanel();
        window.scrollTo(0, 0);
    });
```

**Catatan penting:** Kondisi `else if (IS_RESUME && RESUME_PAGE > 0)` harus diubah menjadi `else if (IS_RESUME)` agar modal juga muncul saat worker resume dari page 0. Kalau kondisi dibiarkan `RESUME_PAGE > 0`, resume dari halaman pertama tidak menampilkan modal sama sekali (langsung masuk tanpa konfirmasi).

Perlu juga dipastikan bahwa `else` branch di bawahnya (`// Normal start or resume from page 0: just render panel`) tetap hanya untuk non-resume (IS_RESUME = false).

## Common Pitfalls

### Pitfall 1: Kondisi `RESUME_PAGE > 0` tidak dihapus
**What goes wrong:** Modal resume tidak muncul ketika worker resume dari halaman pertama (RESUME_PAGE = 0).
**Why it happens:** Kondisi lama mengecek `RESUME_PAGE > 0` karena logika lama ingin navigasi ke halaman non-nol. Setelah D-03 (selalu ke page 0), kondisi ini tidak relevan lagi.
**How to avoid:** Ubah kondisi menjadi `else if (IS_RESUME)` saja.

### Pitfall 2: `else` branch di bawah tetap memanggil `updatePanel()`
**What goes wrong:** Jika tidak diperhatikan, `else` branch (normal start) masih diperlukan untuk non-resume.
**How to avoid:** Jangan hapus `else { updatePanel(); }` — itu untuk fresh start, bukan resume.

### Pitfall 3: Elemen `resumePageNum` masih direferensi di JS tapi sudah dihapus dari HTML
**What goes wrong:** JavaScript error `Cannot set property 'innerText' of null`.
**How to avoid:** Hapus baris `document.getElementById('resumePageNum').innerText = resumeFromNum;` dan seluruh kalkulasi `resumeFromNum` bersamaan dengan menghapus `id="resumePageNum"` dari HTML.

## Validation Architecture

Tidak ada test framework otomatis. Verifikasi dilakukan secara manual oleh user di browser (sesuai keputusan milestone v10.0).

### Manual Verification Steps
| Skenario | Expected Behavior |
|----------|-------------------|
| Resume ujian dari halaman non-pertama | Modal muncul, hanya teks simpel + tombol "Lanjutkan", tanpa info nomor soal |
| Klik "Lanjutkan" | Modal tutup, tampil halaman 1 (soal 1-10) |
| Resume ujian dari halaman pertama | Modal tetap muncul (bukan langsung masuk) |
| Fresh start (bukan resume) | Tidak ada modal, langsung tampil halaman 1 |

## Environment Availability

Perubahan murni frontend (HTML + JS) — tidak ada external dependency baru.

## Sources

### Primary (HIGH confidence)
- `Views/CMP/StartExam.cshtml` line 182-202 — modal HTML yang ada saat ini (dibaca langsung)
- `Views/CMP/StartExam.cshtml` line 794-812 — JS resume logic yang ada saat ini (dibaca langsung)
- `270-CONTEXT.md` — keputusan user D-01, D-02, D-03

## Metadata

**Confidence breakdown:**
- Scope: HIGH — CONTEXT.md sangat spesifik, kode sudah dibaca langsung
- Implementasi: HIGH — perubahan minimal, pattern sudah jelas
- Pitfalls: HIGH — diidentifikasi dari kode aktual

**Research date:** 2026-03-28
**Valid until:** Tidak ada batas waktu — perubahan stabil, tidak ada dependency eksternal
