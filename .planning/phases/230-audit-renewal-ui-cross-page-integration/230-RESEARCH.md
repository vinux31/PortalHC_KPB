# Phase 230: Audit Renewal UI & Cross-Page Integration - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — Bootstrap 5 UI, AJAX partial views, modal patterns, cross-controller integration
**Confidence:** HIGH (semua temuan dari pembacaan kode aktual)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Accordion expand/collapse per grup sertifikat — default collapsed, expand untuk lihat detail pekerja
- **D-02:** Header grup menampilkan: judul sertifikat, jumlah pekerja, breakdown expired vs akan expired
- **D-03:** Color coding merah/kuning sudah OK — audit konsistensi warna di semua tempat (tabel, badge, summary card), tidak perlu gradasi 30/60/90
- **D-04:** Auto-reload tabel via AJAX setiap kali filter diubah — tidak perlu tombol Apply
- **D-05:** Cascade Bagian→Unit (sudah ada) DAN Kategori→SubKategori (baru) — SubKategori disabled sampai Kategori dipilih
- **D-06:** Reset button: semua dropdown kembali ke default, Unit & SubKategori disabled, tabel reload tanpa filter
- **D-07:** Kedua pilihan renewal (via Assessment & via Training) selalu tampil di modal — baik untuk sertifikat tipe Assessment maupun Training
- **D-08:** Bulk renew dengan pekerja yang sudah di-renew: tampilkan warning daftar yang akan di-skip, user konfirmasi sebelum lanjut dengan sisanya
- **D-09:** Renew via Assessment pre-fill: judul (dari sertifikat), kategori (dari MapKategori), peserta (pekerja yang di-renew)
- **D-10:** Renew via Training pre-fill: mengikuti pola yang sama — judul, kategori, peserta
- **D-11:** CDP CertificationManagement: toggle switch, default sembunyikan renewed certs. Toggle untuk menampilkan semua.
- **D-12:** Admin/Index badge count harus sinkron dengan BuildRenewalRowsAsync (sudah single source of truth dari Phase 229)

### Claude's Discretion
- Loading state/skeleton saat AJAX filter reload
- Exact styling accordion headers
- Error state handling pada modal dan pre-fill
- AddTraining pre-fill field mapping detail

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — diskusi tetap dalam scope phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| UIUX-01 | Grouped view RenewalCertificate tampil benar dengan data aktual | Accordion sudah ada, tapi ada gap: rows tidak di-load saat collapse buka (lazy load needed) |
| UIUX-02 | Filter cascade Bagian/Unit/Kategori/Tipe berfungsi dan saling terhubung | Cascade Bagian→Unit ada; Kategori→SubKategori sudah ada di view tapi perlu verifikasi endpoint GetSubCategories |
| UIUX-03 | Renewal method modal (single + bulk) menampilkan pilihan yang benar berdasarkan tipe | Modal sudah ada, kedua pilihan selalu tampil — sesuai D-07, tidak ada masalah |
| UIUX-04 | Certificate history modal menampilkan chain grouping yang akurat | CertificateHistory action sudah menghasilkan grouped view, perlu audit tampilan partial |
| XPAG-01 | CreateAssessment renewal pre-fill (judul, kategori, peserta) dari RenewalCertificate berfungsi | Pre-fill sudah diimplementasi sejak Phase 201/210 — perlu audit akurasi kategori dan peserta |
| XPAG-02 | AddTraining renewal mode (pre-fill + FK) dari RenewalCertificate berfungsi | Sudah ada sejak Phase 210, perlu audit field mapping dan apakah peserta di-pre-fill benar |
| XPAG-03 | CDP Certification Management menyembunyikan renewed certs dengan toggle | Toggle sudah ada dan berfungsi via CSS class, tapi state toggle reset saat AJAX reload |
| XPAG-04 | Admin/Index badge count mencerminkan jumlah renewal yang pending | Sudah menggunakan BuildRenewalRowsAsync — Phase 229 memastikan ini adalah single source of truth |
</phase_requirements>

---

## Summary

Phase 230 adalah audit + perbaikan UI pada sistem renewal yang sudah dibangun. Mayoritas fitur sudah ada di kodebase — pekerjaan utamanya adalah menemukan dan memperbaiki gap, inkonsistensi, dan edge case yang belum ditangani.

Berdasarkan pembacaan kode aktual, ditemukan beberapa gap konkret: (1) Toggle "Tampilkan Riwayat Renewal" di CDP CertificationManagement kehilangan state-nya setiap kali tabel di-reload via AJAX (applyRenewedToggle() dipanggil setelah reload, yang seharusnya benar — perlu verifikasi); (2) D-08 belum diimplementasi — bulk renew tidak menampilkan daftar skip warning, langsung error Mixed; (3) AddTraining pre-fill sudah ada tapi perlu verifikasi apakah peserta (SelectedUserIds) benar ter-pre-fill; (4) Certificate history modal perlu audit chain grouping accuracy.

**Primary recommendation:** Lakukan audit per requirement dengan membandingkan kode aktual terhadap spesifikasi CONTEXT.md, kemudian fix gap secara targeted. Jangan refactor yang sudah berfungsi.

---

## Current State Assessment (Temuan Kode Aktual)

### UIUX-01: Grouped View

**Status saat ini:** Accordion per grup sudah berfungsi (`_RenewalGroupedPartial.cshtml`). Default collapsed. Header menampilkan: Judul, badge TotalCount, badge ExpiredCount (merah), badge AkanExpiredCount (kuning), Kategori/SubKategori kecil di kanan, tombol Renew (hidden, muncul saat ada checkbox dipilih).

**Gap yang ditemukan:**
- Rows di-load via partial langsung saat FilterRenewalCertificate — bukan lazy load. Jadi saat accordion dibuka, data sudah ada. Ini OK secara fungsionalitas.
- Warna: merah (`bg-danger`) dan kuning (`bg-warning text-dark`) digunakan — konsisten dengan summary cards.
- Perlu audit: apakah `_RenewalGroupTablePartial` menampilkan data aktual dari DB, atau ada kasus di mana data stale/wrong.

### UIUX-02: Filter Cascade

**Status saat ini:**
- Cascade Bagian→Unit: menggunakan `/CDP/GetCascadeOptions?section=` — sudah ada dan berfungsi.
- Cascade Kategori→SubKategori: kode JS sudah ada di `RenewalCertificate.cshtml` (line 249-268), memanggil `/CDP/GetSubCategories?category=`. SubKategori disabled sampai Kategori dipilih.
- Reset button: sudah ada dan mereset semua filter (line 273-283).
- Auto-reload: setiap perubahan filter langsung memanggil `refreshTable(1)`.

**Gap yang perlu diverifikasi:**
- Apakah endpoint `/CDP/GetSubCategories` sudah ada dan mengembalikan data yang benar untuk kategori AssessmentCategories.
- Apakah filter `subCategory` benar diteruskan ke `FilterRenewalCertificate` dan diaplikasikan ke `SertifikatRow.SubKategori`.
- TrainingRecords: `SubKategori = null` (hardcoded di BuildRenewalRowsAsync line 6854). Filter subCategory tidak akan pernah match untuk Training records — ini mungkin OK (training tidak punya subkategori) tapi perlu dipastikan.

### UIUX-03: Renewal Method Modal

**Status saat ini:**
- Modal single renew (`renewMethodModal`): Menampilkan 2 tombol — "Renew via Assessment" dan "Renew via Training". Keduanya selalu tampil tanpa kondisi tipe.
- Modal bulk renew (`bulkRenewConfirmModal`): Menampilkan error jika Mixed, atau pilihan metode jika tidak Mixed. Kedua pilihan selalu tampil.
- Sesuai D-07: tidak perlu perubahan logika tampil/sembunyikan tombol.

**Gap (D-08):** Bulk renew "already renewed" skip warning belum ada. Saat ini jika ada pekerja yang sudah di-renew di dalam bulk selection, double renewal guard di Phase 229 akan menolak di sisi server. Tapi D-08 minta warning di sisi client sebelum navigasi. Perlu tambahan: saat membangun `pendingRenewParams`, cek apakah ada `data-is-renewed="true"` di checkbox, tampilkan list skip, minta konfirmasi.

**Pertanyaan:** Apakah checkbox `.cb-select` sudah punya attribute `data-is-renewed`? Perlu dicek di `_RenewalGroupTablePartial.cshtml` — dari kode yang dibaca, atribut `data-sourceid`, `data-recordtype`, `data-group-key` ada, tapi `data-is-renewed` **tidak ada**. Ini karena BuildRenewalRowsAsync sudah memfilter `IsRenewed = false` di baris 6921-6926, jadi renewed certs tidak akan muncul di tabel renewal. D-08 mungkin tidak relevan dalam konteks ini — perlu re-evaluasi.

### UIUX-04: Certificate History Modal

**Status saat ini:**
- Modal memanggil `/Admin/CertificateHistory?workerId=&mode=renewal`
- `CertificateHistory` action (line 6933) mengambil semua sertifikat pekerja, mengelompokkan menjadi "chains" by judul
- Dikembalikan sebagai partial view `_CertificateHistoryModalContent.cshtml`

**Gap:** Perlu audit accuracy chain grouping — apakah 4 FK combinations (AS→AS, AS→TR, TR→TR, TR→AS) terbentuk dengan benar dalam chain visual.

### XPAG-01: CreateAssessment Pre-fill

**Status saat ini (kode aktual line 998-1063):**
- Single renew via Assessment session: pre-fill Title, Category, ValidUntil+1yr, SelectedUserIds=[userId], RenewsSessionId
- Bulk renew via Assessment sessions: pre-fill Title dari first session, Category, SelectedUserIds=[semua userId], RenewalFkMap (JSON), RenewalFkMapType="session"
- Single renew via Training record: pre-fill Title, Category (via MapKategori), ValidUntil+1yr, RenewsTrainingId
- Bulk renew via Training records: pre-fill Title, SelectedUserIds, RenewalFkMap, RenewalFkMapType="training"

**Gap yang mungkin:**
- Apakah View CreateAssessment.cshtml menampilkan pre-filled category di dropdown kategori dengan benar? Category dari Training records adalah raw code, setelah MapKategori menjadi display name — apakah ini match dengan `<option value="">` di dropdown?
- SelectedUserIds: apakah peserta benar ter-select di multi-select user picker?
- ViewBag.RenewalSourceUserName untuk bulk: di-join dengan koma, tapi alert hanya menampilkan nama — bisa panjang jika banyak peserta.

### XPAG-02: AddTraining Pre-fill

**Status saat ini (line 5444-5550):**
- AddTraining GET sudah menerima `renewSessionId` dan `renewTrainingId`
- Pre-fill: Judul (dari Judul/Title sumber), IsRenewalMode, RenewalFkMap (bulk), SelectedUserIds
- View AddTraining.cshtml: alert "Mode Renewal" ditampilkan jika `ViewBag.IsRenewalMode == true`

**Gap:** Perlu verifikasi apakah field Kategori juga di-pre-fill di AddTraining — kode terlihat menyimpan `IsRenewalMode`, `RenewalSourceTitle`, `RenewalSourceUserName`, `RenewalFkMap`, `RenewalFkMapType` tapi Kategori/Judul perlu diperiksa lebih lanjut di kode line 5461-5550.

### XPAG-03: CDP CertificationManagement Toggle

**Status saat ini:**
- Toggle switch `toggle-renewed` sudah ada (line 138)
- `applyRenewedToggle()` menyembunyikan/menampilkan rows dengan class `renewed-row` via inline style `display:none`
- Saat AJAX reload, `applyRenewedToggle()` dipanggil setelah `container.innerHTML = html` — ini **benar**, toggle state dipertahankan
- Initial load: `_CertificationManagementTablePartial.cshtml` sudah set `style="display:none;"` untuk renewed rows secara default

**Status XPAG-03:** Secara fungsional sudah berfungsi. Yang perlu diaudit: apakah `IsRenewed` benar dikalkulasi di `BuildSertifikatRowsAsync` (CDPController) menggunakan 4 FK sets yang sama dengan `BuildRenewalRowsAsync` (AdminController)?

### XPAG-04: Admin/Index Badge Count

**Status saat ini (line 59-61 AdminController):**
- `Index()` action memanggil `BuildRenewalRowsAsync()` dan set `ViewBag.RenewalCount = renewalRows.Count`
- View `Index.cshtml` menampilkan badge jika `ViewBag.RenewalCount > 0`
- Phase 229 memastikan BuildRenewalRowsAsync adalah single source of truth

**Status XPAG-04:** Sudah diimplementasi dan sinkron. Perlu verifikasi di Admin/Index bahwa badge muncul di card yang benar (Renewal Sertifikat).

---

## Architecture Patterns

### Pola AJAX Partial View Reload (digunakan di seluruh codebase)

```javascript
// Pattern: fetch partial → replace container.innerHTML
fetch('/Admin/FilterRenewalCertificate?' + params, { signal: certAbort.signal })
    .then(function (resp) { return resp.text(); })
    .then(function (html) {
        container.innerHTML = html;
        wireCheckboxes();      // re-wire setelah DOM diganti
        wirePagination();
        wireGroupChevrons();
        container.classList.remove('dashboard-loading');
    });
```

**Key pitfall:** Setelah `container.innerHTML = html`, semua event listener hilang. Harus re-wire.

### Loading State Pattern

```css
.dashboard-loading { opacity: 0.5; pointer-events: none; position: relative; }
```

Sudah diimplementasi di RenewalCertificate.cshtml dan CertificationManagement.cshtml.

### Cascade Dropdown Pattern

```javascript
// Trigger: parent change → fetch options → populate child → enable child → refreshTable
fetch('/CDP/GetCascadeOptions?section=' + encodeURIComponent(section))
    .then(r => r.json())
    .then(data => {
        data.units.forEach(u => { /* populate */ });
        unitEl.disabled = false;
        refreshTable(1);
    });
```

Endpoint `/CDP/GetSubCategories?category=` sudah di-consume di filter JS — endpoint ini harus ada di CDPController.

### Bootstrap 5 Modal Pattern

```javascript
// Modal sederhana
new bootstrap.Modal(document.getElementById('modalId')).show();

// Modal dengan AJAX content
modal.show();
fetch('/Admin/CertificateHistory?workerId=...')
    .then(r => r.text())
    .then(html => { modalBody.innerHTML = html; });
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Filter cascade | Custom filter logic | Pola GetCascadeOptions yang sudah ada | Konsisten dengan CDPController pattern di semua halaman |
| Loading state | Custom spinner overlay | CSS class `.dashboard-loading` yang sudah ada | Sudah digunakan di seluruh codebase |
| Pagination per-group | Custom pagination | PaginationHelper.Calculate() yang sudah ada | Sudah digunakan di FilterRenewalCertificateGroup |
| AJAX partial | Custom fetch utility | Pola fetch → innerHTML → re-wire yang sudah ada | Konsisten di semua halaman |

---

## Common Pitfalls

### Pitfall 1: Event Listener Hilang Setelah innerHTML Replace
**What goes wrong:** Setelah `container.innerHTML = html`, checkbox, pagination link, chevron accordion — semua event listener hilang.
**How to avoid:** Selalu panggil `wireCheckboxes()`, `wirePagination()`, `wireGroupChevrons()` setelah setiap AJAX reload. Sudah diimplementasi di kode existing — jangan lupa tambahkan jika ada wire function baru.

### Pitfall 2: Toggle State Hilang Setelah AJAX Reload
**What goes wrong:** Jika toggle state tidak diaplikasikan ulang setelah `innerHTML = html`, renewed rows akan kembali ke state default (hidden).
**How to avoid:** Panggil `applyRenewedToggle()` setelah setiap AJAX reload — sudah ada di CDP CertificationManagement (line 293).

### Pitfall 3: SubKategori Filter Tidak Match Training Records
**What goes wrong:** `SertifikatRow.SubKategori` untuk Training records adalah `null` (hardcoded di BuildRenewalRowsAsync). Filter `?subCategory=xxx` tidak akan mengembalikan Training records.
**How to avoid:** Ini adalah behavior yang benar (Training tidak punya SubKategori). Tidak perlu fix. Pastikan dropdown SubKategori hanya muncul untuk kategori yang punya child (Assessment categories dengan ParentId null yang punya children).

### Pitfall 4: Pre-fill Category Mismatch
**What goes wrong:** Category dari TrainingRecord adalah raw code (mis. "MANDATORY"), setelah MapKategori menjadi "Mandatory HSSE Training". Jika dropdown kategori di CreateAssessment menggunakan value yang berbeda, pre-fill tidak akan ter-select.
**How to avoid:** Pastikan `model.Category` yang di-set di controller (setelah MapKategori) persis sama dengan value yang digunakan di `<option value="...">` di view. AssessmentCategories.Name digunakan sebagai value — harus match.

### Pitfall 5: GroupKey Collision
**What goes wrong:** GroupKey di-generate dari Base64 judul sertifikat. Jika ada karakter edge case, Base64 encoding bisa menghasilkan key yang conflict di DOM ID.
**How to avoid:** Kode sudah mengganti `+` → `_`, `/` → `-`, `=` → `""`. Sudah aman untuk URL dan DOM ID.

---

## Audit Checklist Per Requirement

### UIUX-01 Audit
- [ ] Buka RenewalCertificate dengan data nyata, verifikasi accordion collapse/expand benar
- [ ] Verifikasi badge count di header grup sesuai data aktual
- [ ] Verifikasi warna badge konsisten: merah=Expired, kuning=Akan Expired di header grup DAN di row tabel DAN di summary cards

### UIUX-02 Audit
- [ ] Verifikasi endpoint `/CDP/GetSubCategories` ada dan mengembalikan data yang benar
- [ ] Test: pilih Kategori → SubKategori enabled → pilih SubKategori → tabel reload dengan filter tersebut
- [ ] Test: klik Reset → semua filter default, SubKategori disabled
- [ ] Verifikasi filter `subCategory` diterapkan di FilterRenewalCertificate (kode sudah ada di line 7156)

### UIUX-03 Audit
- [ ] Test single renew — modal menampilkan 2 tombol (Assessment + Training)
- [ ] Test bulk renew homogen — modal menampilkan 2 tombol
- [ ] Test bulk renew mixed — modal menampilkan error (bukan pilihan metode)
- [ ] D-08: Evaluasi apakah skip warning perlu diimplementasi (kemungkinan tidak relevan karena BuildRenewalRowsAsync sudah filter IsRenewed=false)

### UIUX-04 Audit
- [ ] Buka history modal untuk pekerja dengan chain renewal — verifikasi chain tampil dengan benar
- [ ] Verifikasi partial `_CertificateHistoryModalContent.cshtml` menampilkan relasi renewal (mis. "merupakan renewal dari...")

### XPAG-01 Audit
- [ ] Test single renew via Assessment: cek Judul, Kategori, dan 1 peserta ter-pre-fill di CreateAssessment
- [ ] Test bulk renew via Assessment (3+ pekerja): cek Judul, Kategori, semua peserta ter-pre-fill
- [ ] Test single renew via Training: cek Judul, Kategori (display name), 1 peserta
- [ ] Test bulk renew via Training

### XPAG-02 Audit
- [ ] Test single renew via Training ke AddTraining: cek Judul, alert "Mode Renewal" tampil, peserta ter-select
- [ ] Test bulk renew via Training ke AddTraining
- [ ] Verifikasi field Kategori di AddTraining ter-pre-fill (atau apakah tidak diperlukan)
- [ ] Verifikasi RenewalFkMap diteruskan ke POST dan FK disimpan dengan benar

### XPAG-03 Audit
- [ ] Buka CDP CertificationManagement — renewed rows tersembunyi by default
- [ ] Toggle ON — renewed rows tampil dengan opacity 0.5
- [ ] Filter tabel (AJAX reload) — toggle state dipertahankan
- [ ] Verifikasi IsRenewed dikalkulasi dengan 4 FK sets di BuildSertifikatRowsAsync

### XPAG-04 Audit
- [ ] Buka Admin/Index — badge count tampil di card Renewal Sertifikat
- [ ] Verifikasi angka badge sama dengan count di RenewalCertificate page

---

## File Map (Canonical References)

| File | Baris Kunci | Fungsi |
|------|-------------|--------|
| `Views/Admin/RenewalCertificate.cshtml` | 1-577 | Halaman utama: filter bar, modals, JS cascade, AJAX reload |
| `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` | 1-76 | Accordion grup, header, empty state |
| `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` | 1-99 | Tabel pekerja per grup, checkbox, pagination |
| `Controllers/AdminController.cs` | 6771-6928 | `BuildRenewalRowsAsync()` — single source of truth |
| `Controllers/AdminController.cs` | 7109-7260 | `RenewalCertificate`, `FilterRenewalCertificate`, `FilterRenewalCertificateGroup` |
| `Controllers/AdminController.cs` | 6933-7105 | `CertificateHistory` action |
| `Controllers/AdminController.cs` | 957-1063 | `CreateAssessment` GET — renewal pre-fill logic |
| `Controllers/AdminController.cs` | 5444-5550 | `AddTraining` GET — renewal pre-fill logic |
| `Controllers/AdminController.cs` | 59-61 | `Index()` — badge count via BuildRenewalRowsAsync |
| `Controllers/CDPController.cs` | 3047-3132 | `CertificationManagement`, `FilterCertificationManagement` |
| `Views/CDP/CertificationManagement.cshtml` | 138-302 | Toggle renewed, AJAX filter, applyRenewedToggle |
| `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` | 49 | `renewed-row` class dan `display:none` default |
| `Views/Admin/AddTraining.cshtml` | 19-28, 189-202 | IsRenewalMode alert, hidden FK fields |
| `Views/Admin/Index.cshtml` | 221-223 | Badge count display |

---

## Open Questions

1. **Apakah `/CDP/GetSubCategories` endpoint sudah ada?**
   - Dari kode JS di RenewalCertificate.cshtml terlihat sudah dikonsumsi, tapi perlu verifikasi di CDPController
   - Rekomendasi: grep CDPController untuk `GetSubCategories`

2. **D-08 Relevansi — skip warning untuk already-renewed**
   - `BuildRenewalRowsAsync` sudah memfilter `IsRenewed = false`, sehingga sertifikat yang sudah di-renew tidak akan muncul di tabel
   - Kemungkinan D-08 tidak perlu diimplementasi (edge case tidak mungkin terjadi via UI normal)
   - Rekomendasi: konfirmasi dengan user, kemudian mark sebagai "tidak berlaku" atau implementasi guard sederhana

3. **Kategori di AddTraining — apakah perlu di-pre-fill?**
   - AddTraining form mungkin tidak punya dropdown Kategori yang sama dengan AssessmentCategories
   - TrainingRecord.Kategori adalah free text, berbeda dengan AssessmentSession.Category
   - Rekomendasi: cek field Kategori di AddTraining form, tentukan apakah mapping diperlukan

---

## Sources

### Primary (HIGH confidence)
- Kode aktual `Controllers/AdminController.cs` — dibaca langsung dari file
- Kode aktual `Views/Admin/RenewalCertificate.cshtml` — dibaca langsung dari file
- Kode aktual `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` — dibaca langsung
- Kode aktual `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — dibaca langsung
- Kode aktual `Controllers/CDPController.cs` — dibaca langsung dari file
- Kode aktual `Views/CDP/CertificationManagement.cshtml` — dibaca langsung dari file

### Secondary (MEDIUM confidence)
- CONTEXT.md Phase 230 — keputusan user yang sudah dikonfirmasi
- REQUIREMENTS.md — requirement IDs dan deskripsi

---

## Metadata

**Confidence breakdown:**
- Current state assessment: HIGH — dari kode aktual
- Gap identification: HIGH — perbandingan kode vs CONTEXT.md decisions
- Architecture patterns: HIGH — dari kode existing yang sudah berjalan

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (stable codebase, tidak ada perubahan eksternal)
