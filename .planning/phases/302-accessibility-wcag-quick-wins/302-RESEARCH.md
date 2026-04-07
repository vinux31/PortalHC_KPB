# Phase 302: Accessibility WCAG Quick Wins - Research

**Researched:** 2026-04-07
**Domain:** Web Accessibility (WCAG 2.1), ASP.NET Core MVC, SignalR, EF Core Migration
**Confidence:** HIGH (semua temuan diverifikasi langsung dari codebase)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

#### Skip Link
- **D-01:** Skip link "Lewati ke konten utama" ditambahkan hanya di StartExam.cshtml (bukan _Layout global)
- **D-02:** Hidden by default, muncul saat user menekan Tab pertama kali (visually-hidden + :focus-visible pattern)
- **D-03:** Target skip link: area soal (main content container), melewati header/timer/sidebar

#### Keyboard Navigation
- **D-04:** Navigasi antar opsi jawaban dalam satu soal menggunakan Arrow Up/Down keys (native radio group behavior untuk MC, native checkbox behavior untuk MA)
- **D-05:** Navigasi antar soal menggunakan Tab — setelah selesai opsi soal 1, Tab terus ke soal 2
- **D-06:** Sticky footer mobile (Prev, offcanvas trigger, Next) masuk tab order natural — bisa diakses via Tab
- **D-07:** Essay textarea: Tab masuk ke textarea, Tab keluar ke elemen berikutnya (native behavior)

#### Extra Time
- **D-08:** Extra time berlaku per assessment (semua peserta), bukan per individu/sesi
- **D-09:** Peserta yang sudah submit tidak terpengaruh oleh penambahan extra time
- **D-10:** UI: tombol "Extra Time" di halaman AssessmentMonitoring, klik buka modal dengan input waktu
- **D-11:** Range: 5-120 menit, kelipatan 5 (dropdown atau number input step=5)
- **D-12:** Field baru ExtraTimeMinutes di model/tabel Assessment (bukan AssessmentSession)
- **D-13:** Timer peserta diupdate real-time via SignalR saat HC menambah extra time (peserta tidak perlu refresh)

#### Auto-focus
- **D-14:** Saat pindah halaman soal (Prev/Next), focus otomatis berpindah ke card soal pertama di halaman baru
- **D-15:** Implementasi di `performPageSwitch()` — tambah `.focus()` ke elemen soal pertama setelah page switch

#### Scope
- **D-16:** Semua fitur accessibility hanya diterapkan di halaman StartExam
- **D-17:** Anti-copy Phase 280 tidak konflik — hanya block Ctrl+C/A/U/S/P, tidak block Tab/Arrow/Enter/Space

#### Testing
- **D-20:** Validasi manual saja — Tab through halaman, test keyboard nav, test extra time flow. Tanpa automated tooling (axe-core)

### Dropped dari Scope (JANGAN diimplementasikan)
- **D-18:** Screen reader / aria-live timer announcement (A11Y-03) — DIHAPUS
- **D-19:** Font size control A+/A- (A11Y-04) — DIHAPUS

### Claude's Discretion
- CSS styling untuk skip link (visually-hidden class)
- Exact focus outline styling
- Modal layout untuk extra time input
- SignalR message format untuk extra time update
- Apakah ExtraTimeMinutes pakai dropdown atau number input

### Deferred Ideas (OUT OF SCOPE)
- Screen reader support (aria-live timer, ARIA labels per soal)
- Font size control A+/A-
- Skip link global di _Layout.cshtml
- Automated accessibility testing (axe-core)

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Deskripsi | Status | Research Support |
|----|-----------|--------|-----------------|
| A11Y-01 | Skip link "Lewati ke konten utama" | IN SCOPE | Bootstrap visually-hidden tersedia; target = #examContainer atau .col-lg-9 |
| A11Y-02 | Keyboard navigation untuk semua soal dan opsi jawaban | IN SCOPE | Radio/checkbox native sudah keyboard-accessible; Tab order perlu audit di page saat ini |
| A11Y-03 | Screen reader announcement (aria-live) sisa waktu < 5 menit | DROPPED (D-18) | — |
| A11Y-04 | Kontrol ukuran font (A+/A-) | DROPPED (D-19) | — |
| A11Y-05 | ExtraTimeMinutes per assessment untuk peserta kebutuhan khusus | IN SCOPE | Model AssessmentSession hanya punya DurationMinutes; perlu field baru di Assessment + DB migration + SignalR broadcast |
| A11Y-06 | Auto-focus ke soal pertama saat berpindah halaman | IN SCOPE | `performPageSwitch()` sudah ada; tinggal tambah `.focus()` setelah display block |

</phase_requirements>

---

## Summary

Phase 302 adalah implementasi aksesibilitas minimal pada halaman ujian StartExam. Empat fitur aktif: (1) skip link, (2) keyboard navigation audit/fix, (3) extra time per assessment dengan SignalR real-time update, (4) auto-focus saat page switch.

Codebase sudah memiliki fondasi yang kuat: Bootstrap 5 (visually-hidden, modal, offcanvas), SignalR AssessmentHub dengan group messaging, `performPageSwitch()` sebagai titik injeksi focus management. Fitur utama yang butuh perubahan signifikan adalah **Extra Time** karena melibatkan DB migration (kolom baru di AssessmentSession), endpoint controller baru, SignalR broadcast method baru, dan update timer logic di frontend.

Skip link dan auto-focus adalah perubahan minor (HTML + JS satu-dua baris). Keyboard navigation untuk MC/checkbox sudah native-accessible, tapi perlu verifikasi tidak ada elemen yang sengaja mengambil alih Arrow key events (tidak ditemukan di audit kode).

**Primary recommendation:** Implementasi dalam urutan: auto-focus (paling mudah) → skip link → keyboard nav audit → extra time (paling kompleks).

---

## Standard Stack

### Core (sudah ada di proyek)
| Komponen | Versi | Peran di Phase 302 |
|----------|-------|--------------------|
| ASP.NET Core MVC | .NET 8 | Controller endpoint AddExtraTime |
| Bootstrap 5 | 5.x via CDN | visually-hidden class, modal extra time, focus styling |
| SignalR (ASP.NET Core) | Built-in | Broadcast ExtraTimeAdded ke peserta aktif |
| Entity Framework Core | Built-in | DB migration tambah ExtraTimeMinutes |
| Vanilla JS | — | focus() call di performPageSwitch(), skip link :focus-visible |

### Tidak ada library baru yang diperlukan
Semua kebutuhan phase ini dapat dipenuhi dengan stack yang sudah ada. [VERIFIED: codebase audit]

---

## Architecture Patterns

### Pattern 1: Skip Link (visually-hidden + :focus-visible)
**Apa:** Elemen `<a>` tersembunyi yang muncul saat menerima keyboard focus.
**Kapan:** Di awal DOM, sebelum sticky header.
**Contoh HTML:**
```html
<!-- Source: WCAG 2.1 technique G1 [ASSUMED] -->
<a href="#mainContent" class="skip-link visually-hidden-focusable">
    Lewati ke konten utama
</a>
```
Bootstrap 5 sudah menyediakan class `visually-hidden-focusable` yang melakukan persis ini (hidden kecuali saat :focus). [VERIFIED: codebase menggunakan Bootstrap 5 via CDN]

**Target:** `<div class="col-lg-9 exam-protected">` atau wrapper di dalamnya dengan `id="mainContent"`.

### Pattern 2: Auto-focus di performPageSwitch()
**Apa:** Setelah page switch, set focus ke elemen soal pertama di halaman baru.
**Titik injeksi:** `performPageSwitch()` di StartExam.cshtml, tepat setelah baris `document.getElementById('page_' + currentPage).style.display = 'block';`

```javascript
// Tambahkan setelah display block — [VERIFIED: line 951 StartExam.cshtml]
function performPageSwitch(newPage, skipScroll) {
    document.getElementById('page_' + currentPage).style.display = 'none';
    currentPage = newPage;
    document.getElementById('page_' + currentPage).style.display = 'block';
    updatePanel();
    updateMobileNavButtons();
    scrollPanelToCurrentPage();
    saveSessionProgress();
    if (!skipScroll) {
        window.scrollTo(0, 0);
    }
    // TAMBAHKAN INI (D-15):
    var firstQuestion = document.querySelector('#page_' + currentPage + ' .card');
    if (firstQuestion) {
        firstQuestion.setAttribute('tabindex', '-1');
        firstQuestion.focus({ preventScroll: true });
    }
    // ... SignalR LogPageNav tetap seperti existing ...
}
```

### Pattern 3: Extra Time — DB + Controller + SignalR
**Alur lengkap:**
1. HC buka modal di AssessmentMonitoringDetail → pilih menit → submit
2. Controller `AddExtraTime(int sessionId, int minutes)` di AssessmentAdminController:
   - Validasi range 5-120, kelipatan 5
   - Update `assessment.ExtraTimeMinutes += minutes` (atau set jika null)
   - SaveChanges
   - Broadcast SignalR ke group `batch-{batchKey}`: `ExtraTimeAdded(additionalSeconds)`
3. Frontend StartExam menerima event SignalR → update `timerStartRemaining += additionalSeconds` dan reset `timerStartWallClock = Date.now()` dan `timerStartRemaining = timeRemaining + additionalSeconds`

**Timer logic existing (line 443-480 StartExam.cshtml):**
```javascript
// Wall-clock anchor pattern yang sudah ada [VERIFIED]
var timerStartWallClock = Date.now();
var timerStartRemaining = REMAINING_SECONDS_FROM_DB;

function updateTimer() {
    var wallElapsed = Math.floor((Date.now() - timerStartWallClock) / 1000);
    var remaining = Math.max(0, timerStartRemaining - wallElapsed);
    // ...
}
```
Untuk extra time: cukup tambahkan detik ke `timerStartRemaining` tanpa reset `timerStartWallClock`:
```javascript
// Di event handler SignalR ExtraTimeAdded:
connection.on('ExtraTimeAdded', function(additionalSeconds) {
    timerStartRemaining = timeRemaining + additionalSeconds;
    timerStartWallClock = Date.now();
});
```

### Pattern 4: SignalR Broadcast dari Controller
AssessmentHub saat ini dipanggil dari Hub methods (client-initiated). Untuk extra time, HC klik button di browser → controller HTTP endpoint → broadcast ke peserta. Pattern ini membutuhkan `IHubContext<AssessmentHub>` di controller.

```csharp
// Di AssessmentAdminController [ASSUMED — pattern standar SignalR]
private readonly IHubContext<AssessmentHub> _hubContext;

public async Task<IActionResult> AddExtraTime(int assessmentId, int minutes)
{
    // ... validasi, update DB ...
    var batchKey = assessment.AccessToken; // atau Id sebagai string
    await _hubContext.Clients.Group($"batch-{batchKey}")
        .SendAsync("ExtraTimeAdded", minutes * 60);
    return Json(new { success = true });
}
```

### Pattern 5: Model Change — ExtraTimeMinutes
Field baru di `AssessmentSession` (per keputusan D-12 yang menyebut "Assessment", bukan session — perlu klarifikasi: AssessmentSession adalah tabel assessment per-peserta, jadi field di sini berlaku per batch kalau semua peserta punya session dari AccessToken yang sama):

```csharp
// Di AssessmentSession.cs — tambahkan field baru
/// <summary>
/// Tambahan waktu (menit) yang diberikan HC untuk semua peserta assessment ini.
/// Null = tidak ada extra time. Range: 5-120 menit, kelipatan 5.
/// </summary>
public int? ExtraTimeMinutes { get; set; }
```

**Catatan penting D-12 vs arsitektur existing:** CONTEXT.md D-12 menyebut "field baru ExtraTimeMinutes di model/tabel Assessment". Namun tabel utama adalah `AssessmentSessions` (bukan tabel "Assessment" terpisah). Solusi paling tepat: tambah di `AssessmentSession` dan update semua session dalam batch yang sama (same AccessToken) sekaligus, ATAU buat field di level yang mewakili "batch". Melihat kode existing, `AccessToken` adalah identifier batch — semua peserta dalam ujian yang sama share AccessToken yang sama. Implementasi termudah: update `ExtraTimeMinutes` di SEMUA `AssessmentSession` yang share AccessToken yang sama dan masih `InProgress`.

### Anti-Patterns
- **Jangan tambah tabindex="1" atau angka positif** — merusak natural tab order
- **Jangan block keydown untuk Tab/Arrow di anti-copy handler** — existing code sudah benar (hanya block Ctrl+kombinasi)
- **Jangan gunakan `element.focus()` tanpa `tabindex="-1"`** pada elemen non-interactive (div, card) — browser akan ignore focus pada elemen yang tidak focusable secara default

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan Saja |
|---------|-------------|--------------|
| Skip link styling | CSS custom visually-hidden | Bootstrap `visually-hidden-focusable` |
| Modal extra time | Custom dropdown/popup | Bootstrap modal yang sudah dipakai di halaman ini |
| Focus management antar page | Custom event system | Native `.focus()` dengan `tabindex="-1"` |
| SignalR broadcast dari server | WebSocket custom | `IHubContext<AssessmentHub>` DI pattern |

---

## Common Pitfalls

### Pitfall 1: focus() pada elemen non-interactive
**Yang salah:** `document.querySelector('.card').focus()` — tidak bekerja tanpa `tabindex`
**Yang benar:** Set `tabindex="-1"` pada card soal secara programatik, LALU panggil `.focus()`
**Catatan:** tabindex="-1" berarti elemen bisa di-focus via JS tapi tidak masuk natural Tab order — ini pattern yang benar untuk skip target dan page-switch focus

### Pitfall 2: Timer drift saat extra time ditambah
**Yang salah:** Hanya update `timerStartRemaining` tanpa reset anchor `timerStartWallClock`
**Yang benar:** Saat terima `ExtraTimeAdded`, update KEDUANYA:
```javascript
timerStartRemaining = timeRemaining + additionalSeconds;
timerStartWallClock = Date.now();
```
Ini memastikan timer wall-clock anchor tetap benar.

### Pitfall 3: Extra time mempengaruhi server-side timer check
**Yang salah:** Hanya update timer di frontend, sehingga server masih reject jawaban setelah durasi original
**Yang benar:** Saat AddExtraTime disimpan ke DB (`ExtraTimeMinutes`), semua server-side timer check di CMPController dan AssessmentHub harus memperhitungkan nilai ini:
```csharp
// SaveMultipleAnswer di AssessmentHub (line 208-213):
var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60;
```
Dan di CMPController `SubmitExam`, `AkhiriUjian`, dan `AkhiriSemuaUjian`.

### Pitfall 4: Skip link — href target tidak ada id
**Yang salah:** `<a href="#mainContent">` tapi tidak ada elemen dengan `id="mainContent"`
**Yang benar:** Pastikan target elemen punya ID yang matching. Di StartExam.cshtml, elemen target kandidat adalah `<div class="col-lg-9 exam-protected">` — perlu tambah `id="mainContent"`.

### Pitfall 5: Focus outline dihilangkan CSS global
**Warning:** Banyak proyek memiliki `outline: none` atau `outline: 0` di CSS global. Verifikasi tidak ada di project ini sebelum menganggap keyboard focus sudah visible.

---

## Temuan Kode Existing (Verified)

### StartExam.cshtml — struktur DOM awal [VERIFIED: line 10-31]
```html
<!-- Sticky Header — INI yang harus di-skip -->
<div class="sticky-top bg-white shadow-sm py-2 px-3 border-bottom" id="examHeader">
    ...timer, progress, tombol keluar...
</div>

<!-- examContainer — ini target skip link -->
<div class="container-fluid py-3" id="examContainer" inert>
    <div class="row">
        <div class="col-lg-9 exam-protected">  <!-- tambah id="mainContent" di sini -->
```
Skip link harus ditempatkan sebelum `#examHeader`, sebagai elemen PERTAMA di body/page.

### AssessmentHub.cs — existing methods [VERIFIED: Hubs/AssessmentHub.cs]
- `JoinBatch(string batchKey)` — peserta join group `batch-{batchKey}`
- `JoinMonitor(string batchKey)` — HC join group `monitor-{batchKey}`
- `SaveTextAnswer`, `SaveMultipleAnswer` — cek timer dengan `session.DurationMinutes * 60` (perlu update ke `DurationMinutes + ExtraTimeMinutes`)
- `LogPageNav` — fire-and-forget log

Method baru yang perlu ditambah di Hub TIDAK diperlukan — broadcast ExtraTimeAdded dilakukan dari controller via `IHubContext`.

### Timer calculation di CMPController [VERIFIED: line 1030-1049]
```csharp
int durationSeconds = assessment.DurationMinutes * 60;
// ... wallClockElapsed check ...
int remainingSeconds = durationSeconds - elapsedSec;
ViewBag.RemainingSeconds = remainingSeconds;
```
Kalau ExtraTimeMinutes sudah ada di DB saat peserta load halaman, `durationSeconds` harus sudah include extra time. Tapi untuk extra time yang ditambah SAAT ujian berlangsung (real-time), `RemainingSeconds` di ViewBag tidak relevan — yang relevan adalah update JS timer via SignalR.

### AssessmentMonitoringDetail.cshtml — modal pattern [VERIFIED: line 89-119]
Halaman ini sudah menggunakan Bootstrap modal untuk token display dan regenerate. Pattern modal AJAX sudah established. Tombol Extra Time bisa ditambah di sebelah tombol Regenerate Token atau di section terpisah.

---

## Database Migration

### Field baru di AssessmentSession
```csharp
// AssessmentSession.cs — tambahkan:
public int? ExtraTimeMinutes { get; set; }
```

### EF Core Migration
```bash
dotnet ef migrations add AddExtraTimeMinutesToAssessmentSession
dotnet ef database update
```

Migration akan menghasilkan kolom nullable `ExtraTimeMinutes int NULL` di tabel `AssessmentSessions`. Nilai default NULL (tidak ada extra time) kompatibel dengan semua data existing. [ASSUMED — berdasarkan pola migration EF Core standard]

---

## Validation Architecture

Sesuai keputusan D-20: **validasi manual saja**, tanpa automated tooling.

### Manual Test Checklist

| Req | Test | Cara |
|-----|------|------|
| A11Y-01 | Skip link muncul saat Tab pertama | Buka StartExam, tekan Tab, lihat "Lewati ke konten utama" muncul |
| A11Y-01 | Skip link berfungsi | Tekan Enter saat skip link fokus, verifikasi scroll/focus ke area soal |
| A11Y-02 | Keyboard nav MC | Tab ke soal, Arrow Up/Down antar opsi, Enter/Space untuk pilih |
| A11Y-02 | Tab order antara soal | Setelah opsi terakhir soal 1, Tab lanjut ke soal 2 |
| A11Y-02 | Mobile sticky footer | Tab sampai ke tombol Prev/Next di bawah |
| A11Y-05 | Extra time flow | Login sebagai HC → AssessmentMonitoringDetail → Extra Time modal → submit → cek timer peserta bertambah real-time |
| A11Y-05 | Peserta submit setelah extra time | Submit exam setelah extra time ditambahkan, verifikasi server tidak reject |
| A11Y-06 | Auto-focus page switch | Klik Next, verifikasi focus visual berpindah ke card soal pertama halaman baru |

---

## Assumptions Log

| # | Klaim | Section | Risiko jika Salah |
|---|-------|---------|-------------------|
| A1 | `IHubContext<AssessmentHub>` DI injection pattern untuk broadcast dari controller | Architecture Patterns P3 | Perlu periksa apakah AssessmentHub sudah terdaftar di Program.cs dengan benar |
| A2 | Bootstrap `visually-hidden-focusable` tersedia dari CDN yang sudah dipakai | Standard Stack | Jika versi Bootstrap < 5.1, class ini tidak ada; gunakan CSS manual |
| A3 | EF Core migration untuk nullable int field aman tanpa data migration | Database Migration | Selalu safe untuk nullable column baru di EF Core |
| A4 | Semua server-side timer check ada di CMPController dan AssessmentHub saja | Pitfall 3 | Jika ada timer check di tempat lain, extra time tidak akan diperhitungkan |

---

## Open Questions

1. **Apa `batchKey` yang digunakan untuk group SignalR?**
   - Yang diketahui: `JoinBatch(string batchKey)` sudah ada; kode JS di StartExam memanggil ini dengan nilai tertentu
   - Yang belum jelas: nilai exact `batchKey` yang dikirim dari JS (AccessToken? AssessmentId?)
   - Rekomendasi: Cari di `assessment-hub.js` atau inisialisasi hub di StartExam.cshtml

2. **Apakah ExtraTimeMinutes perlu di-persist per session atau cukup di-broadcast real-time?**
   - Per D-12: field baru di model — berarti persist ke DB
   - Ini penting agar peserta yang disconnect-reconnect mendapat durasi yang benar

---

## Environment Availability

Step 2.6: SKIPPED — phase ini adalah pure code/config changes tanpa external tool dependencies baru. SignalR dan EF Core sudah available sebagai bagian dari project existing.

---

## Sources

### Primary (HIGH confidence — verified dari codebase)
- `Views/CMP/StartExam.cshtml` — struktur DOM, timer logic, performPageSwitch(), anti-copy handler
- `Hubs/AssessmentHub.cs` — existing SignalR methods, group naming pattern
- `Models/AssessmentSession.cs` — existing fields, tidak ada ExtraTimeMinutes
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — modal pattern existing
- `Controllers/CMPController.cs` — timer calculation, RemainingSeconds ViewBag

### Secondary (MEDIUM confidence — pattern standard)
- Bootstrap 5 docs: `visually-hidden-focusable` class [ASSUMED dari training knowledge]
- ASP.NET Core SignalR `IHubContext` pattern [ASSUMED dari training knowledge]

---

## Metadata

**Confidence breakdown:**
- Skip link + auto-focus: HIGH — perubahan minimal, pattern jelas dari codebase
- Keyboard navigation: HIGH — native browser behavior, anti-copy tidak konflik (verified)
- Extra time (DB + controller): HIGH — pattern established (AJAX modal, EF migration)
- Extra time (SignalR real-time): MEDIUM — IHubContext pattern standar tapi batchKey exact belum diverifikasi

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stack stabil, tidak ada dependency bergerak cepat)
