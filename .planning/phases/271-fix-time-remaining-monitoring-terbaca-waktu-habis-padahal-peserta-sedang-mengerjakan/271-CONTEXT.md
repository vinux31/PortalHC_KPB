# Phase 271: Fix Timer Ujian — Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaiki bug timer ujian yang menyebabkan: (1) waktu tiba-tiba habis saat resume padahal masih sisa waktu, (2) timer bertambah setelah resume (misal 25:30 → 25:35). Kedua bug berakar pada `ElapsedSeconds` yang tidak reliable saat navigate away / refresh.

</domain>

<decisions>
## Implementation Decisions

### Root cause
- **D-01:** Kedua bug (waktu habis tiba-tiba + timer bertambah) punya **satu root cause** — `ElapsedSeconds` yang dikirim client ke server tidak akurat saat navigate away atau refresh
- **D-02:** `sendBeacon` pada `beforeunload` tidak selalu reliable (terutama saat navigate away ke halaman lain, bukan close tab)
- **D-03:** Periodic save setiap 30 detik berarti ada window sampai 30 detik di mana `ElapsedSeconds` di DB bisa outdated

### Skenario bug
- **D-04:** Bug terjadi di **worker page saat resume** — user mulai ujian, navigate away (ke home/menu lain), masuk lagi → waktu habis atau timer bertambah
- **D-05:** Juga mungkin terjadi saat browser refresh

### Strategi fix: Server-authoritative timer
- **D-06:** Server harus punya **source of truth sendiri** — gunakan `StartedAt` + `DateTime.UtcNow` sebagai cross-check, jangan murni percaya `ElapsedSeconds` dari client
- **D-07:** Saat resume, server hitung remaining berdasarkan: `remaining = (DurationMinutes × 60) - max(ElapsedSeconds_dari_DB, elapsed_dari_StartedAt)` — ambil yang lebih masuk akal
- **D-08:** Client tetap kirim `elapsedSeconds` untuk periodic save, tapi server harus **validate/clamp** nilainya agar tidak melebihi wall-clock elapsed

### Scope
- **D-09:** Scope terbatas ke fix 2 bug ini saja — tidak ada refactor besar atau fitur timer baru
- **D-10:** Tidak ada bug timer lain yang ditemukan saat UAT selain 2 ini

### Claude's Discretion
- Exact validation/clamp logic di server
- Apakah perlu adjust periodic save interval (30 detik → lebih pendek)
- Detail implementasi cross-check `StartedAt` vs `ElapsedSeconds`

</decisions>

<specifics>
## Specific Ideas

- Timer harus **selalu berkurang** setelah resume, tidak pernah bertambah
- Saat resume setelah navigate away, remaining harus akurat berdasarkan waktu real yang sudah berlalu sejak exam dimulai
- Jangan sampai worker yang masih punya sisa waktu tiba-tiba dapat "waktu habis"

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above.

### Key source files
- `Controllers/CMPController.cs` — Server-side timer logic: StartExam (line ~705-917), UpdateSessionProgress (line ~329-364)
- `Views/CMP/StartExam.cshtml` — Client-side timer: countdown (line ~320-382), periodic save (line ~621-639), sendBeacon (line ~827-840)
- `Models/AssessmentSession.cs` — Timer fields: `DurationMinutes`, `ElapsedSeconds`, `StartedAt`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `UpdateSessionProgress` endpoint: sudah handle save `ElapsedSeconds` ke DB — perlu tambah validation logic
- `sendBeacon` + periodic save pattern: sudah ada, perlu diperkuat reliability-nya

### Established Patterns
- Wall-clock based countdown di client (`Date.now()` anchor) — pattern sudah benar, masalah di sinkronisasi dengan server
- `ViewBag.RemainingSeconds` dan `ViewBag.ElapsedSeconds` pattern untuk pass data ke view

### Integration Points
- `StartExam` action: tempat kalkulasi `remainingSeconds` saat resume — perlu fix kalkulasi di sini
- `UpdateSessionProgress` action: tempat validasi `ElapsedSeconds` dari client
- `beforeunload` handler: tempat `sendBeacon` yang tidak reliable

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 271-fix-time-remaining-monitoring-terbaca-waktu-habis-padahal-peserta-sedang-mengerjakan*
*Context gathered: 2026-03-28*
