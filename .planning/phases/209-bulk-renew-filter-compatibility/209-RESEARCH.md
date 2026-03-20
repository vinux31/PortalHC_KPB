# Phase 209: Bulk Renew & Filter Compatibility - Research

**Researched:** 2026-03-20
**Domain:** ASP.NET Core MVC — JavaScript DOM manipulation, Bootstrap modal, AJAX partial rendering
**Confidence:** HIGH (semua temuan dari kode aktual proyek)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Tombol Renew per Group**
- Tombol "Renew N Pekerja" muncul di dalam header accordion masing-masing group, bukan global
- Tombol global "Renew Selected" yang ada di atas filter bar DIHAPUS
- Tombol hanya muncul saat ada checkbox tercentang di group tersebut, hilang saat tidak ada

**Select-All & Checkbox Behavior**
- Checkbox di-lock per group: centang di group A → checkbox di group B-Z disabled
- Hapus logic lock per kategori (data-kategori) — diganti lock per group (data-group-key)
- `cb-group-select-all` checkbox di header group mencentang/uncentang semua checkbox di group-nya
- Saat navigasi pagination per group, checkbox di-reset (tidak persist lintas page)

**Konfirmasi Bulk Renew**
- Sebelum redirect ke CreateAssessment, tampilkan modal konfirmasi: "Anda akan me-renew N pekerja untuk sertifikat X. Lanjutkan?"
- Jika user klik "Lanjutkan" → redirect ke CreateAssessment dengan parameter
- Jika user klik "Batal" → modal tertutup, tidak ada aksi

**Filter + Grouped View**
- Group yang semua anggotanya terfilter keluar → group disembunyikan (tidak muncul sama sekali)
- Badge count di group header (N expired, N akan expired) update sesuai filter aktif
- Jika semua group tersembunyi karena filter → tampilkan pesan "Tidak ada sertifikat yang sesuai filter" dengan tombol "Reset Filter"

**Summary Cards**
- Summary cards (Expired count, Akan Expired count) menampilkan angka dari data terfilter (bukan total keseluruhan)
- Mekanisme `updateSummaryCards()` dari Phase 208 sudah tersedia

### Claude's Discretion
- Implementasi detail modal konfirmasi (reuse Bootstrap modal atau inline)
- Urutan disabled/enabled saat unlock group
- Animasi hide/show group saat filter berubah

### Deferred Ideas (OUT OF SCOPE)
None — diskusi tetap dalam scope phase.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| BULK-01 | Checkbox select-all per group untuk memilih semua pekerja dalam satu sertifikat | `cb-group-select-all` sudah ada di partial (line 9-10), tinggal wire JS di `wireCheckboxes()` |
| BULK-02 | Tombol "Renew N Pekerja" per group muncul saat ada checkbox tercentang | Perlu tambah tombol di `_RenewalGroupedPartial.cshtml` header dan fungsi JS per-group |
| FILT-01 | Filter Bagian/Unit/Kategori/Sub Kategori/Status tetap berfungsi pada tampilan grouped | `FilterRenewalCertificate` server-side sudah filter dan group — perlu pastikan empty-state "tidak ada sertifikat sesuai filter" |
| FILT-02 | Summary cards (Expired count, Akan Expired count) tetap dipertahankan dan update sesuai filter | `updateSummaryCards()` sudah berfungsi, hidden spans sudah tersedia di `_RenewalGroupedPartial.cshtml` |
</phase_requirements>

---

## Summary

Phase 209 adalah murni pekerjaan frontend/JS wiring di atas infrastruktur yang sudah dibangun di Phase 208. Tidak ada perubahan DB, ViewModel baru, atau endpoint baru yang diperlukan. Server-side filter sudah berfungsi dengan benar — `FilterRenewalCertificate` sudah mem-filter row dan me-group hasilnya, dan `FilterRenewalCertificateGroup` sudah meneruskan filter params ke pagination per group.

Pekerjaan utama ada tiga area: (1) refactor `wireCheckboxes()` dari lock-per-kategori ke lock-per-group, (2) tambah tombol renew per group di header accordion dan wire logikanya, (3) tambah modal konfirmasi sebelum bulk renew.

Filter compatibility (FILT-01, FILT-02) sudah 90% berfungsi dari Phase 208 — yang perlu ditambah adalah empty-state khusus filter ("Tidak ada sertifikat yang sesuai filter" + tombol Reset Filter), karena empty-state saat ini hanya menangani kasus "tidak ada data sama sekali" bukan "tidak ada hasil filter".

**Primary recommendation:** Satu plan tunggal 209-01 cukup — semua perubahan terpusat di dua file (RenewalCertificate.cshtml dan _RenewalGroupedPartial.cshtml) tanpa perubahan controller.

---

## Standard Stack

### Core (sudah ada di proyek)
| Komponen | Versi | Purpose | Catatan |
|----------|-------|---------|---------|
| Bootstrap 5 | 5.x | Modal, collapse, badge | `bootstrap.Modal` tersedia, pola `certificateHistoryModal` bisa di-reuse |
| Vanilla JS | ES5-compatible | DOM manipulation, AJAX | Semua kode existing menggunakan IIFE + vanilla JS |
| ASP.NET Core MVC | 6+ | Partial views, controller actions | Tidak ada perubahan controller diperlukan |

**Tidak ada library tambahan yang diperlukan.**

---

## Architecture Patterns

### Struktur File yang Terlibat

```
Views/Admin/
├── RenewalCertificate.cshtml          — Main page JS logic (wireCheckboxes, dsb)
└── Shared/
    ├── _RenewalGroupedPartial.cshtml  — Group accordion HTML (tambah tombol renew di header)
    └── _RenewalGroupTablePartial.cshtml — Per-group table (cb-group-select-all sudah ada)

Controllers/
└── AdminController.cs                 — FilterRenewalCertificate, FilterRenewalCertificateGroup
                                         (tidak perlu diubah)
```

### Pattern 1: Lock Per Group (menggantikan lock per kategori)

**Yang ada sekarang (harus dihapus):**
```javascript
// wireCheckboxes() — lock berdasarkan data-kategori
if (cb.checked) {
    var cat = cb.dataset.kategori || '';
    if (selectedKategori === null) { selectedKategori = cat; }
    checkboxes.forEach(function (other) {
        var otherCat = other.dataset.kategori || '';
        if (!other.checked && otherCat !== selectedKategori) {
            other.disabled = true;
        }
    });
}
```

**Yang harus diimplementasi (lock berdasarkan data-group-key):**
```javascript
// Variabel state — satu group key yang sedang aktif
var selectedGroupKey = null;

function wireCheckboxes() {
    var checkboxes = container.querySelectorAll('.cb-select');
    checkboxes.forEach(function (cb) {
        cb.addEventListener('change', function () {
            var groupKey = cb.dataset.groupKey || '';
            if (cb.checked) {
                if (selectedGroupKey === null) { selectedGroupKey = groupKey; }
                // Disable semua checkbox di group lain
                checkboxes.forEach(function (other) {
                    if (!other.checked && (other.dataset.groupKey || '') !== selectedGroupKey) {
                        other.disabled = true;
                    }
                });
            } else {
                var anyChecked = container.querySelectorAll('.cb-select:checked').length > 0;
                if (!anyChecked) {
                    selectedGroupKey = null;
                    checkboxes.forEach(function (other) {
                        other.disabled = false;
                    });
                }
            }
            updateGroupRenewButton(groupKey);
        });
    });
    // Wire cb-group-select-all
    wireGroupSelectAll();
}
```

### Pattern 2: Select-All Per Group

```javascript
function wireGroupSelectAll() {
    container.querySelectorAll('.cb-group-select-all').forEach(function (selectAll) {
        selectAll.addEventListener('change', function () {
            var groupKey = selectAll.dataset.groupKey;
            var groupCheckboxes = container.querySelectorAll(
                '.cb-select[data-group-key="' + groupKey + '"]'
            );
            // Jika ada group lain yang sedang aktif, abaikan
            if (selectedGroupKey !== null && selectedGroupKey !== groupKey) return;
            groupCheckboxes.forEach(function (cb) {
                if (!cb.disabled) cb.checked = selectAll.checked;
            });
            // Trigger update state
            if (selectAll.checked && groupCheckboxes.length > 0) {
                selectedGroupKey = groupKey;
                // Disable group lain
                container.querySelectorAll('.cb-select').forEach(function (other) {
                    if ((other.dataset.groupKey || '') !== groupKey && !other.checked) {
                        other.disabled = true;
                    }
                });
            } else if (!selectAll.checked) {
                var anyChecked = container.querySelectorAll('.cb-select:checked').length > 0;
                if (!anyChecked) {
                    selectedGroupKey = null;
                    container.querySelectorAll('.cb-select').forEach(function (other) {
                        other.disabled = false;
                    });
                }
            }
            updateGroupRenewButton(groupKey);
        });
    });
}
```

### Pattern 3: Tombol Renew Per Group (HTML di _RenewalGroupedPartial.cshtml)

Tombol ditambahkan di header accordion, setelah badge counts. Tombol di-render server-side di partial tapi awalnya hidden, kemudian JS yang show/hide-nya:

```html
<!-- Di _RenewalGroupedPartial.cshtml, dalam .card-header, setelah div badge -->
<button class="btn btn-warning btn-sm btn-renew-group d-none ms-auto me-2"
        data-group-key="@group.GroupKey"
        data-group-judul="@group.Judul"
        onclick="renewGroup('@group.GroupKey', '@group.Judul')">
    <i class="bi bi-arrow-repeat me-1"></i>Renew <span class="renew-count-@group.GroupKey">0</span> Pekerja
</button>
```

Perhatian: tombol berada di dalam `div[role="button"][data-bs-toggle="collapse"]` — klik tombol akan juga trigger collapse. Perlu `e.stopPropagation()` di event handler tombol.

### Pattern 4: Update Tombol Renew Per Group (JS)

```javascript
function updateGroupRenewButton(groupKey) {
    var checked = container.querySelectorAll('.cb-select[data-group-key="' + groupKey + '"]:checked').length;
    var btn = container.querySelector('.btn-renew-group[data-group-key="' + groupKey + '"]');
    var countSpan = container.querySelector('.renew-count-' + groupKey);
    if (!btn) return;
    if (checked > 0) {
        btn.classList.remove('d-none');
        if (countSpan) countSpan.textContent = checked;
    } else {
        btn.classList.add('d-none');
        if (countSpan) countSpan.textContent = '0';
    }
}
```

### Pattern 5: Reset Checkbox Saat Pagination Per Group

Di `refreshGroupTable()`, setelah `tableContainer.innerHTML = html`, tambahkan reset state:

```javascript
function refreshGroupTable(groupKey, judul, page) {
    // ... existing code ...
    .then(function (html) {
        tableContainer.innerHTML = html;
        // Reset checkbox state untuk group ini
        if (selectedGroupKey === groupKey) {
            selectedGroupKey = null;
            // Re-enable semua checkbox di group lain juga
            container.querySelectorAll('.cb-select').forEach(function (cb) {
                cb.disabled = false;
            });
        }
        // Update tombol renew group (hide)
        updateGroupRenewButton(groupKey);
        // Reset cb-group-select-all
        var selectAll = container.querySelector('.cb-group-select-all[data-group-key="' + groupKey + '"]');
        if (selectAll) selectAll.checked = false;
        wireCheckboxes();
        wirePagination();
    });
}
```

### Pattern 6: Modal Konfirmasi Bulk Renew

Reuse pola `certificateHistoryModal` yang sudah ada. Tambah modal baru di `RenewalCertificate.cshtml`:

```html
<!-- Bulk Renew Confirmation Modal -->
<div class="modal fade" id="bulkRenewConfirmModal" tabindex="-1" aria-labelledby="bulkRenewConfirmLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="bulkRenewConfirmLabel">
                    <i class="bi bi-arrow-repeat me-2 text-warning"></i>Konfirmasi Bulk Renew
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
            </div>
            <div class="modal-body" id="bulk-renew-confirm-body">
                Anda akan me-renew <strong id="bulk-renew-count">0</strong> pekerja untuk sertifikat
                <strong id="bulk-renew-judul"></strong>. Lanjutkan?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <button type="button" class="btn btn-warning" id="btn-bulk-renew-confirm">
                    <i class="bi bi-arrow-repeat me-1"></i>Lanjutkan
                </button>
            </div>
        </div>
    </div>
</div>
```

```javascript
var pendingRenewParams = null;

function renewGroup(groupKey, judul) {
    var checked = container.querySelectorAll('.cb-select[data-group-key="' + groupKey + '"]:checked');
    if (checked.length === 0) return;
    var params = new URLSearchParams();
    checked.forEach(function (cb) {
        if (cb.dataset.recordtype === 'Assessment') {
            params.append('renewSessionId', cb.dataset.sourceid);
        } else {
            params.append('renewTrainingId', cb.dataset.sourceid);
        }
    });
    pendingRenewParams = params.toString();
    document.getElementById('bulk-renew-count').textContent = checked.length;
    document.getElementById('bulk-renew-judul').textContent = judul;
    var modal = new bootstrap.Modal(document.getElementById('bulkRenewConfirmModal'));
    modal.show();
}
window.renewGroup = renewGroup;

document.getElementById('btn-bulk-renew-confirm').addEventListener('click', function () {
    if (pendingRenewParams) {
        window.location.href = '/Admin/CreateAssessment?' + pendingRenewParams;
    }
});
```

### Pattern 7: Empty State Filter (FILT-01)

`_RenewalGroupedPartial.cshtml` sudah punya empty state untuk "tidak ada data". Perlu dibedakan dua kasus:

```html
@if (Model.Groups.Count == 0)
{
    @if (Model.IsFiltered)
    {
        <!-- Empty karena filter aktif -->
        <div class="text-center py-5">
            <i class="bi bi-funnel fs-1 text-muted d-block mb-3"></i>
            <strong>Tidak ada sertifikat yang sesuai filter</strong>
            <p class="text-muted mt-1 mb-3">Coba ubah atau hapus filter untuk melihat semua sertifikat.</p>
            <button class="btn btn-outline-secondary" onclick="document.getElementById('btn-reset').click()">
                <i class="bi bi-x-circle me-1"></i>Reset Filter
            </button>
        </div>
    }
    else
    {
        <!-- Empty karena memang tidak ada data -->
        <div class="text-center py-5"> ... existing content ... </div>
    }
}
```

Untuk ini, `RenewalGroupViewModel` perlu properti `IsFiltered` (bool) yang di-set di controller ketika ada filter params aktif.

### Anti-Patterns yang Harus Dihindari

- **Jangan tambah endpoint baru**: Semua logic filter sudah ada di `FilterRenewalCertificate` dan `FilterRenewalCertificateGroup`
- **Jangan simpan checkbox state lintas pagination**: Reset adalah intended behavior sesuai keputusan user
- **Jangan taruh tombol renew di luar header accordion**: Keputusan locked — harus di dalam header
- **Jangan lupa stopPropagation**: Tombol renew di dalam `div[data-bs-toggle="collapse"]` harus hentikan event bubble agar tidak toggle accordion

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan | Kenapa |
|---------|-------------|---------|--------|
| Modal konfirmasi | Custom dialog HTML dari nol | Pola `certificateHistoryModal` yang sudah ada | Sudah ada modal structure Bootstrap di halaman |
| Cascade dropdown Bagian→Unit | Logic baru | `/CDP/GetCascadeOptions` yang sudah ada dan sudah di-wire | Sudah berfungsi |
| Cascade Kategori→Sub Kategori | Logic baru | `/CDP/GetSubCategories` yang sudah ada | Sudah berfungsi |

---

## Common Pitfalls

### Pitfall 1: Tombol Renew Memicu Toggle Accordion

**Masalah:** Tombol "Renew N Pekerja" berada di dalam `div.card-header[data-bs-toggle="collapse"]`. Klik tombol akan propagate ke header dan toggle accordion terbuka/tertutup.
**Cara menghindari:** Tambah `onclick="event.stopPropagation(); renewGroup(...)"` ATAU pasang event listener dengan `e.stopPropagation()` di tombol.
**Tanda peringatan:** Accordion menutup/membuka setiap kali tombol renew diklik.

### Pitfall 2: `wireCheckboxes()` Dipanggil Berulang Saat AJAX

**Masalah:** Setiap `refreshTable()` dan `refreshGroupTable()` memanggil `wireCheckboxes()` ulang. Jika tidak hapus event listener lama, checkbox bisa memiliki multiple listener.
**Cara menghindari:** Gunakan event delegation dari `container` (satu listener di parent) alih-alih listener per checkbox. Atau gunakan `cloneNode` / flag untuk pastikan tidak duplikat.
**Alternatif praktis:** Karena `container.innerHTML = html` menghapus DOM lama beserta event listener-nya, pemanggilan ulang `wireCheckboxes()` setelah set innerHTML adalah aman. Yang perlu dijaga: `selectedGroupKey` di luar scope tidak ikut ter-reset.

### Pitfall 3: State `selectedGroupKey` Tidak Sinkron Saat Filter Berubah

**Masalah:** Jika user mencentang checkbox di group A, lalu mengubah filter (yang memanggil `refreshTable()`), `selectedGroupKey` masih bernilai group A tapi group A sudah di-replace dengan HTML baru.
**Cara menghindari:** Pada `refreshTable()`, selalu reset `selectedGroupKey = null` dan sembunyikan semua tombol renew per group sebelum/sesudah render HTML baru. Pola ini sudah ada di `resetKategoriLock()` — ganti `selectedKategori = null` dengan `selectedGroupKey = null`.

### Pitfall 4: `cb-group-select-all` Tidak Ter-wire Setelah Pagination

**Masalah:** `refreshGroupTable()` memanggil `wireCheckboxes()` tapi jika `wireGroupSelectAll()` adalah fungsi terpisah, perlu dipastikan dipanggil juga.
**Cara menghindari:** Pastikan `wireCheckboxes()` memanggil `wireGroupSelectAll()` di dalamnya, sehingga satu panggilan cukup.

### Pitfall 5: `IsFiltered` Property Belum Ada di ViewModel

**Masalah:** `RenewalGroupViewModel` tidak punya properti `IsFiltered`. Empty state berbeda ("tidak ada data" vs "tidak ada hasil filter") perlu dibedakan di view.
**Cara menghindari:** Tambah `bool IsFiltered` ke `RenewalGroupViewModel` dan set di `FilterRenewalCertificate` action berdasarkan apakah ada filter param yang aktif.

---

## Code Examples

### Existing: `updateSummaryCards()` (dari RenewalCertificate.cshtml, line 383-394)

Sudah berfungsi — membaca hidden span dari partial dan update DOM:
```javascript
function updateSummaryCards() {
    var expiredSpan = container.querySelector('#partial-expired-count');
    var akanExpiredSpan = container.querySelector('#partial-akan-expired-count');
    if (expiredSpan) {
        var el = document.getElementById('count-expired');
        if (el) el.textContent = expiredSpan.textContent;
    }
    if (akanExpiredSpan) {
        var el2 = document.getElementById('count-akan-expired');
        if (el2) el2.textContent = akanExpiredSpan.textContent;
    }
}
```
Fungsi ini dipanggil di `refreshTable()` — berarti FILT-02 sudah otomatis berfungsi, tidak perlu perubahan tambahan.

### Existing: GroupKey Encoding (dari AdminController.cs, line 6987-6989)

```csharp
GroupKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g.Key))
                 .Replace("+", "_").Replace("/", "-").Replace("=", "")
```
GroupKey digunakan sebagai HTML id suffix — aman dari karakter spesial.

### Existing: Bootstrap Modal Pattern (dari RenewalCertificate.cshtml, line 400-411)

```javascript
function openHistoryModal(workerId, workerName, mode) {
    var modal = new bootstrap.Modal(document.getElementById('certificateHistoryModal'));
    modal.show();
}
```
Pola yang sama digunakan untuk modal konfirmasi bulk renew.

---

## State of the Art

| Komponen Lama (Phase 208) | Yang Diubah di Phase 209 | Impact |
|---------------------------|--------------------------|--------|
| `selectedKategori` — lock per kategori | `selectedGroupKey` — lock per group | Lebih intuitif untuk user |
| `updateRenewSelectedButton()` — global button | `updateGroupRenewButton(groupKey)` — per group | Sesuai keputusan user |
| `#btn-renew-selected` — global di atas filter | Hapus; ganti tombol per group di header | Konsistensi dengan grouped view |
| `wireCheckboxes()` — tanpa select-all | `wireCheckboxes()` + `wireGroupSelectAll()` | Feature BULK-01 terpenuhi |
| Empty state: hanya "tidak ada data" | Dua empty state: "tidak ada data" vs "filter kosong" | FILT-01 proper UX |

---

## Open Questions

1. **Posisi tombol renew di dalam header yang juga toggle collapse**
   - Yang diketahui: Header memiliki `data-bs-toggle="collapse"` — semua klik di dalamnya akan toggle accordion
   - Yang perlu diputuskan: Apakah tombol di-render server-side di partial atau JS-injected setelah AJAX load
   - Rekomendasi: Render server-side di `_RenewalGroupedPartial.cshtml` (lebih clean), tambah `stopPropagation` di onclick attribute

2. **Apakah `IsFiltered` perlu ditambah ke `RenewalGroupViewModel`**
   - Yang diketahui: Empty state filter berbeda dari empty state tanpa data
   - Rekomendasi: Tambah properti `bool IsFiltered` ke `RenewalGroupViewModel`, set di controller — perubahan minimal (2 baris kode)

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada unit test framework terdeteksi untuk frontend) |
| Config file | Tidak ada |
| Quick run command | Jalankan aplikasi, buka /Admin/RenewalCertificate |
| Full suite command | Verifikasi semua success criteria satu per satu |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Cara Verifikasi |
|--------|----------|-----------|-----------------|
| BULK-01 | Select-all per group mencentang semua checkbox di group-nya | Manual | Expand group, centang select-all di header tabel, pastikan semua row tercentang |
| BULK-01 | Centang group A → checkbox group B-Z disabled | Manual | Centang satu checkbox di group A, pastikan checkbox group lain tidak bisa diklik |
| BULK-02 | Tombol "Renew N Pekerja" muncul saat ada checkbox tercentang | Manual | Centang satu checkbox, pastikan tombol muncul di header accordion group yang sama |
| BULK-02 | Tombol menghilang saat tidak ada yang tercentang | Manual | Uncheck semua, pastikan tombol hilang |
| BULK-02 | Modal konfirmasi muncul sebelum redirect | Manual | Klik tombol Renew, pastikan modal muncul dengan nama sertifikat dan jumlah pekerja yang benar |
| FILT-01 | Filter bagian → group yang semua anggotanya terfilter keluar tidak muncul | Manual | Pilih bagian yang hanya ada sebagian pekerja, pastikan group dengan nol anggota tidak muncul |
| FILT-01 | Empty state "sesuai filter" + tombol Reset Filter tampil jika semua group tersembunyi | Manual | Set filter yang tidak cocok dengan data apapun, pastikan pesan dan tombol reset muncul |
| FILT-02 | Summary card Expired count update sesuai filter | Manual | Filter status "Expired", pastikan card hanya menampilkan count sesuai filter |
| FILT-02 | Summary card Akan Expired count update sesuai filter | Manual | Filter status "AkanExpired", pastikan card update |

### Wave 0 Gaps
Tidak ada gap infrastruktur — tidak diperlukan setup test framework atau file test baru.

---

## Sources

### Primary (HIGH confidence)
- `Views/Admin/RenewalCertificate.cshtml` — JS logic lengkap: wireCheckboxes, refreshTable, updateSummaryCards, wirePagination, refreshGroupTable
- `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` — HTML structure accordion groups, hidden spans count
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — Per-group table, `cb-group-select-all` element, `data-group-key` pada `.cb-select`
- `Controllers/AdminController.cs` (line 6956-7065) — FilterRenewalCertificate, FilterRenewalCertificateGroup actions lengkap
- `.planning/phases/209-bulk-renew-filter-compatibility/209-CONTEXT.md` — Semua locked decisions

### Secondary (MEDIUM confidence)
- Bootstrap 5 docs — `bootstrap.Modal` instantiation pattern, `data-bs-toggle="collapse"` event propagation behavior

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua komponen sudah ada di proyek, tidak ada library baru
- Architecture: HIGH — semua pattern diverifikasi dari kode aktual Phase 208
- Pitfalls: HIGH — identifikasi dari analisis langsung kode dan pola interaksi DOM
- Filter compatibility: HIGH — server-side sudah berfungsi, gap hanya di empty state dan `IsFiltered` property

**Research date:** 2026-03-20
**Valid until:** Tidak ada batas waktu (kode stabil, tidak ada dependency eksternal yang berubah)
