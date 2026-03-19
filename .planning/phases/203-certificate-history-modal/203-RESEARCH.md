# Phase 203: Certificate History Modal - Research

**Researched:** 2026-03-19
**Domain:** ASP.NET Core MVC — Bootstrap Modal + AJAX Partial View + Renewal Chain Query
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Tampilan Tabel**
- Layout tabel sederhana di dalam modal (bukan timeline atau card)
- Kolom: Judul, Kategori, Sub Kategori, Valid, Status, Sumber (Assessment/Training)
- Modal size: modal-lg dengan max-height dan scroll internal
- Sertifikat di-group by renewal chain dengan grouping header row per chain

**Grouping & Sorting**
- Setiap chain ditampilkan dengan header row (misal "K3 Migas")
- Sertifikat standalone (tanpa chain) tetap tampil sebagai group sendiri dengan 1 entry — format konsisten
- Urutan group: terbaru di atas (berdasarkan sertifikat terbaru dalam chain)
- Dalam satu group/chain: terbaru di atas, original di bawah (newest to oldest)

**Trigger & Entry Point**
- Renewal Certificate page: icon clock/history di kolom Aksi per baris → buka modal history pekerja
- CDP CertificationManagement: nama pekerja jadi clickable link → buka modal history read-only
- Data di-load via AJAX on-demand (klik trigger → fetch partial view → inject ke modal)

**Mode & Aksi**
- Satu shared partial view, mode dibedakan via query param (?mode=renewal atau ?mode=readonly)
- Mode renewal: tombol Renew tampil pada sertifikat expired/akan expired yang belum di-renew
- Mode readonly: tanpa tombol aksi apapun
- Klik Renew di modal → langsung redirect ke CreateAssessment dengan renewSessionId/renewTrainingId (tanpa tutup modal dulu)

### Claude's Discretion
- Empty state jika pekerja tidak punya sertifikat
- Exact styling grouping header
- Loading spinner saat AJAX fetch
- Error handling jika fetch gagal

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope fase.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HIST-01 | Modal timeline riwayat sertifikat per pekerja, grouped by renewal chain (terbaru di atas) | BuildSertifikatRowsAsync + renewal chain FKs tersedia; query baru perlu filter by workerId dan resolve chain grouping |
| HIST-02 | Di Renewal page, modal history menampilkan tombol Renew pada sertifikat expired/akan expired yang belum di-renew | IsRenewed + Status fields sudah ada di SertifikatRow; mode=renewal param mengaktifkan tombol |
| HIST-03 | Di CDP CertificationManagement, klik nama pekerja membuka modal history read-only | _CertificationManagementTablePartial kolom Nama saat ini plain text — perlu diubah jadi link; mode=readonly menyembunyikan tombol |

</phase_requirements>

---

## Summary

Phase 203 membangun modal riwayat sertifikat yang di-load via AJAX on-demand. Modal ini shared (digunakan dari dua halaman berbeda) dengan mode berbeda: renewal (dengan tombol Renew) dan readonly. Data diambil dari controller action baru yang menghasilkan partial view HTML langsung di-inject ke modal container.

Seluruh data model yang dibutuhkan sudah ada: `SertifikatRow` memiliki `SourceId`, `RecordType`, `IsRenewed`, `Status`, `ValidUntil`, `Judul`, `Kategori`, `SubKategori`. Renewal chain tracking tersedia via `RenewsSessionId`/`RenewsTrainingId` di `AssessmentSession` dan `TrainingRecord`. Tantangan utama adalah **resolving renewal chain grouping**: query harus menelusuri chain dari setiap sertifikat untuk mengelompokkan sertifikat yang saling terhubang ke satu group, dengan urutan newest-to-oldest per group dan group terbaru di atas.

**Primary recommendation:** Tambah action `CertificateHistoryAsync(string workerId, string mode)` di `AdminController` — letakkan di AdminController bukan CDPController karena halaman Renewal Certificate ada di Admin scope. Shared partial di `Views/Shared/_CertificateHistoryModal.cshtml`.

---

## Standard Stack

### Core (sudah ada di project)
| Komponen | Versi/Lokasi | Purpose |
|----------|-------------|---------|
| Bootstrap Modal | 5.x (sudah ada) | Modal container, backdrop, show/hide API |
| Bootstrap Icons | bi bi-clock-history | Trigger icon di kolom Aksi |
| jQuery / Vanilla JS fetch | Sudah ada | AJAX fetch partial view |
| ASP.NET Core PartialView | CDPController pattern | Return HTML partial dari controller action |
| SertifikatRow model | Models/CertificationManagementViewModel.cs | Data model sudah sesuai kebutuhan |

### Pattern Yang Sudah Terbukti di Codebase
| Pattern | Lokasi Referensi | Applicable |
|---------|-----------------|------------|
| AJAX partial view + inject innerHTML | CDPController FilterCertificationManagement → _CertificationManagementTablePartial | Sama persis untuk load modal content |
| Bootstrap modal HTML + JS show | ManageCategories.cshtml deleteModal | Template modal structure |
| AbortController untuk cancel inflight | RenewalCertificate.cshtml certAbort | Gunakan jika modal dibuka cepat-cepat |
| PartialView() return | CDPController line 3126 | Return partial HTML dari action |

---

## Architecture Patterns

### Recommended Project Structure (new files)

```
Views/
└── Shared/
    └── _CertificateHistoryModal.cshtml    # Shell modal (container)

Controllers/
└── AdminController.cs                     # Tambah CertificateHistoryAsync action
```

### Modifikasi File Yang Ada

```
Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml
    → Tambah icon trigger (bi-clock-history) di kolom Aksi per baris
    → data-worker-id="@row.WorkerId" untuk trigger AJAX

Views/Admin/RenewalCertificate.cshtml
    → Tambah modal container shell + modal JS
    → Include _CertificateHistoryModal.cshtml atau inline modal HTML

Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
    → Ubah kolom Nama dari plain text ke <a href="#" data-worker-id="..."> (hanya jika RoleLevel <= 4)
    → Tambah data-mode="readonly"

Views/CDP/CertificationManagement.cshtml
    → Tambah modal container shell + modal JS
```

### Pattern 1: AJAX On-Demand Modal Load (diterapkan di project)

**Apa:** Klik trigger → fetch URL → inject HTML ke modal body → tampilkan modal.

**Contoh (berdasarkan pattern CDPController):**
```javascript
// Source: RenewalCertificate.cshtml pattern (certAbort, fetch, innerHTML)
function openHistoryModal(workerId, mode) {
    var modalBody = document.getElementById('history-modal-body');
    modalBody.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-primary"></div></div>';
    var modal = new bootstrap.Modal(document.getElementById('certificateHistoryModal'));
    modal.show();

    fetch('/Admin/CertificateHistory?workerId=' + encodeURIComponent(workerId) + '&mode=' + mode)
        .then(function(r) { return r.text(); })
        .then(function(html) { modalBody.innerHTML = html; })
        .catch(function(e) {
            modalBody.innerHTML = '<div class="alert alert-danger">Gagal memuat data. Silakan coba lagi.</div>';
        });
}
```

### Pattern 2: Renewal Chain Grouping Query

**Apa:** Untuk history modal, sertifikat milik satu pekerja harus di-group berdasarkan renewal chain. Chain adalah pohon sertifikat yang saling terhubung via RenewsSessionId/RenewsTrainingId.

**Algoritma (in-memory, bukan SQL):**
1. Query semua sertifikat pekerja (AS dengan GenerateCertificate+IsPassed + TR dengan SertifikatUrl) — mirip BuildSertifikatRowsAsync tapi filter `workerId`
2. Load semua renewal FK pairs untuk sertifikat pekerja tersebut (AS.RenewsSessionId, AS.RenewsTrainingId, TR.RenewsSessionId, TR.RenewsTrainingId)
3. Build graph: setiap node adalah (RecordType, SourceId); edge = "A renews B"
4. Union-Find atau DFS untuk menemukan connected components — setiap component = satu chain group
5. Sort dalam group: newest ValidUntil di atas
6. Sort groups: group dengan ValidUntil terbaru dari member terbaru di atas

**Catatan penting:** Chain bisa campuran AS dan TR (misal: TR original → AS renewal → AS renewal lagi). Graph traversal harus handle cross-type links.

### Pattern 3: Shared Partial View dengan Mode Parameter

**Apa:** Action controller menerima `mode` string, pass ke partial via ViewBag/ViewData.

```csharp
// AdminController — action baru
[HttpGet]
public async Task<IActionResult> CertificateHistory(string workerId, string mode = "readonly")
{
    // query sertifikat + build chain groups
    ViewBag.Mode = mode;
    ViewBag.WorkerName = workerName;
    return PartialView("Shared/_CertificateHistoryModalContent", chainGroups);
}
```

```razor
@* Views/Shared/_CertificateHistoryModalContent.cshtml *@
@model List<CertificateChainGroup>
@{
    var mode = ViewBag.Mode as string ?? "readonly";
    var isRenewalMode = mode == "renewal";
}
```

### Anti-Patterns to Avoid

- **Jangan load semua worker sekaligus:** Modal harus on-demand per pekerja — jangan preload semua history ke DOM
- **Jangan pakai ViewComponent untuk ini:** AJAX partial view sudah cukup dan konsisten dengan pola project yang ada
- **Jangan tambah controller baru:** Gunakan AdminController (karena RenewalCertificate sudah di Admin) untuk endpoint shared

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Modal container + backdrop | Custom overlay | Bootstrap Modal API (`new bootstrap.Modal(el).show()`) | Sudah ada di project, konsisten |
| Spinner loading state | Custom CSS animation | Bootstrap `spinner-border` class | Sudah ada, zero effort |
| Status badge | Custom styling | Pattern dari _RenewalCertificateTablePartial (badge bg-danger/warning/success) | Konsisten dengan halaman lain |

---

## Common Pitfalls

### Pitfall 1: WorkerId Tidak Tersedia di SertifikatRow
**What goes wrong:** `SertifikatRow` menyimpan `NamaWorker` (string) tapi tidak `WorkerId` (string). Trigger modal membutuhkan `workerId` untuk AJAX call.
**Why it happens:** BuildSertifikatRowsAsync tidak menyertakan UserId di SertifikatRow karena tidak dibutuhkan sebelumnya.
**How to avoid:** Tambah field `WorkerId` ke `SertifikatRow` model, isi saat mapping di BuildSertifikatRowsAsync dan di BuildRenewalRowsAsync (AdminController). Atau, alternatif: tambah data attribute saat render tanpa mengubah model (tapi ini rapuh jika baris bisa diklik dari berbagai konteks).
**Warning signs:** Klik trigger tidak mengirim workerId yang benar ke endpoint.

### Pitfall 2: Chain Grouping Dengan Mixed RecordType
**What goes wrong:** Chain bisa: TR original → AS renewal. Graph traversal yang hanya melihat AS.RenewsSessionId akan melewatkan link TR→AS.
**Why it happens:** Ada 4 FK kombinasi renewal: AS→AS, AS→TR, TR→AS, TR→TR. Mudah lupa salah satu.
**How to avoid:** Load semua 4 set renewal FK saat membangun graph. Identifikasi node sebagai tuple `(RecordType, SourceId)`.

### Pitfall 3: Modal HTML Tidak Ada Saat JS Dijalankan
**What goes wrong:** JS mencoba `document.getElementById('certificateHistoryModal')` tapi modal belum ada karena include partial belum dirender.
**Why it happens:** Modal shell perlu ada di halaman host (RenewalCertificate.cshtml, CertificationManagement.cshtml) sebelum JS berjalan.
**How to avoid:** Tambah modal shell HTML langsung ke view host (bukan di partial content yang di-load via AJAX).

### Pitfall 4: Dua Modal Container di Halaman Yang Sama
**What goes wrong:** Jika `_CertificateHistoryModal.cshtml` di-include di dua tempat, ada konflik ID.
**How to avoid:** Modal shell (container dengan `id="certificateHistoryModal"`) harus ada satu per halaman. Gunakan `@await Html.PartialAsync` sekali di setiap view host, atau inline langsung.

### Pitfall 5: Tombol Renew di Modal Tidak Menutup Modal Sebelum Redirect
**What goes wrong:** User klik Renew → redirect ke CreateAssessment — ini sudah benar per keputusan (langsung redirect tanpa tutup modal). Bootstrap modal akan otomatis hilang saat navigasi. Tidak ada issue di sini, tapi jangan tambahkan `modal.hide()` sebelum redirect karena menambah delay tidak perlu.
**How to avoid:** Pastikan href/window.location.href langsung, tidak dibungkus callback tambahan.

---

## Code Examples

### Modal Shell di View Host
```html
<!-- Source: ManageCategories.cshtml deleteModal pattern, adapted -->
<div class="modal fade" id="certificateHistoryModal" tabindex="-1" aria-labelledby="historyModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="historyModalLabel">
                    <i class="bi bi-clock-history me-2"></i>Riwayat Sertifikat — <span id="history-modal-worker-name"></span>
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
            </div>
            <div class="modal-body" id="history-modal-body" style="max-height: 70vh; overflow-y: auto;">
                <!-- content injected via AJAX -->
            </div>
        </div>
    </div>
</div>
```

### Trigger di _RenewalCertificateTablePartial — Kolom Aksi
```html
<!-- Tambahkan sebelum/sesudah tombol Renew yang sudah ada -->
<button type="button"
        class="btn btn-sm btn-outline-secondary btn-history"
        data-worker-id="@row.WorkerId"
        data-worker-name="@row.NamaWorker"
        data-mode="renewal"
        title="Lihat riwayat sertifikat @row.NamaWorker">
    <i class="bi bi-clock-history"></i>
</button>
```

### Trigger di _CertificationManagementTablePartial — Kolom Nama
```html
@if (Model.RoleLevel <= 4)
{
    <td>
        <a href="#"
           class="btn-history text-decoration-none"
           data-worker-id="@row.WorkerId"
           data-worker-name="@row.NamaWorker"
           data-mode="readonly">
            @row.NamaWorker
        </a>
    </td>
}
```

### JS Event Delegation (cocok karena partial di-refresh via AJAX)
```javascript
// Source: wireCheckboxes() pattern dari RenewalCertificate.cshtml
// Pakai event delegation pada container karena partial bisa di-reload
document.addEventListener('click', function(e) {
    var btn = e.target.closest('.btn-history');
    if (!btn) return;
    e.preventDefault();
    var workerId = btn.dataset.workerId;
    var workerName = btn.dataset.workerName;
    var mode = btn.dataset.mode || 'readonly';
    openHistoryModal(workerId, workerName, mode);
});
```

### Grouping Data Structure (C# ViewModel)
```csharp
// ViewModel baru — tidak perlu di Models folder, bisa anonymous di controller
public class CertificateChainGroup
{
    public string ChainTitle { get; set; } = "";   // misal: "K3 Migas" atau judul sertifikat jika standalone
    public List<SertifikatRow> Certificates { get; set; } = new(); // newest first
    public DateTime? LatestValidUntil { get; set; } // untuk sorting groups
}
```

### Grouping Header Row di Partial (pattern tabel dengan grup)
```html
<!-- Di _CertificateHistoryModalContent.cshtml -->
@foreach (var group in Model)
{
    <tr class="table-secondary">
        <td colspan="6" class="fw-semibold ps-2">
            <i class="bi bi-link-45deg me-1 text-muted"></i>@group.ChainTitle
            <span class="badge bg-secondary ms-2">@group.Certificates.Count sertifikat</span>
        </td>
    </tr>
    @foreach (var cert in group.Certificates)
    {
        <tr>
            <td>@cert.Judul</td>
            <td>@(cert.Kategori ?? "-")</td>
            <td>@(cert.SubKategori ?? "-")</td>
            <td>@(cert.ValidUntil?.ToString("dd MMM yyyy") ?? "Permanent")</td>
            <td><!-- badge status --></td>
            <td><!-- badge sumber --></td>
            @if (isRenewalMode && (cert.Status == CertificateStatus.Expired || cert.Status == CertificateStatus.AkanExpired) && !cert.IsRenewed)
            {
                <td>
                    <a href="/Admin/CreateAssessment?renew@(cert.RecordType == RecordType.Assessment ? "SessionId" : "TrainingId")=@cert.SourceId"
                       class="btn btn-sm btn-warning">
                        <i class="bi bi-arrow-repeat me-1"></i>Renew
                    </a>
                </td>
            }
        </tr>
    }
}
```

---

## Data Model Analysis

### SertifikatRow — Field Yang Dibutuhkan Tapi Belum Ada

| Field | Status | Action |
|-------|--------|--------|
| `WorkerId` (string UserId) | TIDAK ADA di SertifikatRow | Tambah ke model + isi di BuildSertifikatRowsAsync dan BuildRenewalRowsAsync |
| `Judul` | Ada | OK |
| `Kategori` | Ada | OK |
| `SubKategori` | Ada | OK |
| `ValidUntil` | Ada | OK |
| `Status` | Ada | OK |
| `RecordType` | Ada | OK (untuk Sumber badge) |
| `IsRenewed` | Ada | OK |
| `SourceId` | Ada | OK (untuk Renew URL) |

### Renewal Chain FK Summary

```
AssessmentSession.RenewsSessionId → AssessmentSession.Id   (AS renews AS)
AssessmentSession.RenewsTrainingId → TrainingRecord.Id     (AS renews TR)
TrainingRecord.RenewsSessionId → AssessmentSession.Id      (TR renews AS — jarang)
TrainingRecord.RenewsTrainingId → TrainingRecord.Id        (TR renews TR)
```

Semua 4 FK harus diquery untuk membangun graph yang benar.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project pattern) |
| Quick run command | Jalankan aplikasi + buka browser |
| Full suite command | Tidak ada automated test suite |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Verified By |
|--------|----------|-----------|------------|
| HIST-01 | Modal muncul dengan sertifikat grouped by chain, newest first | Manual | Buka dari Renewal page, periksa grouping |
| HIST-02 | Mode renewal menampilkan tombol Renew hanya pada expired/akan expired yang belum di-renew | Manual | Cek sertifikat dengan berbagai status |
| HIST-03 | Mode readonly tidak menampilkan tombol Renew; klik nama pekerja di CDP membuka modal | Manual | Buka dari CertificationManagement |

### Wave 0 Gaps
- [ ] `Views/Shared/_CertificateHistoryModal.cshtml` — shell modal (belum ada)
- [ ] `Views/Shared/_CertificateHistoryModalContent.cshtml` — content partial (belum ada)
- [ ] `WorkerId` field di `SertifikatRow` (belum ada, blocking semua trigger)

---

## Open Questions

1. **Controller placement: AdminController vs CDPController?**
   - Yang diketahui: Halaman Renewal di Admin; CertificationManagement di CDP; BuildSertifikatRowsAsync ada di CDPController (private method)
   - Rekomendasi: Letakkan `CertificateHistoryAsync` di `AdminController` karena: (a) Admin/HC akses endpoint; (b) tidak membutuhkan role scoping dari BuildSertifikatRowsAsync yang kompleks; (c) query history tidak perlu l5OwnDataOnly
   - Alternatif: Buat helper method private di AdminController yang replikasi logic query sederhana (hanya filter by workerId, tidak perlu scoping)

2. **ChainTitle untuk group header: Judul atau Kategori?**
   - Yang diketahui: CONTEXT.md menyebut contoh "K3 Migas" — ini tampak seperti Kategori atau SubKategori, bukan Judul sertifikat
   - Rekomendasi: Gunakan `SubKategori` jika ada, fallback ke `Kategori`, fallback ke `Judul` sertifikat pertama (yang terlama) di chain — ini akan muncul konsisten untuk chain dan standalone

---

## Sources

### Primary (HIGH confidence)
- `Models/CertificationManagementViewModel.cs` — SertifikatRow fields, CertificateStatus enum, DeriveCertificateStatus
- `Models/AssessmentSession.cs` — RenewsSessionId, RenewsTrainingId
- `Models/TrainingRecord.cs` — RenewsSessionId, RenewsTrainingId
- `Controllers/CDPController.cs` lines 3194-3382 — BuildSertifikatRowsAsync, renewal chain batch lookup pattern
- `Views/Admin/RenewalCertificate.cshtml` — AJAX fetch pattern, AbortController, wireCheckboxes
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` — kolom Aksi yang akan ditambah icon history
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — kolom Nama yang akan dijadikan link
- `Views/Admin/ManageCategories.cshtml` — Bootstrap modal HTML pattern

### Secondary (MEDIUM confidence)
- Bootstrap 5 Modal docs — `new bootstrap.Modal(el).show()` API, `data-bs-dismiss="modal"` pattern

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua teknologi sudah ada dan digunakan di project
- Architecture: HIGH — pola AJAX partial view + Bootstrap modal sudah terbukti di codebase
- Data model: HIGH — SertifikatRow dan renewal FK sudah ada; satu field tambahan (WorkerId) teridentifikasi jelas
- Chain grouping algorithm: MEDIUM — logika benar secara konseptual tapi detail edge case (chain dengan > 3 level, orphaned FK) perlu diverifikasi saat implementasi

**Research date:** 2026-03-19
**Valid until:** 2026-04-19 (stack stabil)
