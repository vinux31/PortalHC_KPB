---
phase: 412
slug: live-monitoring-ui-signalr
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-21
---

# Phase 412 — UI Design Contract
# Live Monitoring UI + SignalR (v32.5)

> Enhancement ke halaman EXISTING `Views/Admin/AssessmentMonitoringDetail.cshtml`.
> Seluruh komponen baru WAJIB mengadopsi kelas Bootstrap + pola markup yang sudah ada.
> Tidak ada sistem warna, tipografi, atau spacing baru yang diperkenalkan.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Bootstrap 5 native — existing page; tidak ada shadcn/Radix) |
| Preset | not applicable |
| Component library | Bootstrap 5 modal/card/badge/dropdown (existing) |
| Icon library | Bootstrap Icons (`bi bi-*`) — already used throughout page |
| Font | inherited dari layout — tidak didefinisikan ulang |

**Catatan adopsi:** Semua komponen baru menggunakan kelas yang persis sama dengan yang sudah ada di halaman:
- Modal: `modal fade` + `modal-dialog` + `modal-content` + `modal-header bg-danger text-white` (pola `akhiriSemuaModal`)
- Card panel: `card border-0 shadow-sm mb-4` (pola token-card, extra-time-card)
- Tabel: `table table-hover align-middle mb-0` dalam `table-responsive` (pola per-user table)
- Badge: `badge bg-success/bg-warning text-dark/bg-secondary/bg-light text-dark border` (pola statusBadgeClass)
- Button: `btn btn-sm btn-danger/btn-outline-secondary/btn-outline-primary/btn-secondary` (pola aksi baris)

---

## Spacing Scale

Mengikuti spacing yang sudah dipakai halaman (Bootstrap utility classes):

| Token | Value | Bootstrap Class | Usage |
|-------|-------|-----------------|-------|
| xs | 4px | `gap-1`, `me-1`, `ms-1` | Icon gap dalam tombol, jarak inline |
| sm | 8px | `gap-2`, `me-2`, `py-2` | Jarak antar elemen dalam satu baris |
| md | 16px | `gap-3`, `px-3`, `py-3` | Padding card-body default |
| lg | 24px | `mb-4`, `py-4`, `px-4` | Jarak antar section |
| xl | 32px | `g-3 mb-4` (summary cards) | Layout row gaps |

Exceptions: touch-target minimum 40px pada semua button aksi baris (`style="min-height:40px;"`) — sudah dipakai di `⋮` dropdown dan btn-activity-log, WAJIB diterapkan ke tombol Hapus dan Restore.

---

## Typography

Mengikuti tipografi halaman existing — tidak ada deklarasi font baru:

| Role | Size | Weight | Line Height | Bootstrap Class |
|------|------|--------|-------------|-----------------|
| Body | 16px (browser default) | 400 | 1.5 | (default) |
| Label / small | 12–13px | 400 | 1.4 | `small`, `text-muted small` |
| Heading card | 20px (h5) | 600 | 1.2 | `h5 fw-semibold`, `h5 mb-0 fw-semibold` |
| Nama peserta | 16px | 600 | 1.4 | `fw-semibold` (td col-0) |

---

## Color

Mengadopsi penuh palet semantic Bootstrap yang sudah dipakai halaman:

| Role | Bootstrap Token | Hex Approx | Usage spesifik |
|------|-----------------|------------|----------------|
| Dominant (60%) | bg-white / bg-light | #fff / #f8f9fa | Surface halaman, card background, tabel header `table-light` |
| Secondary (30%) | bg-white card `border-0 shadow-sm` | #fff + shadow | Card panel (token, extra-time, tabel peserta, panel removed) |
| Accent primary (10%) | text-primary / `bg-primary` | #0d6efd | Badge kategori OJT, ikon section heading, teks NIP secondary |
| Destructive | `bg-danger text-white` | #dc3545 | Header modal keras, tombol konfirmasi hapus, badge InProgress kick |
| Warning/friction | `bg-warning text-dark` | #ffc107 | Header modal "sedang ujian", badge InProgress, tombol Tambah Peserta (selaras "Tambah Waktu" existing) |
| Success | `bg-success` | #198754 | Badge Completed, tombol View Results, badge "Peserta Berhasil Ditambah" toast |
| Secondary/muted | `bg-secondary` / `text-muted` | #6c757d | Badge not-started, kolom waktu/alasan di panel removed |

Accent reserved for:
- Heading icon section "Status Per-Peserta" (`text-primary`)
- Tautan breadcrumb aktif
- Tombol "Tambah Peserta" BUKAN primary biru — pakai `btn-outline-primary btn-sm` (ringan, non-destructive)

Border-left accent cards (WAJIB dipertahankan untuk panel baru):
- Panel "Peserta Dikeluarkan": `style="border-left: 4px solid #dc3545 !important;"` (merah, destructive semantic)
- Tombol "Tambah Peserta" card wrapper: tidak diperlukan card wrapper terpisah — tombol disisipkan di `card-header` existing (lihat layout section)

---

## Komponen Baru — Spesifikasi Detail

### 1. Tombol "Tambah Peserta" (Placement)

**Lokasi:** `card-header` section "Status Per-Peserta", baris yang sama dengan tombol "Ekspor Hasil" dan "Akhiri Semua Ujian", sebelum tombol Ekspor.

**Conditional render:** Tampilkan HANYA bila `Model.Category != "Assessment Proton"` (Proton dikecualikan per spec §F).

**Markup tombol:**
```html
<button type="button"
        class="btn btn-outline-primary btn-sm"
        id="btnTambahPeserta"
        data-bs-toggle="modal"
        data-bs-target="#tambahPesertaModal"
        title="Tambah peserta ke batch ujian ini">
    <i class="bi bi-person-plus me-1"></i>Tambah Peserta
</button>
```

---

### 2. Modal Picker "Tambah Peserta" (`#tambahPesertaModal`)

**Pola reuse:** Struktur mengikuti `#akhiriSemuaModal` (header berwarna, loading state, konten di-fetch async).

**Ukuran:** `modal-dialog modal-lg` (perlu ruang untuk daftar eligible).

**Markup modal:**
```html
<div class="modal fade" id="tambahPesertaModal" tabindex="-1"
     aria-labelledby="tambahPesertaModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title fw-semibold" id="tambahPesertaModalLabel">
                    <i class="bi bi-person-plus me-2"></i>Tambah Peserta
                </h5>
                <button type="button" class="btn-close btn-close-white"
                        data-bs-dismiss="modal" aria-label="Tutup"></button>
            </div>
            <div class="modal-body">
                <!-- Loading state -->
                <div id="tambahPesertaLoading" class="text-center py-3">
                    <span class="spinner-border spinner-border-sm me-2"></span>Memuat daftar peserta...
                </div>
                <!-- Eligible list -->
                <div id="tambahPesertaContent" style="display:none;">
                    <p class="text-muted small mb-2">
                        Pilih satu atau lebih pekerja untuk ditambahkan ke batch ini.
                    </p>
                    <div id="tambahPesertaEmpty" style="display:none;" class="text-center text-muted py-3">
                        <i class="bi bi-people fs-3 d-block mb-2"></i>
                        Semua pekerja aktif sudah terdaftar dalam batch ini.
                    </div>
                    <div id="tambahPesertaList">
                        <!-- Diisi JS: checkbox list -->
                    </div>
                </div>
                <!-- Error state -->
                <div id="tambahPesertaError" style="display:none;" class="alert alert-danger mb-0">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    <span id="tambahPesertaErrorMsg">Gagal memuat daftar peserta.</span>
                    Tutup dan coba lagi.
                </div>
            </div>
            <div class="modal-footer" id="tambahPesertaFooter" style="display:none;">
                <span class="me-auto text-muted small" id="tambahPesertaSelectedCount">0 dipilih</span>
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <button type="button" class="btn btn-primary" id="btnKonfirmasiTambah" disabled>
                    <i class="bi bi-check-lg me-1"></i>Tambah (<span id="tambahCount">0</span>)
                </button>
            </div>
        </div>
    </div>
</div>
```

**Struktur setiap item di `#tambahPesertaList`** (dirender oleh JS per item dari JSON `[{id, fullName, nip}]`):
```html
<div class="form-check py-1 border-bottom">
    <input class="form-check-input tambah-peserta-check" type="checkbox"
           value="{id}" id="check_{id}" />
    <label class="form-check-label w-100" for="check_{id}">
        <span class="fw-semibold">{fullName}</span>
        <span class="text-muted small ms-2">{nip}</span>
    </label>
</div>
```

**Perilaku JS modal:**
- On `show.bs.modal`: fetch `GET /Admin/GetEligibleParticipantsToAdd?sessionId={representativeId}`, tampilkan loading.
- On fetch success: bila array kosong → tampilkan `#tambahPesertaEmpty`, sembunyikan footer. Bila ada data → render checklist, tampilkan footer.
- On fetch error: tampilkan `#tambahPesertaError`.
- On checkbox change: hitung yang dicheck, update `#tambahPesertaSelectedCount` dan `#tambahCount`, toggle `disabled` pada `#btnKonfirmasiTambah`.
- On `#btnKonfirmasiTambah` click: POST ke `/Admin/AddParticipantsLive` dengan `sessionId` + `userIds[]` + antiforgery token. Tombol → loading state (`spinner-border-sm`). On success: tutup modal, tampilkan toast sukses (reuse `showAssessmentToast`). On error: tampilkan pesan error dalam modal (jangan tutup).
- Sumber kebenaran baris baru: SignalR `participantAdded` event (BUKAN DOM inject langsung dari response POST). Bila `participantAdded` tidak datang dalam 3 detik setelah sukses POST, inject dari `added[]` payload sebagai fallback (dedup by sessionId).

---

### 3. Tombol Hapus Per-Baris

**Lokasi:** Kolom "Aksi" — disisipkan sebagai item baru di `<ul class="dropdown-menu dropdown-menu-end">` existing per baris (bawah item Reset, atas item Akhiri Ujian). ATAU sebagai tombol standalone sebelum dropdown `⋮` — pilih dropdown untuk menghindari overflow baris.

**Conditional render:** JANGAN tampilkan untuk sesi Proton. Cek `data-category` dari hidden field `#hCategory`.

**Item dropdown:**
```html
<li>
    <button type="button"
            class="dropdown-item text-danger btn-hapus-peserta"
            data-session-id="{sessionId}"
            data-worker-name="{fullName}"
            data-status="{DeriveUserStatus}"
            data-has-cert="{true/false}"
            style="min-height:40px;">
        <i class="bi bi-person-x me-1"></i>Hapus Peserta
    </button>
</li>
```

**Data attribute `data-status`:** diisi dari `session.UserStatus` (Razor server-side) — nilai: `InProgress`, `Completed`, `Not started`, `Cancelled`, `Abandoned`, `Menunggu Penilaian`.

**Data attribute `data-has-cert`:** `true` bila `session.NomorSertifikat != null && session.NomorSertifikat != ""` (Razor server-side).

Untuk baris yang di-inject SignalR (`participantAdded`): item hapus di-inject oleh `buildActionsHtml()` yang diperluas, dengan `data-status` dari payload.

---

### 4. Modal Konfirmasi Hapus — RINGAN (`#hapusPesertaLightModal`)

**Trigger:** baris dengan `data-status` bukan `InProgress` DAN bukan (`Completed` + `data-has-cert="true"`).
Status yang masuk jalur ringan: `Not started`, `Cancelled`, `Abandoned`, `Menunggu Penilaian`, `Completed` tanpa sertifikat.

**Pola reuse:** `modal-dialog` kecil, header netral (bukan bg-danger), tombol Hapus warna `btn-danger`.

```html
<div class="modal fade" id="hapusPesertaLightModal" tabindex="-1"
     aria-labelledby="hapusPesertaLightLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title fw-semibold" id="hapusPesertaLightLabel">
                    <i class="bi bi-person-x me-2 text-danger"></i>Hapus Peserta
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
            </div>
            <div class="modal-body">
                <p class="mb-2">Hapus <strong id="hapusLightWorkerName"></strong> dari batch ini?</p>
                <div class="mb-3">
                    <label for="hapusLightReason" class="form-label fw-semibold">
                        Alasan penghapusan
                        <span class="text-muted small fw-normal">
                            (wajib bila peserta sudah mengerjakan)
                        </span>
                    </label>
                    <input type="text" id="hapusLightReason" class="form-control"
                           maxlength="500"
                           placeholder="Masukkan alasan..." />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <button type="button" class="btn btn-danger" id="btnHapusLightKonfirmasi">
                    <i class="bi bi-person-x me-1"></i>Hapus
                </button>
            </div>
        </div>
    </div>
</div>
```

---

### 5. Modal Konfirmasi Hapus — KERAS (`#hapusPesertaHardModal`)

**Trigger:** baris dengan `data-status === "InProgress"` ATAU (`data-status === "Completed"` DAN `data-has-cert === "true"`).

**Extra-friction markers (D-01):**
- Header `bg-danger text-white` (sama dengan `#akhiriSemuaModal`)
- Ikon `bi-exclamation-octagon-fill` (lebih berat dari triangle)
- Teks peringatan eksplisit sesuai status
- Tombol konfirmasi pakai teks tegas, bukan hanya "Hapus"

```html
<div class="modal fade" id="hapusPesertaHardModal" tabindex="-1"
     data-bs-backdrop="static" data-bs-keyboard="false"
     aria-labelledby="hapusPesertaHardLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-danger text-white">
                <h5 class="modal-title fw-semibold" id="hapusPesertaHardLabel">
                    <i class="bi bi-exclamation-octagon-fill me-2"></i>Konfirmasi Hapus Peserta
                </h5>
                <!-- Tidak ada btn-close: backdrop=static, harus klik "Batal" -->
            </div>
            <div class="modal-body">
                <!-- Warning block (InProgress) -->
                <div id="hapusHardWarnInProgress" style="display:none;">
                    <div class="alert alert-danger d-flex align-items-start gap-2 mb-3">
                        <i class="bi bi-exclamation-octagon-fill fs-5 flex-shrink-0 mt-1"></i>
                        <div>
                            <strong>Peserta sedang mengerjakan ujian.</strong><br>
                            <span class="small">Menghapus peserta ini akan <strong>memaksa keluar</strong> dari ujian secara langsung.</span>
                        </div>
                    </div>
                </div>
                <!-- Warning block (Completed + cert) -->
                <div id="hapusHardWarnCompleted" style="display:none;">
                    <div class="alert alert-warning d-flex align-items-start gap-2 mb-3">
                        <i class="bi bi-shield-exclamation fs-5 flex-shrink-0 mt-1"></i>
                        <div>
                            <strong>Peserta sudah menyelesaikan ujian dan memiliki sertifikat.</strong><br>
                            <span class="small">Data jawaban dan sertifikat akan diarsipkan (soft-remove, dapat dipulihkan).</span>
                        </div>
                    </div>
                </div>

                <p class="mb-2">Hapus <strong id="hapusHardWorkerName"></strong> dari batch ini?</p>

                <div class="mb-3">
                    <label for="hapusHardReason" class="form-label fw-semibold">
                        Alasan penghapusan
                        <span class="text-muted small fw-normal">
                            (wajib bila peserta sudah mengerjakan)
                        </span>
                    </label>
                    <input type="text" id="hapusHardReason" class="form-control"
                           maxlength="500"
                           placeholder="Masukkan alasan penghapusan..." />
                    <div class="form-text text-danger d-none" id="hapusHardReasonError">
                        Alasan wajib diisi untuk peserta ini.
                    </div>
                </div>

                <p class="text-danger small mb-0">
                    <i class="bi bi-exclamation-triangle-fill me-1"></i>
                    Tindakan ini akan dicatat di audit log.
                </p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <button type="button" class="btn btn-danger fw-semibold" id="btnHapusHardKonfirmasi">
                    <i class="bi bi-person-x me-1"></i>Ya, Hapus Peserta Ini
                </button>
            </div>
        </div>
    </div>
</div>
```

**Perilaku JS shared untuk kedua modal hapus:**
- Handler `click` pada `.btn-hapus-peserta` (delegated event, `document.addEventListener('click', ...)`):
  - Baca `data-session-id`, `data-worker-name`, `data-status`, `data-has-cert`
  - Tentukan tingkat: InProgress → HARD; Completed + has-cert=true → HARD; lainnya → LIGHT
  - Populasi nama di `#hapusLightWorkerName` atau `#hapusHardWorkerName`
  - Bila HARD: tampilkan blok warning yang sesuai (InProgress atau Completed)
  - Reset field reason ke kosong sebelum show
  - Buka modal yang sesuai via `new bootstrap.Modal(...).show()`
- Handler `click` pada `#btnHapusLightKonfirmasi` / `#btnHapusHardKonfirmasi`:
  - Baca `sessionId` dari variabel closure JS (disimpan saat modal dibuka)
  - Baca `reason` dari input field masing-masing
  - POST ke `/Admin/RemoveParticipantLive` dengan `sessionId` + `reason` + antiforgery
  - Tombol → loading state selama fetch
  - On success (HTTP 200, `mode: "soft"` atau `"hard"`):
    - Tutup modal
    - Sumber kebenaran perubahan baris: SignalR `participantRemoved` event. Bila tidak datang dalam 3 detik, jalankan fallback DOM: pindahkan `<tr data-session-id>` dari tbody aktif ke panel removed.
  - On error (HTTP 400/404/500): tampilkan pesan error dalam modal (jangan tutup).

---

### 6. Panel "Peserta Dikeluarkan" (collapsible)

**Lokasi:** Di bawah card tabel "Status Per-Peserta", di atas section Essay Grading (bila ada).

**Render server-side:** Panel di-render server-side bila ada soft-removed sessions di batch. Data soft-removed diload di `AssessmentMonitoringDetail` controller action (`:3273`). Bila tidak ada soft-removed → panel tidak dirender (bukan panel kosong tersembunyi).

**Styling panel:** Card dengan `border-left: 4px solid #dc3545` — konsisten dengan destructive semantic, sejajar token-card dan extra-time-card.

```html
<div class="card border-0 shadow-sm mt-4" style="border-left: 4px solid #dc3545 !important;"
     id="panelPesertaDikeluarkan">
    <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center"
         id="panelPesertaDikeluarkanHeader"
         style="cursor:pointer;"
         data-bs-toggle="collapse"
         data-bs-target="#panelPesertaDikeluarkanBody"
         aria-expanded="false"
         aria-controls="panelPesertaDikeluarkanBody">
        <h5 class="mb-0 fw-semibold">
            <i class="bi bi-person-slash me-2 text-danger"></i>
            Peserta Dikeluarkan
            <span class="badge bg-secondary ms-2 small fw-normal" id="countRemoved">
                @(ViewBag.RemovedSessions?.Count ?? 0)
            </span>
        </h5>
        <i class="bi bi-chevron-down text-muted" id="panelRemovedChevron"></i>
    </div>
    <div class="collapse" id="panelPesertaDikeluarkanBody"
         aria-labelledby="panelPesertaDikeluarkanHeader">
        <div class="card-body p-0">
            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0" id="tblRemoved">
                    <thead class="table-light">
                        <tr>
                            <th>Nama</th>
                            <th>Waktu Dikeluarkan</th>
                            <th>Oleh</th>
                            <th>Alasan</th>
                            <th>Aksi</th>
                        </tr>
                    </thead>
                    <tbody id="tbodyRemoved">
                        @* Diisi Razor server-side dari ViewBag.RemovedSessions *@
                        @* Format baris: lihat spesifikasi baris removed di bawah *@
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
```

**Chevron rotate JS:** On Bootstrap `shown.bs.collapse` / `hidden.bs.collapse` event pada `#panelPesertaDikeluarkanBody` → toggle `rotate(180deg)` pada `#panelRemovedChevron` via `style.transform`.

**Baris tabel removed (Razor server-side + mirror JS untuk inject baru):**
```html
<tr data-session-id="{sessionId}" data-removed="true">
    <td class="fw-semibold">{fullName}
        <br><small class="text-muted">{nip}</small>
    </td>
    <td class="text-muted small">
        @{removedAt.ToString("dd MMM yyyy HH:mm")} WIB
    </td>
    <td class="text-muted small">{removedByDisplayName}</td>
    <td class="text-muted small" style="max-width:200px; word-break:break-word;">
        @{Html.Encode(removalReason) bila tidak kosong, atau "—"}
    </td>
    <td>
        <button type="button"
                class="btn btn-outline-success btn-sm btn-restore-peserta"
                data-session-id="{sessionId}"
                data-worker-name="{fullName}"
                style="min-height:40px;"
                title="Pulihkan peserta ke batch aktif">
            <i class="bi bi-arrow-counterclockwise me-1"></i>Restore
        </button>
    </td>
</tr>
```

**Kolom "Oleh":** tampilkan `RemovedBy` sebagai nama tampil (controller resolve UserId → FullName) atau NIP fallback.

**Kolom "Alasan":** XSS-safe: gunakan `Html.Encode()` Razor (server-side) atau `textContent =` (JS inject). JANGAN `innerHTML` untuk kolom ini.

---

### 7. Tombol Restore (D-04 — 1-klik, tanpa konfirmasi)

**Perilaku JS:**
- Handler `click` pada `.btn-restore-peserta` (delegated event):
  - Baca `data-session-id`, `data-worker-name`
  - Disable tombol, tampilkan spinner inline: `<span class="spinner-border spinner-border-sm"></span>`
  - POST ke `/Admin/RestoreParticipantLive` dengan `sessionId` + antiforgery
  - On success: sumber kebenaran = SignalR `participantAdded` event (baris balik ke tabel aktif live). Bila tidak datang dalam 3 detik: fallback — pindahkan `<tr data-session-id data-removed="true">` dari `#tbodyRemoved` ke `<tbody>` aktif, update `data-removed` → hapus atribut, inject kolom yang sesuai.
  - On error: re-enable tombol, tampilkan `alert()` singkat (konsisten dengan pola reshuffle error): `'Gagal memulihkan peserta. Silakan coba lagi.'`
  - Update `#countRemoved` setelah sukses.

---

### 8. SignalR Client Handlers (AssessmentMonitoringDetail.cshtml)

Disisipkan setelah handler `workerAnswerEdited` yang sudah ada (setelah baris ~:1411), masih dalam `@section Scripts`.

#### 8a. `participantAdded`

**Payload shape (dari 410 endpoint):**
```json
{
  "sessionId": 123,
  "userId": "abc",
  "fullName": "Budi Santoso",
  "nip": "12345678",
  "status": "Not started",
  "hasPackages": false
}
```

**Perilaku:**
```
window.assessmentHub.on('participantAdded', function(data) {
    // 1. Dedup: bila <tr data-session-id="{data.sessionId}"> sudah ada di tbody aktif, skip
    // 2. Bila <tr data-session-id data-removed="true"> ada di #tbodyRemoved, pindahkan ke tbody aktif
    //    (kasus Restore — baris sudah ada tinggal dipindah)
    // 3. Bila benar-benar baru: inject <tr data-session-id="{data.sessionId}"> baru ke tbody aktif
    //    dengan kolom: Nama (fw-semibold), Progress (—), Status (badge Not started),
    //    Nilai (—), Hasil (text-muted —), Selesai Pada (—), Aksi (buildActionsHtml)
    // 4. flashRow(tr, 'flash-update')  — reuse helper existing
    // 5. updateSummaryFromDOM()       — reuse helper existing
    // 6. Update #countRemoved bila baris berasal dari panel removed
    // 7. showAssessmentToast(data.fullName + ' ditambahkan ke batch')
    // 8. Update #last-updated-time
});
```

**Baris baru yang di-inject:** Kolom Aksi menggunakan `buildActionsHtml()` yang diperluas (sudah include tombol Hapus Peserta). `data-status="Not started"`, `data-has-cert="false"`.

#### 8b. `participantRemoved`

**Payload shape (dari 411 endpoint):**
```json
{
  "sessionId": 123,
  "mode": "soft",
  "fullName": "Budi Santoso",
  "nip": "12345678",
  "removedAt": "2026-06-21T07:30:00Z",
  "removedBy": "Admin Name",
  "removalReason": "Alasan penghapusan"
}
```

**Perilaku:**
```
window.assessmentHub.on('participantRemoved', function(data) {
    var tr = document.querySelector('tbody tr[data-session-id="' + data.sessionId + '"]:not([data-removed])');
    if (!tr) return;  // sudah tidak ada atau sudah di panel removed

    if (data.mode === 'hard') {
        // Hard-delete: hapus baris dari DOM saja, tidak masuk panel removed
        tr.remove();
    } else {
        // Soft-remove: pindahkan ke #tbodyRemoved
        // 1. Bangun <tr data-session-id data-removed="true"> dengan kolom removed
        // 2. Escape reason dan nama via textContent (XSS-safe)
        // 3. Inject ke #tbodyRemoved (prepend — terbaru di atas)
        // 4. Hapus baris dari tbody aktif: tr.remove()
        // 5. Tampilkan / expand panel #panelPesertaDikeluarkan bila tersembunyi
        // 6. Update #countRemoved++
    }
    updateSummaryFromDOM();
    showAssessmentToast(data.fullName + ' dikeluarkan dari batch');
    // Update #last-updated-time
});
```

**Panel auto-expand:** Bila panel removed belum ada di DOM (belum ada removed sebelumnya, server tidak render), inject panel ke DOM sebelum inject baris. Atau: server selalu render panel (meski kosong) bila `IsProtonBatch == false`, dengan collapsed state default.

**Rekomendasi (Claude's discretion):** Server selalu render `#panelPesertaDikeluarkan` (collapsed, kosong) untuk batch non-Proton agar JS tidak perlu inject panel dari scratch. Panel hanya tampil visible setelah ada minimal 1 baris di `#tbodyRemoved`.

---

### 9. Force-Kick Worker — `examRemoved` pada StartExam.cshtml

**File target:** `Views/CMP/StartExam.cshtml`

**Pola reuse:** Identik dengan `examClosed` handler (baris ~:1254), termasuk modal non-dismissable `data-bs-backdrop="static" data-bs-keyboard="false"`.

**Payload `examRemoved`:**
```json
{ "reason": "Alasan dari admin" }
```

**Modal baru `#examRemovedModal`** (disisipkan di bawah `#sessionResetModal` ~:359):
```html
<div class="modal fade" id="examRemovedModal" tabindex="-1"
     data-bs-backdrop="static" data-bs-keyboard="false"
     aria-hidden="true"
     style="z-index: 9999;">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-header bg-danger text-white border-bottom-0 pb-0">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-person-x me-2"></i>Dikeluarkan dari Ujian
                </h5>
            </div>
            <div class="modal-body text-center py-3">
                <p class="mb-2 fw-semibold">Anda telah dikeluarkan dari ujian ini.</p>
                <p class="text-muted small mb-0" id="examRemovedReasonText" style="display:none;">
                    Alasan: <em id="examRemovedReasonValue"></em>
                </p>
                <p class="text-muted small mt-2 mb-0">
                    Anda akan diarahkan ke daftar ujian dalam
                    <strong id="removedCountdown">5</strong> detik...
                </p>
            </div>
            <div class="modal-footer justify-content-center border-top-0">
                <button type="button" class="btn btn-danger fw-semibold" id="btnExamRemovedKembali">
                    <i class="bi bi-arrow-left me-1"></i>Kembali ke Daftar Ujian
                </button>
            </div>
        </div>
    </div>
</div>
```

**Handler JS `examRemoved`** (disisipkan setelah handler `examClosed` ~:1284):
```javascript
window.assessmentHub.on('examRemoved', function(payload) {
    if (examClosed) return;  // dual-trigger guard (reuse var existing)
    examClosed = true;
    clearInterval(timerInterval);
    clearInterval(saveInterval);
    window.onbeforeunload = null;

    // Tampilkan alasan bila ada
    if (payload && payload.reason) {
        document.getElementById('examRemovedReasonValue').textContent = payload.reason;
        document.getElementById('examRemovedReasonText').style.display = '';
    }

    var redirectTarget = '@Url.Action("Assessment", "CMP")';
    var modal = new bootstrap.Modal(document.getElementById('examRemovedModal'));
    modal.show();

    document.getElementById('btnExamRemovedKembali').onclick = function() {
        window.location.href = redirectTarget;
    };

    var countdown = 5;
    var countdownEl = document.getElementById('removedCountdown');
    var countdownInterval = setInterval(function() {
        countdown--;
        if (countdownEl) countdownEl.textContent = countdown;
        if (countdown <= 0) {
            clearInterval(countdownInterval);
            window.location.href = redirectTarget;
        }
    }, 1000);
});
```

**Banner TempData (D-02):** Server set `TempData["Error"] = "Anda telah dikeluarkan dari ujian ini."` di endpoint `RemoveParticipantLive` setelah mengirim `examRemoved` — ditampilkan di halaman `/CMP/Assessment` setelah redirect. TempData["Error"] sudah dirender oleh layout/halaman assessment list (verifikasi saat implement).

---

## Copywriting Contract

| Element | Copy (Bahasa Indonesia) |
|---------|------------------------|
| Primary CTA tambah peserta | "Tambah Peserta" (`bi-person-plus`) |
| CTA konfirmasi tambah | "Tambah (N)" — N = jumlah dipilih |
| CTA hapus — jalur ringan | "Hapus" |
| CTA hapus — jalur keras | "Ya, Hapus Peserta Ini" |
| CTA restore | "Restore" (`bi-arrow-counterclockwise`) |
| Heading panel removed | "Peserta Dikeluarkan" |
| Empty state eligible list | "Semua pekerja aktif sudah terdaftar dalam batch ini." |
| Label field alasan | "Alasan penghapusan (wajib bila peserta sudah mengerjakan)" |
| Placeholder field alasan ringan | "Masukkan alasan..." |
| Placeholder field alasan keras | "Masukkan alasan penghapusan..." |
| Warning InProgress | "Peserta sedang mengerjakan ujian. Menghapus peserta ini akan memaksa keluar dari ujian secara langsung." |
| Warning Completed+cert | "Peserta sudah menyelesaikan ujian dan memiliki sertifikat. Data jawaban dan sertifikat akan diarsipkan (soft-remove, dapat dipulihkan)." |
| Catatan audit | "Tindakan ini akan dicatat di audit log." |
| Toast tambah | "{fullName} ditambahkan ke batch" |
| Toast hapus | "{fullName} dikeluarkan dari batch" |
| Toast restore | "{fullName} dipulihkan ke batch aktif" |
| Error fetch eligible | "Gagal memuat daftar peserta. Tutup dan coba lagi." |
| Error fetch POST | Pesan dari server (`data.message`) atau "Terjadi kesalahan. Silakan coba lagi." |
| Force-kick modal heading | "Dikeluarkan dari Ujian" |
| Force-kick modal body | "Anda telah dikeluarkan dari ujian ini." |
| Force-kick countdown | "Anda akan diarahkan ke daftar ujian dalam N detik..." |
| Force-kick button | "Kembali ke Daftar Ujian" |
| Force-kick TempData banner | "Anda telah dikeluarkan dari ujian ini." |
| Force-kick reason label | "Alasan: {reason}" (tampil hanya bila reason tidak kosong) |
| Error restore | "Gagal memulihkan peserta. Silakan coba lagi." |

---

## Layout Visual — Urutan Elemen Halaman (setelah Phase 412)

```
[Breadcrumb]
[Kembali ke Monitoring]
[Header: Title + Category badge + Status badge + Hub status badge]
[Token Card]                ← existing, hanya bila IsTokenRequired
[Tambah Waktu Card]         ← existing
[Summary Cards row]         ← existing (7 kartu, count-total stays = active only)
[Card "Status Per-Peserta"] ← existing + tombol "Tambah Peserta" di card-header
    [thead]
    [tbody: baris aktif] ← kolom Aksi diperluas dengan item "Hapus Peserta"
[Card "Peserta Dikeluarkan"] ← BARU, collapsed by default, border-left merah
    [thead: Nama/Waktu/Oleh/Alasan/Aksi]
    [tbody: baris soft-removed + tombol Restore]
[Section Essay Grading]     ← existing, bila ada
[Section Proton Interview]  ← existing, bila isProtonInterview
[Antiforgery form hidden]   ← existing
[Hidden fields hTitle/hCategory/hScheduleDate] ← existing
[Last updated indicator]    ← existing
```

---

## Summary Count — Kontrak Update

`updateSummaryFromDOM()` (existing) menghitung baris dari `tbody tr[data-session-id]`. Setelah Phase 412:
- Query selector HARUS dikecualikan baris di `#tbodyRemoved`: gunakan `tbody:not(#tbodyRemoved) tr[data-session-id]` atau flag `data-removed="true"`.
- `count-total` = peserta aktif saja (exclude removed) — sesuai Phase 409 query server-side.
- Tidak ada kartu summary baru untuk "Dikeluarkan" — hanya badge count di header panel.

---

## Accessibility

| Komponen | Requirement |
|----------|-------------|
| Semua modal | `aria-labelledby` + `aria-hidden="true"` + `role="dialog"` (sudah di Bootstrap modal) |
| Modal keras | `data-bs-backdrop="static" data-bs-keyboard="false"` — tidak dismiss via ESC/overlay click |
| Modal ringan | dismissable: `btn-close` + ESC + overlay click |
| Picker checklist | `for`/`id` pair per checkbox, label mencakup nama + NIP |
| Tombol hapus per-baris | `title="Hapus {nama} dari batch"` |
| Tombol restore | `title="Pulihkan {nama} ke batch aktif"` |
| Panel collapsible | `aria-expanded` toggle via Bootstrap `data-bs-toggle="collapse"` |
| Force-kick modal | `data-bs-backdrop="static"` — tidak dismiss; tombol navigasi eksplisit |
| XSS: kolom Alasan | `textContent` (JS) / `Html.Encode()` (Razor) — TIDAK `innerHTML` |
| Min touch target | `style="min-height:40px;"` pada semua tombol aksi baris |

---

## SignalR Contract

| Event | Direction | Grup | Payload Fields | Trigger |
|-------|-----------|------|----------------|---------|
| `participantAdded` | Server → Monitor | `monitor-{batchKey}` | `sessionId, userId, fullName, nip, status` | Post-commit AddParticipantsLive + RestoreParticipantLive |
| `participantRemoved` | Server → Monitor | `monitor-{batchKey}` | `sessionId, mode, fullName, nip, removedAt, removedBy, removalReason` | Post-commit RemoveParticipantLive |
| `examRemoved` | Server → Worker | Grup user / personal | `reason` | RemoveParticipantLive bila target InProgress |

**Dedup rule:** Client cek `document.querySelector('tbody tr[data-session-id="{id}"]:not([data-removed])')` sebelum inject. Bila sudah ada → skip inject.

**Broadcast guard:** Hanya dikirim setelah `CommitAsync()` sukses (tidak pada rollback).

**Rejoin after reconnect:** Pola existing `window.assessmentHub.onreconnected` sudah handle `JoinMonitor`. Tidak ada perubahan.

---

## Pola Antiforgery — Semua POST AJAX Baru

Semua POST baru menggunakan token dari `#antiforgeryForm` existing:
```javascript
var token = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]').value;
// kirim sebagai form body field: __RequestVerificationToken={token}
// atau header: 'RequestVerificationToken': token  (konsisten dengan regenToken existing)
```

Gunakan `Content-Type: application/x-www-form-urlencoded` dan field body — konsisten dengan pola `addExtraTime()` existing.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5 (existing) | modal, card, badge, table, collapse, toast | not required — existing dependency |
| Bootstrap Icons (existing) | bi-person-plus, bi-person-x, bi-person-slash, bi-arrow-counterclockwise, bi-exclamation-octagon-fill, bi-shield-exclamation, bi-chevron-down | not required — existing dependency |
| SignalR (existing) | assessmentHub client | not required — existing wiring |
| Third-party | none | not applicable |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending

---

*Phase: 412-live-monitoring-ui-signalr*
*UI-SPEC created: 2026-06-21*
*Source: CONTEXT.md D-01..D-04 (locked) + existing page markup scan*
