# Phase 271: Fix Timer Ujian — Research

**Researched:** 2026-03-28
**Domain:** ASP.NET Core MVC — server-side timer validation, client-side wall-clock countdown
**Confidence:** HIGH (semua info dari source code langsung, tidak butuh library eksternal)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Kedua bug punya satu root cause — `ElapsedSeconds` yang dikirim client ke server tidak akurat saat navigate away atau refresh
- **D-02:** `sendBeacon` pada `beforeunload` tidak selalu reliable (terutama saat navigate away ke halaman lain)
- **D-03:** Periodic save setiap 30 detik berarti ada window sampai 30 detik di mana `ElapsedSeconds` di DB bisa outdated
- **D-04:** Bug terjadi di worker page saat resume — user mulai ujian, navigate away, masuk lagi → waktu habis atau timer bertambah
- **D-05:** Juga mungkin terjadi saat browser refresh
- **D-06:** Server harus punya source of truth sendiri — gunakan `StartedAt` + `DateTime.UtcNow` sebagai cross-check
- **D-07:** Saat resume, server hitung remaining: `remaining = (DurationMinutes × 60) - max(ElapsedSeconds_dari_DB, elapsed_dari_StartedAt)`
- **D-08:** Client tetap kirim `elapsedSeconds` untuk periodic save, tapi server harus validate/clamp nilainya agar tidak melebihi wall-clock elapsed
- **D-09:** Scope terbatas ke fix 2 bug ini saja — tidak ada refactor besar atau fitur timer baru
- **D-10:** Tidak ada bug timer lain yang ditemukan saat UAT selain 2 ini

### Claude's Discretion
- Exact validation/clamp logic di server
- Apakah perlu adjust periodic save interval (30 detik → lebih pendek)
- Detail implementasi cross-check `StartedAt` vs `ElapsedSeconds`

### Deferred Ideas (OUT OF SCOPE)
- Tidak ada — discussion stayed within phase scope
</user_constraints>

---

## Summary

Phase ini memperbaiki dua bug yang berakar pada satu masalah: `ElapsedSeconds` di database bisa stale atau tidak akurat saat worker navigate away dari halaman exam. Saat resume, server menghitung remaining time murni dari `ElapsedSeconds` di DB tanpa cross-check dengan wall-clock `StartedAt`, sehingga bisa menghasilkan nilai yang keliru.

**Bug 1 (waktu habis tiba-tiba):** Terjadi ketika worker navigate away tepat sebelum periodic save 30 detik berikutnya, lalu resume setelah jeda lama. `ElapsedSeconds` di DB masih nilai lama (stale), tapi waktu nyata yang telah berlalu (wall-clock sejak `StartedAt`) sudah melebihi durasi. Server tidak cross-check — langsung set `remainingSeconds = durationSeconds - elapsedSec`, bisa negatif.

**Bug 2 (timer bertambah saat resume):** `ELAPSED_SECONDS_FROM_DB` diinject ke JS, lalu `elapsedSeconds = ELAPSED_SECONDS_FROM_DB + wallElapsed`. Saat resume, `ELAPSED_SECONDS_FROM_DB` bisa lebih kecil dari waktu nyata yang berlalu (karena stale), sehingga `elapsedSeconds` yang dikirim periodic save justru lebih kecil dari waktu sebenarnya, dan `timeRemaining` yang ter-display bisa lebih besar dari seharusnya (naik dari sebelumnya).

**Primary recommendation:** Perbaiki kalkulasi `remainingSeconds` di server (`StartExam` action) menggunakan `max(ElapsedSeconds_DB, wall_clock_elapsed)`, dan tambahkan validasi clamp di `UpdateSessionProgress` agar `elapsedSeconds` dari client tidak bisa lebih kecil dari nilai sebelumnya maupun melebihi wall-clock elapsed.

---

## Architecture Patterns

### Alur Data Timer (Existing)

```
StartedAt (UTC) disimpan DB saat pertama kali StartExam
       ↓
StartExam GET → hitung remainingSeconds = DurationMinutes*60 - ElapsedSeconds
       ↓
View inject: REMAINING_SECONDS_FROM_DB, ELAPSED_SECONDS_FROM_DB
       ↓
JS: timerStartWallClock = Date.now()
    timerStartRemaining = REMAINING_SECONDS_FROM_DB
    updateTimer() setiap 1 detik:
      wallElapsed = (Date.now() - timerStartWallClock) / 1000
      remaining = timerStartRemaining - wallElapsed     ← correct, berkurang terus
      elapsedSeconds = ELAPSED_SECONDS_FROM_DB + wallElapsed  ← BUG jika ELAPSED stale
       ↓
Periodic save setiap 30 detik → UpdateSessionProgress(elapsedSeconds)
sendBeacon saat beforeunload → UpdateSessionProgress(elapsedSeconds)
       ↓
Resume: StartExam GET → remainingSeconds = DurationMinutes*60 - ElapsedSeconds_DB  ← BUG jika stale
```

### Root Cause Detail

**Skenario Bug 1 (waktu habis mendadak):**
1. Worker mulai jam 10:00:00 (StartedAt = UTC)
2. Worker navigate away jam 10:05:00. Periodic save terakhir jam 10:04:30 → DB: ElapsedSeconds = 270
3. Worker resume jam 10:35:00 (30 menit kemudian)
4. Server: `remaining = 3600 - 270 = 3330` detik (55 menit!) ← **SALAH**
5. Tapi sebenarnya wall-clock elapsed = 35 menit = 2100 detik → remaining seharusnya 3600 - 2100 = 1500 detik

**Skenario Bug 2 (timer bertambah):**
1. Worker mulai jam 10:00:00
2. Sebelum navigate away, timer client sudah menghitung 5 menit (300 detik elapsed)
3. Navigate away terjadi, sendBeacon **gagal** (navigate away bukan close tab)
4. DB masih ElapsedSeconds = 270 (dari periodic save terakhir 30 detik lalu)
5. Resume: server kirim `ELAPSED_SECONDS_FROM_DB = 270`, `REMAINING_SECONDS_FROM_DB = 3330`
6. JS: `timerStartRemaining = 3330` → display "55:30" padahal seharusnya lebih kecil
7. Efek "timer bertambah" karena sebelumnya sudah countdown lebih jauh

### Fix yang Diperlukan

#### Fix 1: Server-side kalkulasi remainingSeconds (StartExam action, baris 908-917)

**Sekarang (BUGGY):**
```csharp
int elapsedSec = assessment.ElapsedSeconds;
int remainingSeconds = durationSeconds - elapsedSec;
ViewBag.ExamExpired = isResume && remainingSeconds <= 0;
```

**Seharusnya (FIXED):**
```csharp
int elapsedSec = assessment.ElapsedSeconds;

// Cross-check dengan wall-clock: ambil max antara DB elapsed vs waktu nyata
if (assessment.StartedAt.HasValue)
{
    int wallClockElapsed = (int)(DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
    elapsedSec = Math.Max(elapsedSec, wallClockElapsed);
}

// Clamp agar tidak melebihi durasi total (defensive)
elapsedSec = Math.Min(elapsedSec, durationSeconds);

int remainingSeconds = durationSeconds - elapsedSec;
ViewBag.ExamExpired = isResume && remainingSeconds <= 0;
```

**Efek:** `remainingSeconds` yang dikirim ke view selalu akurat berdasarkan waktu nyata, bukan DB stale.

#### Fix 2: Server-side validasi/clamp di UpdateSessionProgress (baris 352-358)

**Sekarang (BUGGY):**
```csharp
// Langsung update tanpa validasi
.SetProperty(r => r.ElapsedSeconds, elapsedSeconds)
```

**Seharusnya (FIXED):**
```csharp
// Clamp: elapsedSeconds dari client tidak boleh melebihi wall-clock elapsed
// dan tidak boleh lebih kecil dari nilai sebelumnya (no backward movement)
int clampedElapsed = elapsedSeconds;
if (session.StartedAt.HasValue)
{
    int wallClockElapsed = (int)(DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
    // Jangan izinkan client kirim nilai lebih besar dari wall-clock
    clampedElapsed = Math.Min(elapsedSeconds, wallClockElapsed);
}
// Jangan izinkan backward movement
clampedElapsed = Math.Max(clampedElapsed, session.ElapsedSeconds);
// Clamp ke durasi max
int durationSec = session.DurationMinutes * 60;
clampedElapsed = Math.Min(clampedElapsed, durationSec);
```

Kemudian gunakan `clampedElapsed` sebagai nilai yang disimpan ke DB.

**Efek:** ElapsedSeconds di DB selalu monotonically increasing dan tidak bisa melebihi wall-clock elapsed.

#### Fix 3: Tidak diperlukan perubahan client-side JS

Logic timer di JS sudah benar secara lokal (`timerStartRemaining - wallElapsed` selalu berkurang). Bug-nya ada di nilai awal `REMAINING_SECONDS_FROM_DB` yang tidak akurat dari server. Setelah Fix 1, nilai ini sudah benar.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Waktu server-authoritative | Logika timer tersendiri yang kompleks | `DateTime.UtcNow - StartedAt` | C# built-in, presisi millisecond |
| Prevent negative remaining | Custom floor logic | `Math.Max(0, remaining)` | Built-in, sudah ada di JS juga |

---

## Common Pitfalls

### Pitfall 1: Fix hanya di server tanpa clamp di UpdateSessionProgress
**Yang salah:** Hanya fix kalkulasi di StartExam, tapi UpdateSessionProgress masih terima nilai stale dari client saat ini.
**Kenapa terjadi:** Terlihat cukup hanya fix di "entry point" resume.
**Cara hindari:** Fix kedua tempat — StartExam (untuk nilai awal saat load) dan UpdateSessionProgress (untuk validasi saves ongoing).

### Pitfall 2: Wall-clock elapsed bisa melebihi durasi ujian
**Yang salah:** Tidak clamp `elapsedSec = Math.Min(elapsedSec, durationSeconds)` → `remainingSeconds` bisa negatif bahkan setelah cross-check.
**Kenapa terjadi:** Worker yang sedang mengerjakan bisa saja durasi ujian habis saat navigate away — ini valid, harus `ExamExpired = true`.
**Cara hindari:** Selalu clamp setelah max(), lalu cek `remainingSeconds <= 0`.

### Pitfall 3: StartedAt bisa null (first load, bukan resume)
**Yang salah:** Tidak check `assessment.StartedAt.HasValue` sebelum wall-clock calc → NullReferenceException.
**Kenapa terjadi:** Saat `justStarted = true`, `StartedAt` baru di-set di baris 769 tapi belum di-refresh di object yang sama.
**Cara hindari:** Guard check `if (assessment.StartedAt.HasValue)` atau hitung wall-clock hanya saat `!justStarted`.

### Pitfall 4: Backward movement ElapsedSeconds saat sendBeacon terlambat tiba
**Yang salah:** sendBeacon dari navigate-away bisa tiba setelah periodic save dari sesi baru → overwrite dengan nilai lebih kecil.
**Kenapa terjadi:** sendBeacon tidak dijamin ordering dengan request lain.
**Cara hindari:** Clamp di UpdateSessionProgress: `clampedElapsed = Math.Max(clampedElapsed, session.ElapsedSeconds)` (sudah ada di Fix 2).

---

## Code Examples

### Kalkulasi server-authoritative di StartExam

```csharp
// Source: CMPController.cs, bagian Resume state (setelah baris 908)
bool isResume = assessment.StartedAt != null;
int durationSeconds = assessment.DurationMinutes * 60;
int elapsedSec = assessment.ElapsedSeconds;

// Cross-check dengan wall-clock elapsed sejak StartedAt
// Ini fix untuk: (1) waktu habis mendadak, (2) timer bertambah
if (!justStarted && assessment.StartedAt.HasValue)
{
    int wallClockElapsed = (int)(DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
    // Server-authoritative: ambil yang lebih besar (paling tidak menguntungkan worker)
    // Ini mencegah stale ElapsedSeconds di DB memberikan "bonus waktu" palsu
    elapsedSec = Math.Max(elapsedSec, wallClockElapsed);
}

// Defensive clamp — tidak boleh melebihi durasi total
elapsedSec = Math.Min(elapsedSec, durationSeconds);

int remainingSeconds = durationSeconds - elapsedSec;

ViewBag.IsResume = isResume;
ViewBag.LastActivePage = assessment.LastActivePage ?? 0;
ViewBag.ElapsedSeconds = elapsedSec;          // nilai yang sudah dicross-check
ViewBag.RemainingSeconds = remainingSeconds;
ViewBag.ExamExpired = isResume && remainingSeconds <= 0;
```

### Validasi clamp di UpdateSessionProgress

```csharp
// Source: CMPController.cs UpdateSessionProgress, sebelum ExecuteUpdateAsync
int clampedElapsed = elapsedSeconds;

// Clamp 1: tidak boleh melebihi wall-clock elapsed
if (session.StartedAt.HasValue)
{
    int wallClockElapsed = (int)(DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
    clampedElapsed = Math.Min(clampedElapsed, wallClockElapsed);
}

// Clamp 2: tidak boleh mundur (monotonically increasing)
clampedElapsed = Math.Max(clampedElapsed, session.ElapsedSeconds);

// Clamp 3: tidak boleh melebihi durasi total
clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);

// Gunakan clampedElapsed untuk update
await _context.AssessmentSessions
    .Where(s => s.Id == sessionId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.ElapsedSeconds, clampedElapsed)
        .SetProperty(r => r.LastActivePage, currentPage)
        .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
    );
```

---

## State of the Art

| Old Approach | Current Approach | Mengapa Bermasalah |
|--------------|-----------------|-------------------|
| Server percaya `ElapsedSeconds` dari client 100% | Server cross-check dengan `StartedAt` | Client bisa kirim stale/salah nilai |
| sendBeacon + periodic save 30 detik | + server-side wall-clock validation | sendBeacon tidak reliable saat navigate |

---

## Open Questions

1. **Apakah perlu shorten periodic save interval (30s → 10s)?**
   - Yang diketahui: 30 detik adalah window di mana data bisa stale
   - Tidak jelas: apakah ada performance concern di server dev dengan banyak peserta?
   - Rekomendasi: Tidak perlu, karena Fix 1 (wall-clock cross-check) sudah menghilangkan efek negatif dari stale data. 30 detik tetap oke.

2. **Update ElapsedSeconds di DB saat StartExam load jika wall-clock lebih besar?**
   - Yang diketahui: Kita bisa sekalian update DB saat cross-check menemukan nilai lebih besar
   - Tidak jelas: Apakah perlu? Periodic save akan segera update-nya anyway
   - Rekomendasi: Tidak perlu — cukup ViewBag yang diupdate. DB akan catch up di periodic save berikutnya.

---

## Environment Availability

Step 2.6: SKIPPED — fase ini hanya perubahan kode server-side C# dan tidak ada external dependency baru.

---

## Validation Architecture

Test dilakukan manual oleh user di browser (server development http://10.55.3.3/KPB-PortalHC/).

### Phase Requirements → Test Map

| ID | Behavior | Test Type | Cara Verifikasi |
|----|----------|-----------|-----------------|
| BUG-01 | Timer tidak tiba-tiba habis saat resume setelah navigate away lama | Manual | Mulai ujian → navigate ke halaman lain → tunggu > 30 detik → resume → verifikasi remaining = full_duration - waktu_nyata_berlalu |
| BUG-02 | Timer tidak bertambah saat resume | Manual | Mulai ujian → catat timer (misal 25:30) → navigate away → resume → verifikasi timer lebih kecil dari 25:30 |
| EDGE-01 | ExamExpired = true jika resume setelah durasi habis | Manual | Mulai ujian 1 menit → tunggu > 1 menit → resume → verifikasi modal "waktu habis" muncul |
| EDGE-02 | ElapsedSeconds di DB tidak mundur jika sendBeacon terlambat | Auto-derived | Diverifikasi via Fix 2 di code review |

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` lines 705-917 — StartExam action, kalkulasi remainingSeconds
- `Controllers/CMPController.cs` lines 326-364 — UpdateSessionProgress action
- `Views/CMP/StartExam.cshtml` lines 315-382 — Client-side timer logic
- `Views/CMP/StartExam.cshtml` lines 620-639, 825-841 — Periodic save + sendBeacon
- `Models/AssessmentSession.cs` — Field definitions: `ElapsedSeconds`, `StartedAt`, `DurationMinutes`

---

## Metadata

**Confidence breakdown:**
- Root cause analysis: HIGH — verified langsung dari source code
- Fix strategy: HIGH — `Math.Max(DB, wallClock)` adalah standard server-authoritative timer pattern
- Pitfalls: HIGH — semua pitfall di-derive dari code reading, bukan asumsi

**Research date:** 2026-03-28
**Valid until:** Stable — tidak ada external library, hanya internal code logic
