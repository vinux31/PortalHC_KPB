# Phase 311: ManageAssessment Performance - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-07
**Phase:** 311-manageassessment-performance
**Mode:** interactive discuss-phase --revise (supersedes initial discussion 2026-05-05)
**Areas discussed:** Trigger semantics, Filter form behavior, Tab cache invalidation strategy, Fate of old backend patches

**Pre-discussion context:**
- Phase 311 reframed via brainstorm 2026-05-07 from backend query optimization to HTMX lazy-load architecture
- See `311-DESIGN.md` (approved 2026-05-07) for full design rationale
- Old discussion log entries (2026-05-05) for D-01..D-16 superseded; preserved in git history (commits `fd2bcf15`, `44b11e66`)

---

## Trigger Semantics

### Q1: Inactive tab (Training/History) trigger mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Bootstrap event (Recommended) | hx-trigger='shown.bs.tab from:closest button.nav-link once' — listens to Bootstrap 5 native event setelah tab visually aktif | ✓ |
| HTMX click selector | hx-trigger='click from:#tab-training-button once' — langsung listen click pada button | |
| You decide | Claude pilih (rekomendasi: Bootstrap event) | |

**User's choice:** Bootstrap event (Recommended)
**Notes:** Respect Bootstrap tab lifecycle — fire AFTER animation done dan DOM ready, hindari swap di tengah transisi.

### Q2: HTMX version untuk vendoring lokal

| Option | Description | Selected |
|--------|-------------|----------|
| HTMX 2.0.x (Recommended) | Latest stable 2026, drops IE11 support, smaller bundle, modern API | ✓ |
| HTMX 1.9.x | Still supported, IE11 compatible, slightly larger bundle | |
| You decide | Claude pilih (rekomendasi: 2.0.x latest stable) | |

**User's choice:** HTMX 2.0.x (Recommended)
**Notes:** Admin user di kantor pakai Chrome/Edge modern → drop IE11 acceptable.

---

## Filter Form Behavior

### Q3: Filter form (search/category/status) — live filter atau submit button?

| Option | Description | Selected |
|--------|-------------|----------|
| Live filter dengan debounce (Recommended) | Search box debounce 500ms, dropdown category/status instant fetch saat change | ✓ |
| Explicit submit button | User isi semua filter, klik tombol 'Cari'. Tidak ada live filter | |
| Hybrid | Dropdown instant, search box pakai submit button atau Enter | |
| You decide | Claude pilih (rekomendasi: Live filter dengan debounce 500ms) | |

**User's choice:** Live filter dengan debounce (Recommended)
**Notes:** UX modern, payload kecil (~30-50KB) acceptable lewat proxy lambat.

### Q4: Pagination behavior

| Option | Description | Selected |
|--------|-------------|----------|
| AJAX via HTMX (Recommended) | Klik 'Next page' → hx-trigger fire pada link → swap konten tab dengan page baru | ✓ |
| Full page reload | Klik 'Next page' → navigate ke /Admin/ManageAssessment?page=2 → full reload | |
| You decide | Claude pilih (rekomendasi: AJAX via HTMX) | |

**User's choice:** AJAX via HTMX (Recommended)
**Notes:** Konsisten dengan pattern HTMX di tab lain.

---

## Tab Cache Invalidation Strategy

### Q5: Saat filter berubah, apa yang terjadi ke tab non-aktif yang sudah di-load?

| Option | Description | Selected |
|--------|-------------|----------|
| Invalidate semua tab (Recommended) | Filter change → fetch ulang tab aktif + clear data tab lain (skeleton lagi). Saat user buka tab lain, fetch ulang dengan filter baru | ✓ |
| Hanya invalidate tab aktif | Filter change → fetch ulang tab aktif saja. Tab non-aktif keep data lama (stale) | |
| Tidak invalidate apapun + tombol Refresh per tab | Filter change → fetch tab aktif. Tab non-aktif keep stale, user explicit klik 'Refresh' | |
| You decide | Claude pilih (rekomendasi: Invalidate semua tab) | |

**User's choice:** Invalidate semua tab (Recommended)
**Notes:** Data konsisten, no stale state. Cost: 1 round trip extra saat user pindah tab post-filter (acceptable, 30-50 KB partial).

### Q6: HTTP cache headers untuk partial endpoint response

| Option | Description | Selected |
|--------|-------------|----------|
| no-store (Recommended) | 'Cache-Control: no-store' — browser tidak cache response | ✓ |
| private, max-age=60 | Browser boleh cache response 60 detik | |
| You decide | Claude pilih (rekomendasi: no-store — hindari double-cache complexity) | |

**User's choice:** no-store (Recommended)
**Notes:** HTMX manage tab cache di JS-side, tidak butuh HTTP cache. Hindari complexity dual-layer.

---

## Fate of Old D-01..D-10 Backend Patches

### Q7: Old backend patches (AsNoTracking + IX_LinkedGroupId + IX_ExamWindowCloseDate + Categories MemoryCache 5min)

| Option | Description | Selected |
|--------|-------------|----------|
| Plan 03 opportunistic same-phase (Recommended) | Keep di Phase 311, jadikan Plan 03 yang dijalankan PARALEL/SETELAH Plan 02 HTMX | ✓ |
| Defer ke phase terpisah | Drop dari Phase 311 entirely. Bikin phase baru saat ada kapasitas | |
| Drop entirely | Tidak relevan lagi (backend bukan bottleneck). Update REQUIREMENTS.md PERF-01 | |
| You decide | Claude pilih (rekomendasi: Plan 03 opportunistic same-phase) | |

**User's choice:** Plan 03 opportunistic same-phase (Recommended)
**Notes:** Low-cost (~50 baris kode + 1 migration), small wins backend (10-20% per partial action), resilience untuk scaling future. Tidak block Plan 02.

### Q8: REQUIREMENTS.md PERF-01 acceptance criteria

| Option | Description | Selected |
|--------|-------------|----------|
| Update PERF-01 dengan strategy & criteria baru (Recommended) | Edit REQUIREMENTS.md: ganti '≥30% reduction backend' jadi acceptance HTMX (≤40s wifi kantor, <14KB initial doc, ≤2s tab switch) | ✓ |
| Keep PERF-01 as-is + note di SUMMARY.md | REQUIREMENTS.md not touched. Phase 311 SUMMARY.md jelaskan deviation | |
| You decide | Claude pilih (rekomendasi: Update PERF-01 di Plan 02 awal) | |

**User's choice:** Update PERF-01 dengan strategy & criteria baru (Recommended)
**Notes:** Requirements jadi truth-source. Update di Plan 02 sebagai task awal.

---

## Done?

### Q9: Ready untuk write CONTEXT.md, atau explore more gray areas?

| Option | Description | Selected |
|--------|-------------|----------|
| Ready untuk context (Recommended) | Decisions sudah cukup untuk planner. Hal-hal kecil jadi Claude's Discretion | ✓ |
| Explore more gray areas | Identify 2-3 area tambahan untuk discuss | |

**User's choice:** Ready untuk context (Recommended)

---

## Claude's Discretion (areas user did not pin down)

- **HTMX swap mode**: `innerHTML` (default) vs `outerHTML`. Default: `innerHTML`.
- **Skeleton style**: Bootstrap 5 `.placeholder-glow` + `.placeholder` classes (sudah ada di project) atau custom CSS. Default: Bootstrap classes.
- **Error template**: Simple alert + retry button. Style by Claude.
- **Partial action method naming**: `ManageAssessmentTab_Assessment`, `_Training`, `_History` (PascalCase, underscore separator) sesuai konvensi controller existing.
- **Migration timestamp** (Plan 03): auto-generated by `dotnet ef migrations add AddManageAssessmentPerfIndexes`.
- **Active tab `hx-trigger`**: `hx-trigger="load"` (immediate) atau `hx-trigger="load delay:50ms"`. Default: `load`.
- **Race condition**: HTMX `hx-sync="this:replace"` untuk auto-cancel previous in-flight saat tab switching cepat.

---

## Deferred Ideas (not in current phase scope)

- Composite index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` — defer kalau Plan 03 single-column indexes belum cukup
- Persisted computed column `EffectiveDate = COALESCE(ExamWindowCloseDate, Schedule)` — schema migration overhead
- Frontend service worker / advanced caching — overkill
- WebSocket / real-time updates — different concern
- Lazy-load pattern reuse di halaman lain (ManageWorkers, Coach Workload, etc.) — defer ke milestone berikutnya
- MiniProfiler / Application Insights / OpenTelemetry — separate observability phase
- Client-side filtering — contradicts payload-reduction goal

## Reviewed Todos (not folded)

- `realtime-assessment.md` (2026-03-09, score 0.6) — concept real-time assessment monitoring. Different concern, tidak relevan untuk Phase 311. Stays di todos backlog.
