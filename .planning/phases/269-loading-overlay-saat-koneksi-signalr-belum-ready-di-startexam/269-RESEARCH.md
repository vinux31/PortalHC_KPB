# Phase 269: Loading Overlay SignalR - Research

**Researched:** 2026-03-28
**Domain:** Frontend JavaScript — SignalR connection lifecycle + CSS overlay UX
**Confidence:** HIGH

## Summary

Phase ini menambahkan full-screen loading overlay di `StartExam.cshtml` yang memblokir semua interaksi user sampai `window.assessmentHubStartPromise` resolve. Semua infrastruktur yang dibutuhkan sudah tersedia: promise sudah di-expose dari `assessment-hub.js`, Bootstrap 5 sudah digunakan di halaman yang sama, dan pola CSS overlay semi-transparan adalah teknik CSS murni tanpa dependensi eksternal baru.

Implementasi terdiri dari tiga bagian: (1) HTML overlay element di dalam StartExam.cshtml, (2) CSS untuk overlay + transisi fade, dan (3) JavaScript logic yang mengintegrasikan `assessmentHubStartPromise` dengan state management overlay (loading → ready → error). Tidak ada library baru yang perlu diinstall.

**Primary recommendation:** Tambahkan overlay HTML + CSS inline di StartExam.cshtml, lakukan orchestrasi via JavaScript yang sudah ada di file tersebut menggunakan `assessmentHubStartPromise` yang sudah di-expose.

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Full-screen overlay semi-transparan, spinner di tengah. Soal tetap di-render di background tapi tidak bisa diklik.
- **D-02:** Teks: "Mempersiapkan ujian..." + status kecil di bawah ("Menghubungkan ke server..." → "Terhubung!" sebelum fade-out).
- **D-03:** Block semua interaksi termasuk keyboard (tab, arrow). Z-index tinggi, elemen di belakang tidak bisa difokuskan.
- **D-04:** Overlay tampil minimal 1 detik. Setelah `assessmentHubStartPromise` resolve DAN minimal 1 detik berlalu, overlay fade-out (~300ms).
- **D-05:** Timer tetap berjalan selama overlay tampil — overlay hanya visual block.
- **D-06:** Jika `assessmentHubStartPromise` reject, overlay berubah jadi error state: spinner berhenti, teks "Koneksi gagal", tombol "Muat Ulang". Konsisten dengan existing `onclose` handler.
- **D-07:** Overlay juga muncul saat resume exam.

### Claude's Discretion
- Detail CSS (warna, opacity, animasi exact)
- Apakah perlu aria attributes untuk accessibility
- Cara block keyboard (tabindex=-1 vs inert attribute vs focus trap)

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — discussion stayed within phase scope
</user_constraints>

## Standard Stack

### Core (sudah tersedia, tidak perlu install baru)
| Asset | Lokasi | Purpose |
|-------|--------|---------|
| Bootstrap 5 | Sudah di-include di layout | Spinner class (`spinner-border`), utility classes |
| `window.assessmentHubStartPromise` | `wwwroot/js/assessment-hub.js` line 93 | Promise resolve saat hub connected |
| `window.assessmentHub` | `wwwroot/js/assessment-hub.js` line 95 | Akses ke connection state |
| CSS custom | Inline di StartExam.cshtml atau assessment-hub.css | Overlay styling |

**Tidak ada package baru yang perlu diinstall.**

## Architecture Patterns

### Struktur Overlay HTML

```html
<!-- Overlay: tampil saat hub belum ready, dihapus/fade-out saat ready -->
<div id="examLoadingOverlay">
    <div class="overlay-content">
        <div class="spinner-border text-light" role="status"></div>
        <div class="overlay-title">Mempersiapkan ujian...</div>
        <div class="overlay-status" id="overlayStatus">Menghubungkan ke server...</div>
        <!-- Error state: hidden by default -->
        <button id="overlayReloadBtn" style="display:none" onclick="window.location.reload()">Muat Ulang</button>
    </div>
</div>
```

**Posisi di HTML:** Tambahkan sebelum exam content (setelah sticky header closing tag, sebelum `<div class="container-fluid py-3">`). Overlay harus OUTSIDE dari conditional `@if (!Model.Questions.Any())` block — harus muncul bahkan jika ada konten.

### Pattern: Promise.all untuk Minimum Duration

```javascript
// D-04: minimal 1 detik + hub connected
var minDelay = new Promise(function(resolve) { setTimeout(resolve, 1000); });
Promise.all([window.assessmentHubStartPromise, minDelay])
    .then(function() {
        // Hub connected DAN 1 detik berlalu — fade out overlay
        overlayFadeOut();
    })
    .catch(function() {
        // Hub gagal — tampilkan error state
        overlayShowError();
    });
```

**Confidence:** HIGH — ini adalah pola standar JavaScript untuk "minimum display time" pada loading state.

### Pattern: Blocking Keyboard (D-03)

Tiga opsi dengan tradeoff:

| Cara | Browser Support | Effort | Rekomendasi |
|------|----------------|--------|-------------|
| `inert` attribute pada exam container | Modern browsers (Chrome 102+, FF 112+, Safari 15.5+) | Minimal — 1 attribute | DIREKOMENDASIKAN |
| `tabindex="-1"` semua focusable elements | Universal | Tinggi — harus enumerate semua elemen | Tidak disarankan |
| Focus trap (custom JS) | Universal | Medium | Overkill untuk use case ini |

**Rekomendasi:** Gunakan `inert` attribute pada exam container `<div class="container-fluid py-3">`. Saat overlay tampil, set `examContainer.inert = true`. Saat overlay hilang, set `examContainer.inert = false`. Attribute `inert` secara otomatis disable semua pointer events, focus, dan keyboard interaction pada subtree.

**Catatan:** Sticky header (`examHeader`) tidak perlu di-inert karena ada di luar container. Timer berjalan via JavaScript, tidak memerlukan user interaction.

### Pattern: CSS Overlay

```css
#examLoadingOverlay {
    position: fixed;
    top: 0; left: 0;
    width: 100%; height: 100%;
    background: rgba(0, 0, 0, 0.7);
    z-index: 2000; /* lebih tinggi dari examHeader z-index: 1020 */
    display: flex;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    color: #fff;
    opacity: 1;
    transition: opacity 0.3s ease;
}
#examLoadingOverlay.fade-out {
    opacity: 0;
    pointer-events: none;
}
```

Fade-out via: tambah class `fade-out` → setelah 300ms → `display: none` atau `remove()`.

### Pattern: Error State (D-06)

Konsisten dengan existing `showPersistentToast` di assessment-hub.js — **jangan duplikasi UI**, buat error state langsung di dalam overlay element:

```javascript
function overlayShowError() {
    // Ganti konten: stop spinner, ubah teks, tampilkan tombol
    document.querySelector('#examLoadingOverlay .spinner-border').style.display = 'none';
    document.getElementById('overlayStatus').textContent = 'Koneksi gagal';
    document.getElementById('overlayReloadBtn').style.display = 'block';
}
```

**Jangan** menutup overlay saat error — biarkan tetap tampil sehingga user tidak bisa berinteraksi dengan soal dalam kondisi hub tidak connected.

### Pattern: Resume Exam (D-07)

`assessmentHubStartPromise` adalah promise yang di-set saat `assessment-hub.js` pertama load (IIFE). Saat user resume exam (buka tab yang sama lagi), `assessment-hub.js` di-reload sebagai bagian dari page load baru — promise fresh setiap kali. Tidak ada perlakuan khusus untuk resume vs. start pertama kali karena keduanya sama-sama merupakan page load baru.

**Ini konfirmasi bahwa satu implementasi menangani kedua skenario (D-07).**

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan |
|---------|-------------|---------|
| Spinner animasi | Custom CSS animation | Bootstrap `spinner-border` |
| Minimum delay + promise | Custom timing logic kompleks | `Promise.all([hubPromise, new Promise(r => setTimeout(r, 1000))])` |
| Block keyboard/focus | Manual tabindex loop | `inert` attribute |

## Common Pitfalls

### Pitfall 1: Z-index Conflict dengan Sticky Header
**What goes wrong:** Overlay di-render di belakang sticky header karena z-index kurang.
**Root cause:** Sticky header (`examHeader`) punya z-index 1020 (dari existing CSS di line 944).
**How to avoid:** Set overlay z-index > 1020. Gunakan 2000 agar aman dari Bootstrap modal defaults (1050) jika ada modal.

### Pitfall 2: `assessmentHubStartPromise` Sudah Settle Sebelum Handler Dipasang
**What goes wrong:** Jika hub konek sangat cepat (lokal), promise sudah resolve sebelum `then()` dipasang — tapi ini tidak masalah karena Promise resolution bersifat async dan `.then()` tetap dipanggil meskipun promise sudah settled.
**Root cause:** Misunderstanding tentang promise semantics.
**How to avoid:** Tidak perlu workaround — JavaScript Promise menjamin `.then()` callback dipanggil meskipun promise sudah settled sebelum handler dipasang.

### Pitfall 3: `assessmentHubStartPromise` undefined pada Skenario Tertentu
**What goes wrong:** `window.assessmentHubStartPromise` belum tersedia saat JavaScript di StartExam dieksekusi.
**Root cause:** Script loading order — jika `assessment-hub.js` dimuat setelah inline script di StartExam.
**How to avoid:** Pastikan `assessment-hub.js` dimuat **sebelum** inline script overlay di StartExam. Periksa urutan `<script>` di layout dan halaman. Berdasarkan kode yang ada, baris 930 di StartExam sudah pakai `if (window.assessmentHubStartPromise)` sebagai guard — pola yang sama harus digunakan.

### Pitfall 4: Overlay Tidak Hilang Saat Hub Konek Tapi State Bukan "Connected"
**What goes wrong:** `assessmentHubStartPromise` resolve, tapi `assessmentHub.state !== 'Connected'` — overlay tetap tampil.
**Root cause:** Assessment hub bisa resolve tapi langsung disconnect (race condition).
**How to avoid:** Periksa `.catch()` — jika hub disconnect setelah connect, `onclose` handler di assessment-hub.js akan handle (persistent toast). Overlay cukup menunggu promise resolve/reject saja.

### Pitfall 5: Overlay Tampil Saat Modal Bootstrap Ada
**What goes wrong:** Z-index overlay lebih rendah dari Bootstrap modal backdrop (z-index: 1040).
**How to avoid:** Set z-index overlay ke 2000 — di atas semua Bootstrap stacking contexts.

## Code Examples

### Integrasi Lengkap (JavaScript di StartExam.cshtml)

```javascript
// === Loading Overlay Logic ===
var examOverlay = document.getElementById('examLoadingOverlay');
var overlayStatusEl = document.getElementById('overlayStatus');
var examContainer = document.getElementById('examContainer'); // wrapper container

function overlayFadeOut() {
    if (overlayStatusEl) overlayStatusEl.textContent = 'Terhubung!';
    setTimeout(function() {
        if (examOverlay) {
            examOverlay.classList.add('fade-out');
            // Re-enable interaksi
            if (examContainer) examContainer.inert = false;
            setTimeout(function() {
                examOverlay.style.display = 'none';
            }, 300);
        }
    }, 400); // tampilkan "Terhubung!" sebentar sebelum fade
}

function overlayShowError() {
    var spinner = examOverlay.querySelector('.spinner-border');
    if (spinner) spinner.style.display = 'none';
    if (overlayStatusEl) overlayStatusEl.textContent = 'Koneksi gagal. Periksa jaringan Anda.';
    var reloadBtn = document.getElementById('overlayReloadBtn');
    if (reloadBtn) reloadBtn.style.display = 'inline-block';
}

// Block interaksi saat overlay tampil
if (examContainer) examContainer.inert = true;

// Tunggu hub ready + minimal 1 detik
var minDelay = new Promise(function(resolve) { setTimeout(resolve, 1000); });
if (window.assessmentHubStartPromise) {
    Promise.all([window.assessmentHubStartPromise, minDelay])
        .then(function() {
            overlayFadeOut();
        })
        .catch(function() {
            overlayShowError();
        });
} else {
    // Fallback: tidak ada promise, langsung hilangkan overlay setelah delay
    minDelay.then(overlayFadeOut);
}
```

### HTML Overlay

```html
<div id="examLoadingOverlay" role="status" aria-label="Mempersiapkan ujian">
    <div class="text-center">
        <div class="spinner-border text-light mb-3" style="width: 3rem; height: 3rem;"></div>
        <div class="fs-5 fw-semibold mb-2">Mempersiapkan ujian...</div>
        <div class="small opacity-75" id="overlayStatus">Menghubungkan ke server...</div>
        <button id="overlayReloadBtn" class="btn btn-light btn-sm mt-3"
                style="display:none" onclick="window.location.reload()">
            Muat Ulang
        </button>
    </div>
</div>
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (browser) — tidak ada automated test framework untuk Razor/SignalR UI |
| Config file | tidak ada |
| Quick run command | Buka StartExam di browser, verifikasi visual |
| Full suite command | Sama |

### Phase Requirements → Test Map
| ID | Behavior | Test Type | Command |
|----|----------|-----------|---------|
| D-01 | Overlay tampil full-screen saat halaman load | manual | Buka StartExam, lihat sebelum hub connect |
| D-02 | Teks "Mempersiapkan ujian..." + status "Menghubungkan ke server..." | manual | Observasi visual |
| D-03 | Klik/tab tidak tembus overlay | manual | Coba klik soal, tekan Tab saat overlay tampil |
| D-04 | Overlay tampil minimal 1 detik meskipun hub cepat | manual | Koneksi lokal cepat — overlay harus tetap 1 detik |
| D-05 | Timer berjalan saat overlay tampil | manual | Perhatikan countdown timer di header |
| D-06 | Error state saat hub gagal | manual | Blokir network, lihat overlay berubah ke error |
| D-07 | Overlay muncul saat resume exam | manual | Close tab, buka kembali URL StartExam |

### Wave 0 Gaps
- Tidak ada — tidak memerlukan test file baru. Semua verifikasi manual via browser.

## Environment Availability

Step 2.6: SKIPPED (tidak ada external dependencies baru — pure frontend CSS/JS di dalam file yang sudah ada)

## Constraint Penting: `assessmentHubStartPromise` Reject Behavior

**Temuan kritis dari membaca kode:**

Di `assessment-hub.js`, fungsi `startHub()` (line 81-90):
```javascript
async function startHub() {
    try {
        await connection.start();
        // ...
    } catch (err) {
        // onclose handles retries exhausted — swallow start errors silently
    }
}
```

**Masalah:** `startHub()` catch semua error dan tidak re-throw — artinya `assessmentHubStartPromise` SELALU RESOLVE (tidak pernah reject), bahkan saat hub gagal konek.

**Implikasi untuk D-06:** `Promise.all([...]).catch()` tidak akan pernah terpanggil via promise rejection. Sebaliknya, error state harus di-trigger via event — saat `connection.onclose` dipanggil.

**Solusi:**

```javascript
// Karena assessmentHubStartPromise tidak pernah reject,
// gunakan flag + event untuk detect error

var hubConnected = false;

window.assessmentHubStartPromise.then(function() {
    // Cek apakah hub benar-benar connected
    if (window.assessmentHub && window.assessmentHub.state === 'Connected') {
        hubConnected = true;
    }
    // assessmentHub.onclose akan trigger error state jika gagal
});

// Listen onclose sebagai error signal
window.assessmentHub.onclose(function() {
    if (!hubConnected) {
        // Hub gagal connect — tampilkan error state di overlay
        overlayShowError();
    }
    // (existing badge update kode di bawahnya tetap berjalan)
});
```

Atau alternatif lebih bersih: **Modifikasi `assessment-hub.js`** agar expose status flag atau expose separate reject promise. Namun karena CONTEXT.md menyebutkan "konsisten dengan existing `onclose` handler", pilihan terbaik adalah: listen `onclose` di overlay logic untuk trigger error state.

## Sources

### Primary (HIGH confidence)
- Kode langsung: `wwwroot/js/assessment-hub.js` — perilaku actual `assessmentHubStartPromise`
- Kode langsung: `Views/CMP/StartExam.cshtml` line 930 — existing usage pattern
- Kode langsung: `wwwroot/css/assessment-hub.css` — existing CSS patterns
- MDN Web Docs: HTML `inert` attribute — browser support (Chrome 102+, Firefox 112+, Safari 15.5+)

### Secondary (MEDIUM confidence)
- Bootstrap 5 docs: `spinner-border` class — verified tersedia karena sudah digunakan di project

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada package baru, semua dari kode existing
- Architecture: HIGH — pola Promise.all + CSS overlay adalah teknik standar
- Pitfall kritis (promise tidak reject): HIGH — ditemukan langsung dari membaca source code
- Browser support `inert`: MEDIUM — modern browsers only, tapi sesuai target environment

**Research date:** 2026-03-28
**Valid until:** 2026-04-28 (stable web APIs, tidak ada dependensi fast-moving)
