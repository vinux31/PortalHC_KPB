# Phase 388: Label Hasil + CoachWorkload Polish (LBL-03 + DSN-04 + DSN-05) - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaikan teks & polish tampilan 2 file view yang **disjoint** (low-risk):
1. **LBL-03** — `Views/CMP/Results.cshtml`: label kartu "Nilai Kelulusan" → "Batas Nilai Kelulusan".
2. **DSN-04** — `Views/Admin/CoachWorkload.cshtml`: filter bar + section "Saran Penyeimbangan" dibungkus card konsisten dgn section lain.
3. **DSN-05** — `Views/Admin/CoachWorkload.cshtml`: bersihkan inline magic-number font-size + selaraskan spacing.

**Pure view edit (`.cshtml` + `@section Scripts` inline).** 0 backend, 0 controller, 0 migration, 0 perubahan endpoint/JS-contract. Behavior parity wajib (filter/export/threshold/setujui/lewati saran tetap jalan — diverifikasi tuntas di Phase 390, tapi tak boleh rusak di sini).

OUT: redesign CoachCoacheeMapping (itu Phase 389), perubahan kolom/fungsi baru, file view lain.
</domain>

<decisions>
## Implementation Decisions

### LBL-03 — Label hasil assessment
- **D-01:** `Views/CMP/Results.cshtml:60` `<h6 class="text-muted mb-2">Nilai Kelulusan</h6>` → `Batas Nilai Kelulusan`. HANYA tambah kata "Batas". `<h2>@Model.PassPercentage%</h2>` di bawahnya TIDAK diubah. Cek tak ada string "Nilai Kelulusan" lain yang ikut keganti (grep konfirmasi: hanya 1 match di file ini).

### DSN-05 — Cleanup inline styling
- **D-02:** Pendekatan = **Bootstrap utility class sebisanya + 1 blok `<style>` scoped minimal** di atas `CoachWorkload.cshtml`. TIDAK buat/extend file CSS bersama (records.css dsb) — overkill untuk 1 halaman.
- **D-03:** Magic-number font-size jadi util Bootstrap: `style="font-size:11px"` (L93 sublabel coachee/coach) & `style="font-size:12px"` (L104 badge "!") → kelas seperti `small` / `text-muted` / `fs-*` (pilih yang paling dekat secara visual). `style="font-size: 0.85rem"` (L156 legend container) boleh ke `small`.
- **D-04:** Legend dot inline (L157-159 `display:inline-block;width:12px;height:12px;border-radius:50%;...`) → 1 kelas scoped `.legend-dot` (+ modifier warna atau inline `background` saja yg ditahan, karena warna = data status). Chevron transition inline (L193/L461) boleh tetap atau ke kelas — Claude's discretion.
- **D-05:** Inline yg **fungsional/layout** (mis. `max-height:300px;overflow-y:auto` chart scroll L153, `min-height:150px` canvas L154 yg juga di-set via JS `canvas.style.height`, `max-width:300px` select L115) BOLEH dibiarkan inline — bukan target DSN-05 (yang disasar = magic-number font-size + spacing). Jangan utak-atik yg dipakai JS Chart.js (canvas height di-set runtime L321).

### DSN-04 — Card framing
- **D-06:** Filter bar (`<form method="get">` L114-131: select section + tombol Filter + Reset) dibungkus **`card border-0 shadow-sm` dengan `card-header` (bi-icon + judul, mis. `<i class="bi bi-funnel me-2"></i>Filter`)** — konsisten penuh dgn card "Grafik Beban Coach" / "Detail Beban Coach". Form pindah ke `card-body`.
- **D-07:** Section "Saran Penyeimbangan" jadi **1 card** (`card-header` ikon+judul + `card-body`); tiap item saran jadi **baris `list-group`/`list-group-flush`** DI DALAM card-body — BUKAN card sendiri lagi (hilangkan card-in-card nesting).
- **D-08 (PARITY KRITIS):** Saat ubah item saran → list-group, **WAJIB pertahankan semua hook JS**: tiap baris tetap `id="sug-@sug.MappingId"` + class mengandung `suggestion-card` (dipakai `btn.closest('.suggestion-card')` di approve/skip handler); tombol tetap `.approve-btn`/`.skip-btn` + semua `data-*` (`data-mapping-id`, `data-new-coach-id`, `data-coachee-name`, `data-from-coach`, `data-to-coach`); `fadeOutCard()` jalan di elemen baris baru. Empty-state "Tidak ada saran penyeimbangan" + role-gate `User.IsInRole("Admin")` pada tombol TIDAK berubah.

### Claude's Discretion
- Pemilihan kelas Bootstrap font-size yang paling dekat secara visual (D-03).
- Penyelarasan spacing (margin/gap) antar section — pakai util Bootstrap (`mb-*`/`g-*`) konsisten.
- Apakah chevron transition dipindah ke kelas atau dibiarkan (D-04).
- Ikon `bi-*` mana untuk card-header filter & saran (pilih yang relevan, mis. `bi-funnel`, `bi-arrow-left-right`/`bi-shuffle`).

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" [area=database, score=0.6] — **tidak di-fold**: itu cleanup DB pasca Phase 367, tak relevan dengan phase view/teks ini.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirement & roadmap
- `.planning/REQUIREMENTS.md` — LBL-03, DSN-04, DSN-05 (acceptance) + Out of Scope.
- `.planning/ROADMAP.md` §v32.1 Phase 388 — success criteria + canonical refs.

### Target files
- `Views/CMP/Results.cshtml` (~L60) — target LBL-03.
- `Views/Admin/CoachWorkload.cshtml` (1-525) — target DSN-04 (filter bar L114-131, "Saran Penyeimbangan" L229-272) + DSN-05 (inline font-size L93/L104/L156, legend dot L157-159). `@section Scripts` L310-524 = approve/skip/threshold JS + Chart.js (JANGAN ubah kontrak).

### Pattern referensi (card idiom yang ditiru)
- `Views/Admin/CoachWorkload.cshtml` L150-162 (card "Grafik Beban Coach") & L165-225 (card "Detail Beban Coach") — model `card shadow-sm` + `card-header fw-semibold` + bi-icon untuk D-06/D-07.

No external ADR/spec — requirements fully captured in decisions above.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Card idiom app: `card border-0 shadow-sm` + `card-header fw-semibold` + Bootstrap Icon `bi-*` (sudah dipakai di chart/table card halaman yang sama → tiru verbatim).
- Bootstrap 5 utility classes (`small`, `text-muted`, `fs-*`, `mb-*`, `g-*`) — ganti magic-number tanpa file CSS baru.
- `list-group`/`list-group-flush` Bootstrap — untuk item saran tanpa nesting card.

### Established Patterns
- AJAX approve/skip pakai `fetch((window.basePath || '') + '/Admin/...')` + `RequestVerificationToken` (L396/L434/L495). PathBase-aware — JANGAN hardcode.
- JS bergantung pada selector struktural: `.suggestion-card`, `#sug-{id}`, `.approve-btn`, `.skip-btn`, `.expand-chevron`, `#workloadChart`, threshold modal ids. Refactor markup HARUS jaga selector ini.
- Chart.js set `canvas.style.height` runtime (L321) — jangan hapus elemen/id `#workloadChart`.

### Integration Points
- Tak ada integrasi baru. Murni re-markup view; controller `CoachMapping`/`Admin` + endpoint `ApproveReassignSuggestion`/`SkipReassignSuggestion`/`SetWorkloadThreshold`/`ExportCoachWorkload` TIDAK disentuh.

### Verification approach
- `dotnet build` (Razor compile) + `dotnet run` localhost:5277 + Playwright/UAT browser: label hasil, filter section, export, set threshold (Admin), setujui & lewati saran tetap jalan. (Parity tuntas di Phase 390; jangan rusak di sini.)
</code_context>

<specifics>
## Specific Ideas

- Arah desain ditetapkan via brainstorm + visual-companion (screen diagnosis): CoachWorkload "sudah lumayan, minor" → polish-only; yang ditandai = filter bar telanjang + heading "Saran Penyeimbangan" polos + inline magic-number. Tampilan summary cards / chart / tabel yang sudah bagus JANGAN diubah.
</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase. (Redesign CoachCoacheeMapping = Phase 389; verifikasi parity penuh = Phase 390.)
</deferred>

---

*Phase: 388-label-hasil-coachworkload-polish-lbl-03-dsn-04-dsn-05*
*Context gathered: 2026-06-17*
