# Phase 311 — ManageAssessment Performance: Design

**Status:** Approved 2026-05-07 — supersedes prior backend-query-optimization scope.
**Author:** Brainstorm session 2026-05-07 (post-baseline diagnostic).
**Replaces direction in:** `311-CONTEXT.md` (D-01..D-16), `311-01-PLAN.md` (Wave 0 baseline), `311-02-PLAN.md` (Wave 1+2 backend patches).
**Preserved from prior work:** Stopwatch instrumentation commit `a4ce556e` (D-11, D-13, D-16) — kept as ongoing perf telemetry.

---

## 1. Context & problem reframe

### 1.1 Original Phase 311 hypothesis (now invalidated)

`ManageAssessment` action di `Controllers/AssessmentAdminController.cs:60-262` dianggap lambat karena fetch 5 query inline (T1 Assessment chain, T2 GetWorkersInSection, T3 GetAllWorkersHistory, T4 Sections+Units, T5 Distinct Categories). Plan 02 lama target backend optimization: AsNoTracking + 2 indexes (LinkedGroupId, ExamWindowCloseDate) + Categories cache 5-min, dengan acceptance criteria p95 ≥30% reduction.

### 1.2 Baseline measurement findings (2026-05-07)

Per-segment Stopwatch instrumentation (commit `a4ce556e`) di-run lokal dengan SQLEXPRESS Dev DB, 3 cold runs:

| Run | T1 | T2 | T3 | T4 | T5 | Total |
|---|---|---|---|---|---|---|
| 1 (JIT warmup, skip) | 42 | 73 | 82 | 9 | 9 | 234 |
| 2 (warm) | 4 | 6 | 13 | 1 | 1 | 27 |
| 3 (warm) | 2 | 3 | 4 | 0 | 0 | 11 |

Backend total = 11-234ms cold, sangat cepat. **Backend bukan bottleneck** dalam skala apapun yang relevan dengan keluhan user.

### 1.3 Real bottleneck (Chrome DevTools, server `10.55.3.3` via wifi kantor)

- Request sent: 0.15ms
- Waiting for server response (TTFB): 281ms ← **server cepat**
- Content download: **1.6 menit** ← **biang lemot**
- Document size: 3.4 KB
- Page total: 8 requests, 196 KB transferred, finish 1.4 menit

User confirmation 3 datapoint network:
- Wifi lain → no lag
- Hotspot HP → <10 detik lag
- Wifi kantor → >1 menit lag

**Root cause:** proxy Pertamina (atau corporate firewall/DLP scanner) di jalur wifi kantor → server `10.55.3.3` mengakibatkan throughput drop ke ~40 byte/detik. Backend optimization tidak akan menyelesaikan masalah ini.

### 1.4 Strategic shift

Proxy adalah konstanta (di luar kontrol code). Strategi yang bekerja: **minimize apa yang harus melewati proxy**. Setiap KB HTML yang tidak perlu = beberapa detik penghematan user time. Ini secara langsung memotivasi lazy-load architecture: kirim shell ringan dulu, data per tab on demand.

---

## 2. Goal & acceptance criteria (revised)

### 2.1 Goal

Reduce perceived `ManageAssessment` load time pada wifi kantor (yang dimediasi proxy Pertamina) dengan lazy-load per tab via HTMX. Target user-facing latency dengan minimal code surface area.

### 2.2 Acceptance criteria

| Kriteria | Target | Method |
|---|---|---|
| Initial response document size | <14 KB (TCP first roundtrip) | curl/DevTools size |
| End-to-end load time wifi kantor (initial visit + active tab fetch) | ≤40 detik (dari baseline ~1.4 menit) | DevTools Network finish time |
| Tab switching latency post-initial-load | ≤2 detik | DevTools Network per tab |
| Smoke test parity | Visual identik (kolom, row count, ordering) per tab | Manual UAT |
| No regression backend perf | TTFB tetap ≤500ms | Stopwatch log + DevTools TTFB |
| Backward compat ViewBag/contract | Filter (search/category/status), pagination behavior preserved | Manual UAT |

### 2.3 Out of scope

- Network/proxy investigation (eskalasi paralel ke IT, bukan code domain)
- Service worker / advanced frontend caching
- WebSocket / real-time updates
- SSR streaming / SPA migration
- Phase 311 lama Plan 02 patches (AsNoTracking + indexes + Categories cache) — di-defer ke Plan 03 opsional sebagai opportunistic improvement, bukan dependency.

---

## 3. Architecture

### 3.1 High-level flow

```
User → GET /Admin/ManageAssessment?tab=assessment
       ↓
Server: render shell view (NO data fetch)
       ↓ Response: ~5-10 KB shell HTML + skeleton + HTMX tag
Browser: instant render shell + skeleton placeholders
       ↓
HTMX: hx-trigger="load" pada tab aktif fire
       ↓
GET /Admin/ManageAssessment/Tab/Assessment?[filters]
       ↓ Response: PartialView ~30-50 KB
HTMX: swap skeleton → real content di pane Assessment

User klik tab Training/History (pertama kali):
       ↓
HTMX: hx-trigger="click once" fire
       ↓
GET /Admin/ManageAssessment/Tab/{Training|History}
       ↓ Response: PartialView
HTMX: swap

User filter (search/category/status):
       ↓
HTMX: hx-trigger="change" pada form fire
       ↓
Re-fetch tab aktif dengan filter params updated
```

### 3.2 Component breakdown

#### Backend (`Controllers/AssessmentAdminController.cs`)

- **`ManageAssessment(...)` action — REFACTORED**: return shell view tanpa data fetch. Hanya populate ViewBag dengan: `ActiveTab`, `Categories` dropdown (cached, T5 logic preserved), filter values dari query string. No T1/T2/T3/T4 fetch.
- **`ManageAssessmentTab_Assessment(...)` action — NEW**: parameter mirror filter params. Eksekusi T1 logic (Assessment query chain L66-176 di kode lama). Return `PartialView("~/Views/Admin/Shared/_AssessmentGroupsTab.cshtml", model)`.
- **`ManageAssessmentTab_Training(...)` action — NEW**: eksekusi T2 + T4 logic. Return `PartialView("~/Views/Admin/Shared/_TrainingRecordsTab.cshtml", model)`.
- **`ManageAssessmentTab_History(...)` action — NEW**: eksekusi T3 logic. Return `PartialView("~/Views/Admin/Shared/_HistoryTab.cshtml", model)`.
- **Stopwatch instrumentation**: dipindah ke per-action (1 Stopwatch per partial endpoint), preserve telemetry pattern dari `a4ce556e`.

#### Frontend (`Views/Admin/`)

- **`ManageAssessment.cshtml` (shell) — REFACTORED**: hapus `<partial name="...">` calls. Tab pane jadi:
  ```html
  <div class="tab-pane fade show active" id="pane-assessment">
    <div hx-get="/KPB-PortalHC/Admin/ManageAssessmentTab_Assessment"
         hx-include="#filter-form"
         hx-trigger="load"
         hx-swap="innerHTML">
      <!-- skeleton placeholder -->
      <div class="placeholder-glow">
        <span class="placeholder col-12 mb-2"></span>
        <span class="placeholder col-8"></span>
      </div>
    </div>
  </div>
  ```
  Untuk tab Training/History: `hx-trigger="click from:#tab-training once"` (tab tidak auto-fetch sampai user klik).

- **`Shared/_AssessmentGroupsTab.cshtml`, `_TrainingRecordsTab.cshtml`, `_HistoryTab.cshtml`**: tetap struktur konten existing, tapi sekarang di-render via partial action endpoint, bukan inline.

- **Filter form (search, category, statusFilter)**: tambah `hx-trigger="change delay:500ms"` atau gunakan HTMX `hx-target` untuk re-fetch tab aktif saat filter berubah.

#### Library (HTMX)

- Vendor copy ke `wwwroot/lib/htmx/htmx.min.js` (~14 KB). Bukan CDN — proxy Pertamina mungkin block/lambat external CDN.
- Versi target: HTMX 2.0.x (latest stable). Kalau ada concern IE11 (legacy), fallback ke 1.9.x.
- Include di shell:
  ```html
  <script src="~/lib/htmx/htmx.min.js"></script>
  ```

### 3.3 URL & route conventions

Pakai existing controller routing convention:
- `GET /KPB-PortalHC/Admin/ManageAssessment` → shell (existing route, renamed semantically)
- `GET /KPB-PortalHC/Admin/ManageAssessmentTab_Assessment` → partial (new)
- `GET /KPB-PortalHC/Admin/ManageAssessmentTab_Training` → partial (new)
- `GET /KPB-PortalHC/Admin/ManageAssessmentTab_History` → partial (new)

Method names follow existing pattern di controller (PascalCase action methods, no attribute routing override).

### 3.4 Data flow detail

**Initial visit (cold cache):**
1. Browser: `GET /KPB-PortalHC/Admin/ManageAssessment?tab=assessment&page=1&pageSize=20`
2. Server: Stopwatch start. Populate ViewBag (Categories cached, ActiveTab, filter defaults). Return `View("ManageAssessment")`. Stopwatch stop, log.
3. Browser receive shell (~5-10 KB). HTML parse, skeleton instan visible.
4. Browser execute `<script src="htmx.min.js">`.
5. HTMX init: scan `hx-*` atributes. Tab aktif `hx-trigger="load"` fire immediately.
6. Browser: `GET /KPB-PortalHC/Admin/ManageAssessmentTab_Assessment?page=1&pageSize=20`
7. Server: T1 logic execute. Return `PartialView("_AssessmentGroupsTab", model)`.
8. Browser receive partial (~30-50 KB). HTMX swap skeleton → real content.

**Tab switch (e.g., user klik Training):**
1. User klik tab button.
2. Bootstrap activate tab Training pane (CSS).
3. HTMX `hx-trigger="click from:#tab-training once"` fire.
4. Browser: `GET /KPB-PortalHC/Admin/ManageAssessmentTab_Training?...`
5. Server: T2 + T4 logic execute. Return `PartialView`.
6. Browser receive, HTMX swap skeleton → content.
7. Subsequent klik tab Training (same session): no re-fetch karena `once`. Konten cached di DOM.

**Filter change:**
1. User type di search box / pilih category.
2. HTMX `hx-trigger="change delay:500ms from:#filter-form"` fire (debounced).
3. HTMX target = pane tab aktif (gunakan `hx-target="#pane-{activeTab}"`).
4. Re-fetch dengan filter params updated. Swap konten.

### 3.5 Error handling

- **HTMX `hx-on::response-error`**: kalau status 4xx/5xx dari partial endpoint, swap dengan template error sederhana:
  ```html
  <div class="alert alert-danger">
    Gagal memuat data. <a href="javascript:void(0)" onclick="htmx.trigger(this.parentElement.parentElement, 'load')">Coba lagi</a>
  </div>
  ```
- **Server endpoint validation**: action method validate filter params (search length, category exists, etc.). Return `BadRequest()` dengan pesan error untuk display.
- **Session expired**: kalau user idle dan login expired, partial endpoint return 401 atau redirect 302 ke login. HTMX auto-handle redirect (dengan `hx-redirect` atau full page reload via JS).
- **No-JS fallback**: kalau JS disabled (rare), tab tetap tampilkan skeleton kosong. Dokumentasikan sebagai known limitation; bukan target compat utama (browser modern + admin user = JS always on).

---

## 4. Testing strategy

### 4.1 Smoke test parity (per tab)

**Methodology:**
1. Capture screenshot ManageAssessment di server lama (April 16 commit `381b36cd`) untuk tiap tab.
2. Deploy new version (proper canary atau staging kalau ada).
3. Capture screenshot new version, side-by-side compare:
   - Header & nav structure
   - Tabel kolom names + ordering
   - Row count untuk dataset identik
   - Pagination footer (total pages, current page)
4. Pass kalau visual struktur identik. Acceptable difference: skeleton flash sebelum data load (expected new behavior).

### 4.2 Performance measurement

**Methodology:**
1. Lokasi: wifi kantor (untuk realistic measurement).
2. Browser: Chrome DevTools Network tab, "Disable cache" enabled, hard reload.
3. Per visit, capture:
   - Shell document: TTFB, content download, total time, size
   - Active tab partial: TTFB, content download, total time, size
   - Total page finish time
4. Run 3x cold (ulang dengan close/reopen browser tab antar run).
5. Skip Run 1 (warmup), median Run 2-3.
6. Compare dengan baseline lama (1.4 menit di wifi kantor):
   - Pass: ≥50% reduction (≤40 detik)
   - Sub-target: shell <14 KB, partial <50 KB

### 4.3 Filter & tab switch UAT

Manual checklist:
- [ ] Initial load: shell muncul instant, skeleton visible, data tab aktif fill in
- [ ] Tab switch (Training): skeleton → data muncul
- [ ] Tab switch lagi ke Training: instant (cached, no re-fetch)
- [ ] Tab History: same pattern
- [ ] Search box: ketik → debounce → re-fetch tab aktif
- [ ] Category dropdown: pilih → re-fetch
- [ ] Status filter: pilih → re-fetch
- [ ] Pagination klik: re-fetch dengan page param updated
- [ ] Hard reload: shell + active tab fetch lagi (no client cache)
- [ ] Session expired (idle 30 menit): handle redirect

### 4.4 No automated test

UAT-style sufficient untuk Phase 311. Justifikasi: existing test coverage di project minim (lihat `.planning/PROJECT.md` testing convention), perilaku ini high-touch UI yang lebih efisien di-verify manual. Kalau project di masa depan pindah ke automated UI test (Playwright?), lazy-load tabs jadi target test cocok.

---

## 5. Phase plan structure (proposed for /gsd-plan-phase)

- **Plan 01** (Wave 0 — DONE, preserved): Stopwatch instrumentation, commit `a4ce556e`. Telemetry retained as ongoing perf monitoring.
- **Plan 02** (REPLACE existing 311-02-PLAN.md): HTMX lazy load architecture
  - Wave 1 — Backend refactor:
    - Refactor `ManageAssessment` action → shell-only (no data fetch)
    - Create 3 partial actions (`ManageAssessmentTab_Assessment`, `_Training`, `_History`)
    - Move per-segment Stopwatch instrumentation to per-action context
  - Wave 2 — Frontend:
    - Vendor HTMX 2.0.x ke `wwwroot/lib/htmx/`
    - Refactor `ManageAssessment.cshtml` shell: HTMX attributes + skeleton
    - Filter form → HTMX `change` trigger
    - Error handling template
  - Wave 3 — Verification:
    - Smoke test parity per tab (visual compare)
    - Perf measurement di wifi kantor (DevTools)
    - UAT checklist (filter, tab switch, pagination, session)
    - Document hasil di SUMMARY.md
- **Plan 03** (OPTIONAL bonus — defer atau pisah phase): Opportunistic backend
  - AsNoTracking di T1 query chain
  - Add IX_AssessmentSessions_LinkedGroupId index
  - Categories cache 5-min via IMemoryCache
  - Targeted improvement <30% backend, tapi resilience untuk scaling future

---

## 6. Dependencies & risks

### 6.1 Deployment dependency

- Server `10.55.3.3` saat ini di commit `381b36cd` (April 16). Phase 311 work di laptop, belum deploy. **Tim IT yang deploy** — implementasi tidak akan tervalidasi end-to-end sampai IT push commit baru.
- **Mitigation:** Selesaikan code change + lokal verification, lalu coordinate dengan IT untuk deploy ke staging/dev server. Provide rollback steps.

### 6.2 Risks

- **HTMX learning curve**: Tim mungkin belum familiar. Mitigation: dokumentasi pattern di phase SUMMARY, contoh code di-reference.
- **Filter behavior regression**: filter form yang re-trigger fetch bisa miss edge cases (debounce timing, multi-select, empty values). Mitigation: UAT checklist eksplisit.
- **Skeleton CSS dependency**: pakai Bootstrap `.placeholder` class — sudah ada di project (Bootstrap 5). No new dep.
- **Race condition tab switch**: user cepat klik antar tab → multiple in-flight requests. Mitigation: HTMX auto-cancel previous via `hx-sync="this:replace"`. Dokumentasikan.
- **Wifi kantor measurement consistency**: variansi network bisa pengaruhi numbers. Mitigation: 3 runs, median, ulang kalau outlier signifikan.

### 6.3 Out-of-band escalation (paralel, non-blocking)

- Eskalasi ke IT/Network: keluhan akses `10.55.3.3` lambat dari wifi kantor. Sertakan datapoint baseline (TTFB 281ms, content download 1.4 menit, 3 wifi comparison). IT punya tools untuk trace (packet capture, proxy logs, traceroute). Phase 311 tidak menunggu hasil IT — code change valuable terlepas dari hasil investigasi proxy.

---

## 7. Memory & memory transitions

- **Update memory `project_311_wave0_checkpoint.md`**: tandai Plan 01 baseline complete (resolved via brainstorm 2026-05-07), Skenario decision = "REFRAMED — backend not bottleneck, lazy-load via HTMX adopted".
- **Add memory `project_311_design.md`**: pointer ke `311-DESIGN.md` ini untuk session berikutnya.

---

## 8. Next steps after approval

1. User review doc ini → approve / request changes.
2. Saya update memory + commit doc (kalau user setuju commit).
3. User run `/gsd-discuss-phase 311 --revise` untuk update CONTEXT.md dengan direction baru (decisions D-01..D-16 lama mostly superseded, decisions baru perlu dirumuskan formally).
4. User run `/gsd-plan-phase 311 --plan-num 02` untuk generate Plan 02 baru (HTMX lazy load) menggantikan existing 311-02-PLAN.md.
5. Existing `311-02-PLAN.md` di-archive atau replace.

---

*Brainstorm session: 2026-05-07*
*Approved direction: HTMX lazy load (Opsi 2 dari 6 yang dievaluasi).*
*Research sources: HTMX docs, Bootstrap 5 lazy load patterns, Cloudflare TCP 14KB rule, MDN deferred vs lazy loading.*
