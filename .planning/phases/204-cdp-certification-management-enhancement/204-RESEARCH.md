# Phase 204: CDP Certification Management Enhancement - Research

**Researched:** 2026-03-19
**Domain:** ASP.NET Core MVC — server-side filtering, partial view rendering, JavaScript toggle pattern
**Confidence:** HIGH

## Summary

Phase 204 adalah enhancement murni pada halaman CDP CertificationManagement yang sudah ada. Tidak ada endpoint baru, tidak ada model database baru — semua perubahan terbatas pada logika filter di CDPController dan rendering di partial view + view utama.

Field `IsRenewed` sudah tersedia di `SertifikatRow` dan sudah diisi oleh `BuildSertifikatRowsAsync`. Summary card counts (`ExpiredCount`, `AkanExpiredCount`) dihitung dari `allRows` sebelum pagination — di sinilah filter `!IsRenewed` perlu ditambahkan. Toggle "Tampilkan Riwayat Renewal" adalah checkbox/switch HTML yang mengontrol visibilitas baris di tabel, bukan filter server-side — implementasi client-side paling tepat karena seluruh data sudah ada di DOM.

**Primary recommendation:** Filter `IsRenewed` di controller untuk counts cards; toggle client-side JS hide/show baris dengan `opacity: 0.5` untuk renewed rows.

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- Baris sertifikat yang sudah di-renew ditampilkan dengan opacity 50% (redup) saat toggle aktif
- Toggle "Tampilkan Riwayat Renewal" diposisikan di atas tabel, sejajar dengan filter existing
- Label toggle: "Tampilkan Riwayat Renewal"
- Toggle default state: OFF (sertifikat renewed tersembunyi secara default)
- Card Expired hanya menghitung sertifikat expired yang belum di-renew
- Card Akan Expired juga exclude sertifikat yang sudah di-renew (konsisten)
- Angka card TIDAK berubah saat toggle ON — card selalu menampilkan yang belum di-renew saja
- Card Aktif tidak filter renewed — sertifikat Aktif yang sudah di-renew tetap dihitung sebagai Aktif

### Claude's Discretion
Tidak ada area discretion — semua keputusan sudah locked.

### Deferred Ideas (OUT OF SCOPE)
Tidak ada deferred ideas.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CDP-01 | Sertifikat yang sudah di-renew (ada renewal lulus) default tersembunyi di tabel | Field `IsRenewed` sudah ada di `SertifikatRow`; render semua baris tapi `display:none` + class `renewed-row` pada baris dengan `IsRenewed==true` |
| CDP-02 | Toggle "Tampilkan riwayat" untuk show/hide sertifikat yang sudah di-renew | Toggle checkbox di filter bar — JS event listener toggle class/style pada `.renewed-row` |
| CDP-03 | Summary card Expired hanya menghitung sertifikat yang belum di-renew | Ubah `allRows.Count(r => r.Status == Expired)` menjadi `allRows.Count(r => r.Status == Expired && !r.IsRenewed)` — sama untuk AkanExpired |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.x (existing) | Controller actions + Razor views | Existing stack |
| Bootstrap 5 | 5.x (existing) | Form check/switch component untuk toggle | Already used sitewide |
| Vanilla JS | ES2015+ | Toggle hide/show rows | No new library needed — simple DOM op |

### Tidak Perlu Library Baru
Semua kebutuhan phase ini terpenuhi dengan stack yang sudah ada.

## Architecture Patterns

### Alur Data Existing (dipahami sebelum modifikasi)

```
CertificationManagement GET
  → BuildSertifikatRowsAsync()         ← mengisi IsRenewed per row
  → allRows.Count(r => r.Status == Expired)    ← PERLU DIUBAH
  → vm.Rows = paginated subset         ← semua rows (termasuk IsRenewed)
  → View(vm) + PartialAsync(table)

FilterCertificationManagement GET
  → BuildSertifikatRowsAsync()
  → apply filter params
  → allRows.Count(r => r.Status == Expired)    ← PERLU DIUBAH
  → PartialView(table)
```

### Pattern 1: Filter Count di Controller (CDP-03)

**Apa:** Ubah count calculation untuk `ExpiredCount` dan `AkanExpiredCount` di dua tempat:
1. `CertificationManagement` action (~line 3060)
2. `FilterCertificationManagement` action (~line 3115)

**Contoh perubahan:**
```csharp
// SEBELUM
ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),

// SESUDAH
ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired && !r.IsRenewed),
AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired && !r.IsRenewed),
```

Note: `AktifCount` TIDAK diubah (per keputusan CONTEXT.md).

### Pattern 2: Render Renewed Rows dengan Atribut Data (CDP-01 + CDP-02)

**Apa:** Di `_CertificationManagementTablePartial.cshtml`, tambahkan class atau atribut `data-renewed` pada baris yang `IsRenewed == true`. Baris ini disembunyikan secara default.

**Contoh di partial:**
```cshtml
<tr class="@(row.IsRenewed ? "renewed-row" : "")"
    @(row.IsRenewed ? "style=\"display:none;\"" : "")>
```

Saat toggle ON: hapus `display:none` dan set `opacity: 0.5` pada `.renewed-row`.
Saat toggle OFF: tambahkan kembali `display:none` pada `.renewed-row`.

### Pattern 3: Toggle UI di Filter Bar (CDP-02)

**Apa:** Tambahkan form-check switch Bootstrap 5 di dalam `.card-body` filter bar, sejajar dengan filter lain.

**Di `CertificationManagement.cshtml` (filter bar row):**
```html
<div class="col-md-2 d-flex align-items-end">
    <div class="form-check form-switch mb-0">
        <input class="form-check-input" type="checkbox" id="toggle-renewed" role="switch">
        <label class="form-check-label small" for="toggle-renewed">Tampilkan Riwayat Renewal</label>
    </div>
</div>
```

**JS handler (di dalam IIFE existing):**
```javascript
var toggleRenewed = document.getElementById('toggle-renewed');
toggleRenewed.addEventListener('change', function() {
    var renewedRows = container.querySelectorAll('.renewed-row');
    renewedRows.forEach(function(row) {
        if (toggleRenewed.checked) {
            row.style.display = '';
            row.style.opacity = '0.5';
        } else {
            row.style.display = 'none';
            row.style.opacity = '';
        }
    });
});
```

### Anti-Patterns to Avoid

- **Jangan buat filter server-side baru untuk IsRenewed:** Toggle adalah pure client-side — semua baris sudah di-render di DOM. Membuat endpoint baru hanya untuk ini berlebihan dan merusak konsistensi.
- **Jangan ubah `TotalCount`:** Hanya `ExpiredCount` dan `AkanExpiredCount` yang diubah per keputusan. `TotalCount` tetap menghitung semua sertifikat.
- **Jangan reset toggle saat refreshTable:** `refreshTable()` mengganti innerHTML `container` — setelah replace, baris `.renewed-row` baru kembali ke state awal (hidden). Perlu re-apply toggle state setelah `refreshTable` selesai.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Toggle UI component | Custom CSS toggle | Bootstrap 5 `form-check form-switch` | Sudah ada di project, konsisten |
| Opacity styling | Custom class baru | Inline `style="opacity:0.5"` via JS | Sederhana, tidak perlu CSS class tambahan |

## Common Pitfalls

### Pitfall 1: Toggle State Hilang Setelah refreshTable
**What goes wrong:** Setiap kali filter berubah, `refreshTable()` mengganti `container.innerHTML` dengan HTML partial baru. Baris `.renewed-row` baru di-render dengan `display:none` (default). Jika toggle sedang ON saat filter berubah, renewed rows tetap hidden padahal toggle masih checked.

**How to avoid:** Setelah `container.innerHTML = html` di dalam `.then()`, cek state toggle dan apply ulang:
```javascript
.then(function(html) {
    container.innerHTML = html;
    wirePagination();
    updateSummaryCards();
    applyRenewedToggle();  // re-apply toggle state
    container.classList.remove('dashboard-loading');
});
```

Ekstrak logika toggle ke fungsi `applyRenewedToggle()` yang bisa dipanggil ulang.

### Pitfall 2: Summary Card Tidak Update Saat Toggle
**What goes wrong:** `updateSummaryCards()` membaca `data-expired` dari `#cert-table-content`. Jika data attribute ini masih menghitung including renewed rows, angka card akan salah.

**How to avoid:** Perubahan di controller (CDP-03) memastikan `ExpiredCount` yang dikirim ke ViewModel sudah benar. `data-expired` di partial akan memuat nilai yang sudah difilter. Tidak perlu perubahan di `updateSummaryCards()` JS.

### Pitfall 3: Nomor Baris (No) Berubah Saat Toggle ON
**What goes wrong:** Nomor urut baris dihitung dari index loop `i` — saat renewed rows disembunyikan, gap nomor akan terlihat (1, 2, 5, 6...).

**How to avoid:** Ini adalah perilaku yang dapat diterima — nomor baris menunjukkan posisi dalam dataset, bukan urutan tampil. Tidak perlu renumbering. Jika dikehendaki konsisten, gunakan CSS counter — tapi ini di luar scope phase ini.

### Pitfall 4: ExportSertifikatExcel Ikut Filter IsRenewed atau Tidak?
**What goes wrong:** Phase ini tidak membahas export — apakah export Excel harus exclude renewed rows atau tidak?

**How to avoid:** Scope phase ini hanya tabel display dan summary cards. Export Excel tidak diubah — tetap mengekspor semua rows tanpa filter IsRenewed. Ini konsisten dengan eksisting.

## Code Examples

### Lokasi Perubahan — Ringkasan

```
Controllers/CDPController.cs
  Line ~3060: ExpiredCount, AkanExpiredCount (CertificationManagement action)
  Line ~3115: ExpiredCount, AkanExpiredCount (FilterCertificationManagement action)

Views/CDP/CertificationManagement.cshtml
  Filter bar: tambah toggle switch HTML
  Script IIFE: tambah toggle handler + re-apply setelah refreshTable

Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
  Loop baris: tambah class="renewed-row" dan style="display:none" kondisional
```

### Re-apply Toggle Pattern (critical)
```javascript
function applyRenewedToggle() {
    var checked = document.getElementById('toggle-renewed').checked;
    container.querySelectorAll('.renewed-row').forEach(function(row) {
        row.style.display = checked ? '' : 'none';
        row.style.opacity = checked ? '0.5' : '';
    });
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test suite detected) |
| Config file | none |
| Quick run command | Manual: load `/CDP/CertificationManagement` |
| Full suite command | Manual: buka halaman, jalankan semua skenario di bawah |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CDP-01 | Sertifikat renewed tidak tampil saat halaman pertama dibuka | manual | Buka halaman, verifikasi tidak ada baris renewed tampil | N/A |
| CDP-02 | Toggle ON menampilkan renewed rows dengan opacity 50% | manual | Centang toggle, verifikasi baris muncul redup | N/A |
| CDP-03 | Card Expired tidak menghitung renewed | manual | Bandingkan count card vs jumlah baris expired non-renewed | N/A |

### Sampling Rate
- **Per task:** Load halaman di browser, verifikasi behavior spesifik task
- **Phase gate:** Semua 3 skenario manual di atas green sebelum `/gsd:verify-work`

### Wave 0 Gaps
Tidak ada — tidak ada test infrastructure yang perlu disiapkan. Phase ini sepenuhnya manual browser testing sesuai pola project.

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| ExpiredCount menghitung semua expired | ExpiredCount exclude IsRenewed | Card angka akurat |
| Semua baris rendered dan visible | Renewed rows hidden by default | UI lebih bersih |

## Open Questions

Tidak ada open questions — semua keputusan sudah locked di CONTEXT.md dan kode existing sudah siap.

## Sources

### Primary (HIGH confidence)
- Kode existing: `Controllers/CDPController.cs` line 3044-3127 — dibaca langsung
- Model: `Models/CertificationManagementViewModel.cs` — dibaca langsung, `IsRenewed` terkonfirmasi ada
- View: `Views/CDP/CertificationManagement.cshtml` — dibaca langsung, filter bar dan JS IIFE dipahami
- Partial: `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — dibaca langsung, struktur baris dipahami
- CONTEXT.md: keputusan design locked

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — stack existing, tidak ada library baru
- Architecture: HIGH — kode dibaca langsung, titik perubahan teridentifikasi dengan pasti
- Pitfalls: HIGH — diturunkan dari analisis kode actual (refreshTable innerHTML replace, toggle state)

**Research date:** 2026-03-19
**Valid until:** Selama kode CDPController dan partial tidak berubah signifikan
