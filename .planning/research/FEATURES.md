# Feature Research

**Domain:** Audit-fix milestone — UX, perf, dan defensive fixes pada flow assessment HR portal
**Researched:** 2026-04-28
**Confidence:** HIGH untuk T1, T2, T4–T7, T9–T11 (pola industri sangat mapan); MEDIUM untuk T3 (kontekstual, butuh baseline measurement)

## Industry Pattern per Temuan

### T1 — Show Password Toggle

**Sumber referensi:** Material Design 3, Apple HIG, NIST 800-63B (modern password guidance), WAI-ARIA Authoring Practices "Toggle button".

**Table stakes (industri standard):**
- Eye icon (terbuka/tertutup) di kanan input field, dalam input-group
- `aria-label` dinamis ("Show password" / "Hide password")
- Keyboard accessible (Tab + Space/Enter trigger)
- Tidak men-submit form

**Differentiators (nice-to-have, skip untuk audit fix):**
- Auto-revert ke masked setelah X detik visibility (security)
- Unmask hanya saat tombol di-press (PreventEventDefault on mouseup)
- Capslock indicator
- Strength meter

**Complexity:** LOW (~10 LOC inline script + 1 button)

---

### T2 — Score Editable per Question Type

**Sumber referensi:** Moodle Quiz module, Canvas Quizzes, Google Forms.

**Discovery dari arsitektur:** Server-side `AssessmentAdminController.CreateQuestion` baris 4681 dan `EditQuestion` baris 4822 secara eksplisit memaksa `scoreValue = 10` untuk MC/MA. **Fix view-only TIDAK CUKUP** — server-side wajib di-relax menjadi clamp 1–100.

**Table stakes:**
- Editable di semua tipe soal (MC/MA/Essay) dengan range 1–100
- Per-question score override
- Total package score display (sum of all question scores)

**Differentiators:**
- Bobot relatif (% dari total) opsional
- Bulk-set score untuk semua soal sekaligus
- Per-option partial credit (untuk MA all-or-nothing toleransi)

**Anti-pattern (yang harus DIHILANGKAN):**
- Fixed score per question type — bukan pola industri (Moodle/Canvas semua editable)

**Complexity:** LOW-MEDIUM (view + 2 server-side endpoints)

---

### T3 — Performance Optimization Heavy Admin Page

**Sumber referensi:** EF Core 8 Best Practices (Microsoft Learn), DataTables.js server-side processing, Stack Overflow "ASP.NET Core slow query" patterns.

**Table stakes:**
- `AsNoTracking()` pada read-only path
- Composite index pada filter columns (Schedule, ExamWindowCloseDate, LinkedGroupId)
- Server-side pagination di DB level (SKIP/TAKE)
- Projection ke DTO/anonymous type (bukan full entity)
- Cache untuk dropdown reference data

**Differentiators:**
- Server-side DataTable.js (sortable + searchable + paginated by AJAX)
- Virtual scrolling untuk list panjang
- Real-time progress indicators

**Complexity:** MEDIUM (perlu measurement baseline + EF migration untuk index)

**Dependencies:** Existing `PaginationHelper` static class.

---

### T4 — Selected Items Inline Display (Peserta)

**Sumber referensi:** Material Chips, Ant Design Tags, Bootstrap Select2.

**Table stakes:**
- Live count badge (`X peserta dipilih`)
- Real-time list update saat checkbox toggle
- Truncated display dengan expand button (5 nama + "...dan N lainnya")
- DRY: extract function untuk reuse Step 2 + Step 4

**Differentiators:**
- Removable chips per peserta
- Filter peserta yang sudah dipilih
- Sticky panel saat scroll list

**Complexity:** LOW (extract function + 1 panel markup)

---

### T5 / T6 — Timezone Labeling Consistency

**Sumber referensi:** Aplikasi Indonesia multi-region (BCA, Tokopedia, Garuda) yang konsisten label "(WIB)" pada semua input waktu.

**Table stakes:**
- Label `(WIB)` eksplisit di setiap input waktu
- Konsistensi di summary/rangkuman (jam mulai DAN jam tutup)
- Tooltip/helper text jika perlu klarifikasi tambahan

**Differentiators (out of scope):**
- Multi-timezone display (WIB/WITA/WIT) dengan auto-detect lokasi user
- DST handling
- UTC offset display

**Complexity:** LOW (label string saja, ~6 lokasi label + 1 JS template)

---

### T7 — MC vs MA Naming Clarity

**Sumber referensi:** Moodle "Multiple Choice (Single answer)" vs "Multiple Choice (Multiple answers)", Canvas "Multiple Choice" vs "Multiple Answers", AKM Kemendikbud Indonesia.

**Table stakes:**
- Label explicit "1 jawaban benar" vs "≥ 2 jawaban benar"
- Konsisten di semua user-facing text (form input, preview, exam, results)
- Internal enum/string DB **tidak diubah** (backward-compat)

**Differentiators:**
- Helper text/tooltip dengan contoh
- Icon visual berbeda (radio vs checkbox)

**Complexity:** LOW (4 view files dengan label change saja)

**Cross-cutting concern:** Documentation, training material, dan E2E tests perlu update bersamaan agar tidak drift.

---

### T8 — DEFERRED (klarifikasi user)

Skip riset detail sampai user konfirmasi Jalur A (label fix) atau Jalur B (field baru).

---

### T9 — Idempotent State Transition (Finalize Grading)

**Sumber referensi:** Stripe Idempotency Keys pattern, Moodle quiz state machine, REST API idempotent operations (HTTP RFC 9110).

**Discovery dari arsitektur:** Replay guard sudah ada via `ExecuteUpdateAsync` dengan `WHERE Status == "Menunggu Penilaian"` (baris 2778–2784, komentar T-298-16). Fix hanya butuh ganti pesan error baris 2713 menjadi success/no-op message ramah jika status sudah Completed.

**Table stakes:**
- Guard check WHERE clause (sudah ada)
- Friendly UI message saat status sudah final (bukan error)
- Sembunyikan tombol action yang tidak relevan jika status terminal
- Idempotent log/notification (NotifyIfGroupCompleted dengan dedupe)

**Differentiators:**
- Idempotency key dari client (Stripe-style)
- State machine visualization untuk admin

**Complexity:** LOW-MEDIUM (1 message change + UI button hide condition + audit log dedupe)

---

### T10 — Defensive Error Handling Certificate Generation

**Sumber referensi:** OWASP defensive coding guide, Microsoft Learn structured logging, Polly resilience.

**Table stakes:**
- Try-catch per-action dengan structured logging (`_logger.LogError(ex, "Context", id)`)
- Specific exception catches (DbException, FormatException, NullReferenceException)
- Null-safe accessor di view (`Model.User?.FullName ?? "..."`)
- Friendly user-facing message via TempData + redirect (mirror `CertificatePdf` pattern baris 2078–2083)

**Differentiators:**
- Telemetry / Application Insights integration
- Retry with backoff untuk transient errors

**Complexity:** LOW (mirror existing pattern dari sibling action)

**Anti-pattern:** Generic `catch (Exception)` tanpa logging — hides root cause.

---

### T11 — Multi-Step Form Validation dengan Hidden Required Field

**Sumber referensi:** ASP.NET Core unobtrusive validation docs, Bootstrap wizard patterns, Ant Design Form, Formik conditional fields.

**Table stakes:**
- JS handler set value programmatically saat field disembunyikan (`element.value = 'Upcoming'`)
- Server-side `ModelState.Remove("Status")` jika mode-specific (defensive backup)
- Re-parse jQuery validate setelah dynamic show/hide
- Test matrix: switching mode (Standard ↔ PrePost) tidak meninggalkan stale validation state

**Differentiators:**
- FluentValidation rule conditional
- Dedicated ViewModel per mode dengan IValidatableObject

**Complexity:** MEDIUM (perlu test 4 kombinasi: Standard, S→PP→S, PP, PP→S→PP)

**Pattern existing yang diikuti:** `ModelState.Remove()` sudah dipakai 5+ kali di POST `CreateAssessment` (baris 742, 756, 821, 835, 870).

## Phase Batching Recommendation (untuk Roadmapper)

| Wave | Ukuran | Temuan | Karakteristik |
|------|--------|--------|---------------|
| 1 | UI label only | T1, T5, T6, T7 | Low risk, label strings + 1 inline script |
| 2 | UI behavior | T2, T4, T11 | Medium, view + 1-2 controller method |
| 3 | Defensive + state | T9, T10 | High care, butuh integration test + post-deploy log analysis |
| 4 | Performance | T3 | Measure-first, baseline → patch → re-measure |
| 5 | Deferred | T8 | Tracked di STATE.md dengan due date |

## Cross-Cutting Backlog (Future Milestones)

Patterns yang muncul dari riset, layak dijadikan milestone tersendiri di v16.0+:

1. **Accessibility audit menyeluruh** — lanjutan T1, T4 ke seluruh form portal
2. **Idempotency pattern** — terapkan ke Reset/ForceClose actions yang serupa
3. **Defensive programming pattern** — semua complex read-actions (renewal chain, certificate, exam result)
4. **Multi-step validation review** — seluruh wizard di portal (CreateAssessment, AddTraining, ProtonAssessment)

## Sources

- Material Design 3 — text-field component spec
- WAI-ARIA Authoring Practices 1.2 — Toggle button pattern
- NIST 800-63B — Digital Identity Guidelines (password rules)
- Moodle Quiz module documentation
- Canvas LMS Quiz API documentation
- AKM Kemendikbud — pola pelabelan tipe soal Indonesia
- Microsoft Learn — EF Core 8 best practices
- Stripe API documentation — Idempotency Keys pattern
- ASP.NET Core docs — unobtrusive client-side validation

---
*Feature research for: v15.0 Audit Findings 27 April 2026*
*Researched: 2026-04-28*
