# Phase 307: Selected Participants Inline View - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-29
**Phase:** 307-selected-participants-inline-view
**Areas discussed:** Penempatan & visibility panel, Strategi extract helper, Real-time wiring & filter, Display detail & Proton parity, Wiring strategy refinement (post-code-verify)

---

## Penempatan & visibility panel

### Q1: Di mana panel 'Peserta Terpilih' ditempatkan di Step 2?

| Option | Description | Selected |
|--------|-------------|----------|
| Inline setelah userCheckboxContainer | Panel muncul tepat setelah div userCheckboxContainer line 309 — minimal change, alur natural top-down | ✓ |
| Sticky bottom card Step 2 | Panel sticky di bawah card Step 2 — selalu visible saat scroll list panjang | |
| Card terpisah di bawah Step 2 | Panel sebagai card mandiri — visual separation tegas tapi menambah vertical scroll | |

**User's choice:** Inline setelah userCheckboxContainer (Recommended)
**Notes:** Mengikuti rekomendasi default — minimal change & alur baca natural.

### Q2: Apa behavior panel saat 0 peserta dipilih?

| Option | Description | Selected |
|--------|-------------|----------|
| Tampilkan 'Belum ada peserta dipilih' | Panel selalu visible dengan pesan kosong — konsisten informational pattern | ✓ |
| Hide panel sampai ≥1 dipilih | Panel hanya appear setelah ada centang — risk layout shift | |

**User's choice:** Tampilkan 'Belum ada peserta dipilih' (Recommended)
**Notes:** Konsistensi dengan selectedCountBadge default '0 terpilih'.

### Q3: Bagaimana panel berperilaku di Proton mode (Assessment Proton)?

| Option | Description | Selected |
|--------|-------------|----------|
| Single panel, switch data source by isProton | Mirror pattern populateSummary line 1053-1058 — DRY, single DOM node | ✓ |
| Dua panel terpisah | Satu per container — visual coupling tegas tapi duplikasi DOM + JS | |
| Hide panel saat Proton mode | Trade-off: kehilangan parity Step 2 ↔ Step 4 untuk Proton | |

**User's choice:** Single panel, switch data source by isProton (Recommended)
**Notes:** Konsisten dengan flow existing populateSummary.

### Q4: Bagaimana hubungan panel baru dengan selectedCountBadge (line 289) yang sudah ada?

| Option | Description | Selected |
|--------|-------------|----------|
| Keep separate, panel tambahan di bawah container | Phase 304 D-18 stability — additive, tidak break consumer JS lain | ✓ |
| Move count ke header panel, hapus selectedCountBadge | Cleaner tapi modify line 288-290 = lebih banyak permukaan perubahan | |
| Render count di kedua tempat (badge filter + panel header) | Konsisten sumber tapi sedikit redundansi | |

**User's choice:** Keep separate, panel tambahan di bawah container (Recommended)
**Notes:** Stability principle Phase 304 dipertahankan.

---

## Strategi extract helper

### Q1: Lokasi & scope helper renderSelectedParticipants?

| Option | Description | Selected |
|--------|-------------|----------|
| Inline named function di CreateAssessment.cshtml | Konsisten Phase 304 D-09 / D-15 (inline preferred) | ✓ |
| Extract ke wwwroot/js/wizardParticipants.js | Better testability tapi premature per Phase 305 D-03 (n>=4 callers justify) | |
| IIFE module di bottom CreateAssessment.cshtml | Encapsulation tanpa file baru — middle ground | |

**User's choice:** Inline named function di CreateAssessment.cshtml (Recommended)
**Notes:** 2 caller (Step 2 + Step 4) tidak justify file split.

### Q2: Signature & API style helper?

| Option | Description | Selected |
|--------|-------------|----------|
| Imperative: renderSelectedParticipants(targetEl, checkboxes, opts) | Match success criteria #3 wording — simpler caller code | ✓ |
| Pure function: returns HTML string | Lebih testable tapi caller perlu safe innerHTML assignment | |
| Pure data: returns {count, first5, remaining} | Maximum flexibility tapi duplikasi render logic | |

**User's choice:** Imperative: renderSelectedParticipants(targetEl, checkboxes, opts) (Recommended)
**Notes:** Match success criteria signature literal.

### Q3: Apakah helper juga absorb logic count badge update?

| Option | Description | Selected |
|--------|-------------|----------|
| Helper update count badge via opts.countBadgeEl | Self-contained, opt-in | ✓ |
| Helper hanya render list, count badge tetap di caller | Minimal disruption tapi sedikit duplikasi count logic | |
| Helper return data, semua rendering di caller | Tidak relevan — sudah pilih imperative di Q2 | |

**User's choice:** Helper update count badge via opts.countBadgeEl (Recommended)
**Notes:** Step 2 caller pass null (selectedCountBadge handled by updateSelectedCount), Step 4 caller pass #summary-peserta-count.

### Q4: Bagaimana strategi expand button '...dan N lainnya' setelah refactor?

| Option | Description | Selected |
|--------|-------------|----------|
| Helper render button + bind onclick di setiap call | Mirror existing pattern populateSummary line 1087 — no listener leak | ✓ |
| Event delegation di targetEl untuk button click | Lebih clean tapi over-engineering untuk 1 button | |
| Tetap pakai onclick assignment seperti existing code | Mirror exact pattern existing — paling konservatif | |

**User's choice:** Helper render button + bind onclick di setiap call (Recommended)
**Notes:** Mirror pattern populateSummary existing.

---

## Real-time wiring & filter

### Q1: Bagaimana strategi event delegation untuk checkbox change?

| Option | Description | Selected |
|--------|-------------|----------|
| Delegated listener di kedua container untuk change event | Bind 1 listener per container — handle Proton AJAX-loaded checkbox | ✓ |
| Per-checkbox change listener | Fragile saat Proton mode load checkbox dynamic | |
| MutationObserver pada container | Over-engineering untuk simple checkbox toggle | |

**User's choice:** Delegated listener di kedua container untuk change event (Recommended)
**Notes:** Match success criteria #2.

### Q2: Bagaimana panel berperilaku saat filter (#userSearchInput / #sectionFilter) aktif?

| Option | Description | Selected |
|--------|-------------|----------|
| Panel reflect SEMUA selected (across filters) | Konsisten Step 4 — abaikan filter visibility | ✓ |
| Panel reflect hanya visible filtered selected | Bingung: pilih 20 lalu filter 'Operasi' panel tiba-tiba 5 — jangan | |
| Tampilkan dua section: 'Terpilih (visible)' + 'Terpilih (filtered out)' | Lebih informatif tapi double rendering complexity | |

**User's choice:** Panel reflect SEMUA selected (across filters) (Recommended)
**Notes:** Match populateSummary line 1056-1058 behavior.

### Q3: Apa yang di-debounce dengan delay 100ms (per success criteria #4)?

| Option | Description | Selected |
|--------|-------------|----------|
| Render DOM panel — immediate count badge update | Best UX: count instant feedback, list batch render saat selectAll | ✓ |
| Debounce semua (count + render) | Single timer tapi count terasa laggy untuk single click | |
| No debounce, immediate render selalu | Tidak comply success criteria #4 | |

**User's choice:** Render DOM panel — immediate count badge update (Recommended)
**Notes:** Selective debounce — cheap update immediate, expensive update debounced.

### Q4: Kapan panel pertama kali di-render?

| Option | Description | Selected |
|--------|-------------|----------|
| On DOMContentLoaded dengan pre-selected dari ViewBag | Edit mode prefill SelectedUserIds langsung tampil correct | ✓ |
| On step navigate ke Step 2 (saat goToStep(2)) | Defer kerja DOM tapi inkonsisten dengan #selectedCountBadge | |
| On first checkbox event (lazy) | Pre-selected state tidak tampil sampai user touch — bug-prone | |

**User's choice:** On DOMContentLoaded dengan pre-selected dari ViewBag (Recommended)
**Notes:** Mirror line 1451 `updateSelectedCount();` di akhir IIFE init.

---

## Display detail & Proton parity

### Q1: Format display panel Step 2 dibanding Step 4 summary?

| Option | Description | Selected |
|--------|-------------|----------|
| Exact match Step 4 | Single source of truth visual = parity guaranteed (success criteria #5) | ✓ |
| Chips/pills (badge per nama) | Visual richer tapi divergent dari Step 4 — risk no-divergence break | |
| Two-column (nama \| section) | Heavy render untuk 50+, divergent | |

**User's choice:** Exact match Step 4 (Recommended)
**Notes:** Match success criteria #5 absolute.

### Q2: Konten per nama — nama saja atau dengan info tambahan?

| Option | Description | Selected |
|--------|-------------|----------|
| Nama saja (parity Step 4) | Match output populateSummary line 1063-1068 verbatim | ✓ |
| Nama + Section dalam parentheses | Lebih informatif tapi populateSummary perlu di-update juga | |
| Nama dengan tooltip email/section saat hover | Tidak discoverable di mobile (no hover) | |

**User's choice:** Nama saja (parity Step 4) (Recommended)
**Notes:** Konsistensi Step 2 = Step 4 absolute.

### Q3: Sumber data nama saat extract checkbox info?

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse label/strong query (mirror Step 4) | Zero schema change, helper tinggal extract logic existing 1:1 | ✓ |
| Pakai data-name attribute (line 301) yang sudah ada | Cleaner tapi populateSummary perlu di-update juga | |
| Hybrid: data-name primary, label/strong fallback | Over-engineered — keduanya ada di markup | |

**User's choice:** Reuse label/strong query (mirror Step 4) (Recommended)
**Notes:** Helper jadi extract murni dari populateSummary line 1062-1067.

### Q4: Wording empty state untuk Step 2?

| Option | Description | Selected |
|--------|-------------|----------|
| 'Belum ada peserta dipilih' | Neutral, friendly, hint actionable | ✓ |
| Berbeda per mode: 'peserta' / 'coachee' | Lebih akurat tapi butuh logic switch by isProton | |
| '0 peserta dipilih' (parallel selectedCountBadge format) | Match exact format badge tapi kurang inviting | |

**User's choice:** 'Belum ada peserta dipilih' (Recommended)
**Notes:** Generic Indonesian term cukup untuk normal + Proton mode.

---

## Wiring strategy refinement (post-code-verify)

Saya lakukan verifikasi kode existing setelah Area 4. Menemukan: `updateSelectedCount()` sudah exist (line 1436), `selectAllBtn` / `deselectAllBtn` manual call ke fungsi ini, dan Proton IIFE punya duplikat count logic. Ini membuat 3 keputusan tambahan crucial:

### Q1: Bagaimana wiring panel render dengan updateSelectedCount() existing (line 1436)?

| Option | Description | Selected |
|--------|-------------|----------|
| Extend updateSelectedCount() — fungsi yang sama panggil panel render | Single function = single source of truth, selectAllBtn auto-refresh | ✓ |
| Wrapper baru refreshSelectedView() yang call both | Semantic separation tapi harus ubah semua call site existing | |
| Panel render di-attach event listener terpisah, dispatchEvent('change') | Risk regresi side-effect di selectAll | |

**User's choice:** Extend updateSelectedCount() (Recommended)
**Notes:** Minimal touch points, selectAll/deselectAll piggyback existing manual call.

### Q2: Bagaimana refactor Proton IIFE block (line 1504-1520) yang punya duplikat count logic?

| Option | Description | Selected |
|--------|-------------|----------|
| Replace inline change listener line 1515-1519 dengan call updateSelectedCount() | Hilangkan duplikasi, konsisten single source of truth | ✓ |
| Keep duplikat, hanya tambah call renderSelectedParticipants() inline | Tech debt persist | |
| Extract Proton change wiring ke main IIFE | Significant refactor, scope creep risk | |

**User's choice:** Replace inline change listener (Recommended)
**Notes:** Konsisten dengan main mode line 1450.

### Q3: Apakah panel render menggunakan DocumentFragment (per success criteria #4)?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, render dengan DocumentFragment + debounce 100ms | Match success criteria #4 verbatim, 50+ peserta < 200ms verifiable | ✓ |
| innerHTML string concat (mirror existing populateSummary line 1079) | Lebih simple tapi tidak literal match success criteria | |
| Hybrid: textContent untuk first-5, DocumentFragment untuk full expand | Lebih nuanced tapi over-design | |

**User's choice:** Ya, render dengan DocumentFragment + debounce 100ms (Recommended)
**Notes:** Forward compatibility + match success criteria literal.

---

## Claude's Discretion

Areas where Claude has flexibility during plan/execute (5 items captured as CD-01 through CD-07 in CONTEXT.md):

- CD-01: Visual styling panel exact (Bootstrap 5.3 idiom)
- CD-02: Class tombol expand (btn-link vs btn-sm btn-outline-secondary)
- CD-03: Format inline join (', ' vs ' • ')
- CD-04: Implementasi debounce (setTimeout/clearTimeout vs requestAnimationFrame)
- CD-05: Header panel — label "Peserta Terpilih" + badge atau badge standalone
- CD-06: Resolusi scope updateSelectedCount dari Proton IIFE
- CD-07: aria-live="polite" untuk screen reader

---

## Deferred Ideas

Captured in CONTEXT.md `<deferred>` section. 10 items tracked, including:
- Different empty wording per mode (peserta vs coachee)
- Chips/pills format atau two-column
- Email/Section di panel display
- Extract JS ke wwwroot/js/wizardParticipants.js
- EditAssessment.cshtml panel similar
- 1 reviewed-not-folded todo: `realtime-assessment.md` (no overlap WIZ-01)
