# Phase 269: Loading Overlay saat Koneksi SignalR Belum Ready di StartExam - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Menambahkan loading overlay di halaman StartExam yang memblokir interaksi user selama koneksi SignalR belum established. Overlay hilang setelah hub connected, atau berubah jadi error state jika gagal.

</domain>

<decisions>
## Implementation Decisions

### Tampilan Overlay
- **D-01:** Full-screen overlay semi-transparan menutupi seluruh halaman dengan spinner di tengah. Soal tetap di-render di background tapi tidak bisa diklik.
- **D-02:** Teks overlay: "Mempersiapkan ujian..." + status koneksi kecil di bawah (misal "Menghubungkan ke server..." → "Terhubung!" sebelum overlay hilang).
- **D-03:** Block semua interaksi termasuk keyboard (tab, arrow). Overlay punya z-index tinggi, elemen di belakang tidak bisa difokuskan.

### Timing & Transisi
- **D-04:** Overlay tampil minimal 1 detik supaya tidak flicker kalau koneksi cepat. Setelah `assessmentHubStartPromise` resolve DAN minimal 1 detik berlalu, overlay fade-out (~300ms).
- **D-05:** Timer tetap berjalan selama overlay tampil — server sudah mulai menghitung sejak StartExam dipanggil. Overlay hanya visual block, timer harus tetap akurat dengan server.

### Gagal Connect
- **D-06:** Jika `assessmentHubStartPromise` reject (hub gagal konek), overlay berubah jadi error state: spinner berhenti, teks berubah jadi "Koneksi gagal", tampilkan tombol "Muat Ulang" (reload page). Konsisten dengan existing `onclose` handler di assessment-hub.js.

### Resume Exam
- **D-07:** Overlay juga muncul saat resume exam (buka tab lagi setelah close). Hub perlu reconnect dulu — overlay tampil sampai hub ready, baru soal bisa diakses.

### Claude's Discretion
- Detail CSS (warna, opacity, animasi exact)
- Apakah perlu aria attributes untuk accessibility
- Cara block keyboard (tabindex=-1 vs inert attribute vs focus trap)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### SignalR Hub Infrastructure
- `wwwroot/js/assessment-hub.js` — Hub connection setup, expose `assessmentHubStartPromise` dan `window.assessmentHub`
- `Hubs/AssessmentHub.cs` — Server-side hub (JoinBatch, etc.)

### Exam Page
- `Views/CMP/StartExam.cshtml` — Halaman ujian yang akan ditambahkan overlay. Sudah punya `hubStatusBadge` (line ~25), `assessmentHubStartPromise.then()` usage (line ~930)

### Existing Patterns
- `wwwroot/css/assessment-hub.css` — Existing toast styling dari assessment hub

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `window.assessmentHubStartPromise` — Promise yang resolve saat hub connected, sudah di-expose dari assessment-hub.js
- `hubStatusBadge` — Badge di sticky header yang sudah menunjukkan status koneksi (Connecting... → Live)
- Bootstrap 5 modal/spinner patterns sudah digunakan di tempat lain (timeUpWarningModal, token modal)

### Established Patterns
- Assessment-hub.js onclose handler sudah punya pattern "persistent toast + Muat Ulang button"
- StartExam.cshtml sudah punya pattern `assessmentHubStartPromise.then()` untuk menunggu hub ready

### Integration Points
- Overlay HTML ditambahkan di StartExam.cshtml (sebelum/sesudah exam content)
- JavaScript logic menggunakan `assessmentHubStartPromise` yang sudah ada
- CSS bisa ditambahkan inline atau di file terpisah

</code_context>

<specifics>
## Specific Ideas

- Overlay harus muncul baik saat start pertama kali maupun saat resume exam
- Status koneksi di overlay berubah real-time: "Menghubungkan ke server..." → "Terhubung!"
- Error state menggantikan spinner + teks, bukan menambah elemen baru

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 269-loading-overlay-saat-koneksi-signalr-belum-ready-di-startexam*
*Context gathered: 2026-03-28*
