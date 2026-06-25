# Phase 428: StartExam Write-on-GET Idempotency - Context

**Gathered:** 2026-06-25
**Status:** Ready for planning

<domain>
## Phase Boundary

GET `CMP/StartExam(id)` menjadi idempoten untuk **transisi status `Upcoming→Open`** (EXSEC-02): GET tidak lagi mem-persist mutasi `Upcoming→Open` ke DB. Transisi dihitung **in-memory (effective-status by-schedule)** sehingga gate dan rendering tetap benar, tanpa `SaveChangesAsync` untuk transisi status itu di jalur GET.

**Lokus tunggal:** `Controllers/CMPController.cs` blok 922-932 (auto-transition `Upcoming→Open` + `SaveChangesAsync`).

**EKSPLISIT DI LUAR scope fase ini (D-01):**
- Write `InProgress` + `StartedAt` (CMPController:1021-1026) — itu aksi "mulai ujian" aktual worker, TETAP di GET.
- `UserPackageAssignment` create + `SaveChangesAsync` (CMPController:1106-1123) — TETAP di GET.
- Gate apa pun (time-gate, GRDF-01, token-gate EXSEC-01, exam-window, duration, abandoned) — perilaku TIDAK berubah, hanya sumber status untuk gating jadi effective-status.

</domain>

<decisions>
## Implementation Decisions

### Cakupan (Scope)
- **D-01:** Hanya transisi `Upcoming→Open` yang dilepas dari persist-on-GET. `InProgress`/`StartedAt` write (1021-1026) dan assignment-create (1106-1123) **tetap di GET** — keduanya merepresentasikan aksi mulai ujian aktual, bukan side-effect status pasif. Menjaga perubahan seminimal mungkin sesuai catatan **R-1** (StartExam = zona konflik merge PASTI vs `main`).

### Mekanisme (Mechanism)
- **D-02:** **In-memory effective-status** — hitung `isEffectivelyOpen = Status=="Open" || (Status=="Upcoming" && Schedule <= DateTime.UtcNow.AddHours(7))` tanpa menulis DB. Hapus blok persist 922-932. Time-gate hanya memblok bila `Status=="Upcoming" && belum waktunya` (Upcoming + waktu tiba → lolos sebagai openable, tidak diblok, tidak di-persist). Pola identik dengan lobby `Assessment` (CMPController:245-251) yang sudah "display-only, no SaveChangesAsync".
  - Untuk worker yang benar-benar mulai: blok `justStarted` (1021) tetap menulis `Status="InProgress"` + `StartedAt` (Upcoming/Open → InProgress langsung). Itu satu-satunya persist status di jalur ini dan SAH (aksi start, di luar scope SC#1 yang menyasar `Upcoming→Open`).
  - Impersonation tetap read-only otomatis: satu-satunya persist-Open dihapus; InProgress write sudah di-guard `!IsImpersonating()`.

### Test/UAT
- **D-03:** **xUnit integration (real-SQL) + verify**, TANPA Playwright (UI hint=no, tak ada perubahan view). Test wajib buktikan:
  1. GET StartExam pada sesi `Upcoming` yang **waktunya sudah tiba** → response render exam (lolos gate) **TANPA** mengubah `Status` di DB (tetap `Upcoming` sampai ada start aktual). *(SC#1, SC#2)*
  2. GET StartExam pada sesi `Upcoming` yang **belum waktunya** → tetap diblok (redirect "belum dibuka"), `Status` tetap `Upcoming`. *(SC#3 time-gate)*
  3. GRDF-01: Post-Test dengan Pre belum Completed → tetap diblok di GET. *(SC#3)*
  4. Worker mulai ujian (justStarted) → `Status` jadi `InProgress` + `StartedAt` ter-set (alur exam-taking end-to-end utuh). *(SC#4)*
  5. (Regresi) token-gate EXSEC-01 (427) tetap berfungsi: `IsTokenRequired && StartedAt==null && TokenVerifiedAt==null` → diblok.
  - Idempotensi pembuktian: panggil GET dua kali (time-arrived, belum start) → `Status` DB tetap `Upcoming` di kedua panggilan (no write-on-GET untuk transisi status).

### Claude's Discretion
- Bentuk kode persis (inline `bool isEffectivelyOpen` vs ekstrak helper kecil `IsEffectivelyOpen(session, nowWib)`; jika diekstrak, pertimbangkan share dengan lobby 245-251 untuk kill-drift — tapi JANGAN over-refactor; R-1 minimal).
- Penamaan test + fixture (reuse `RetakeServiceFixture`/real-SQL pattern + CMPController test factory dari `RetakeExamEndpointTests`/`TokenVerifiedAtTests`).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/ROADMAP.md` — section "Phase 428: StartExam Write-on-GET Idempotency" (goal + 4 SC + R-1 merge note).
- `.planning/REQUIREMENTS.md` — requirement `EXSEC-02`.

### Implementasi (lokus + pola)
- `Controllers/CMPController.cs:908-1135` — method `StartExam(int id)` GET (target refactor 922-932; gate berurutan time/Completed/GRDF-01/token/window/duration/abandoned; justStarted InProgress write 1021-1026; assignment-create 1106-1123).
- `Controllers/CMPController.cs:245-251` — pola **in-memory effective-status** lobby `Assessment` (display-only, no SaveChanges) yang DI-MIRROR (D-02).
- `Controllers/AssessmentAdminController.cs:165-166,2928-3004,3485-3486` — grouping monitoring baca `Status` persisted (Open||InProgress→Open badge). Dikonfirmasi TIDAK regresi: InProgress write tetap ada → badge tetap flip ke Open saat worker mulai.

### Phase 427 (sekuensial, shared StartExam)
- `.planning/phases/427-exam-token-gate-server-authoritative/427-01-SUMMARY.md` — token-gate EXSEC-01 di StartExam:964-972 (server-authoritative `TokenVerifiedAt`). WAJIB tetap utuh; urutan gate GRDF-01 (setelah cek-Completed) → token-gate dipertahankan (R-1).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Pola in-memory transition lobby** (`CMPController.cs:245-251`): `if (Status=="Upcoming" && Schedule<=nowWib) Status="Open"` — display-only. Cermin langsung untuk D-02.
- **Test factory** `RetakeExamEndpointTests`/`TokenVerifiedAtTests` (real-SQL `RetakeServiceFixture` + `FakeUserStore`/`MakeUserManager` + `ImpersonationService` stub) — reuse untuk integration test StartExam GET.

### Established Patterns
- **Write-on-GET guard impersonation** sudah ada (`!_impersonationService.IsImpersonating()`) — setelah persist-Open dihapus, guard di blok 926-931 ikut hilang; InProgress write (1021) tetap pakai guard.
- Gate StartExam berurutan & sudah leak-safe; refactor HANYA mengganti sumber status pra-gate dari "persist lalu baca" → "effective in-memory".

### Integration Points
- `StartExam` GET dipanggil via redirect dari `VerifyToken` POST (903) + RetakeExam (2589) + link lobby. Semua tetap kompatibel (tak ubah signature/route).

</code_context>

<specifics>
## Specific Ideas

- Urutan WAJIB dipertahankan (R-1 merge-safety): cek `Completed` → **GRDF-01** (950-959) → **token-gate EXSEC-01** (964-972) → exam-window → duration → abandoned → justStarted InProgress write. Effective-status hanya menggantikan blok 922-932 + kondisi time-gate 935.
- Time-gate baru: blok bila `Status=="Upcoming" && Schedule > nowWib` (belum waktunya). Upcoming + waktu tiba → lolos (effective-open) tanpa persist.

</specifics>

<deferred>
## Deferred Ideas

- **Idempotensi GET penuh** (pindah `InProgress`/`StartedAt` + assignment-create ke jalur POST eksplisit) — refactor besar yang mengubah alur exam-taking; di luar scope EXSEC-02. Catat sebagai kandidat hardening masa depan bila diperlukan.
- **Admin monitoring effective-status by-schedule** (badge "Open" sebelum ada worker InProgress saat waktu tiba) — TIDAK diperlukan (tak ada regresi karena InProgress write tetap ada). Hanya kosmetik bila kelak diinginkan konsistensi badge pra-start.

*Reviewed Todos (not folded): none — todo match-phase 428 = 0.*

</deferred>

---

*Phase: 428-startexam-write-on-get-idempotency*
*Context gathered: 2026-06-25*
