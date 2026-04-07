---
phase: 299
reviewers: [opencode]
reviewed_at: 2026-04-07
plans_reviewed: [299-01-PLAN.md, 299-02-PLAN.md]
---

# Cross-AI Plan Review — Phase 299

## OpenCode Review

### Plan 299-01 (Backend)

**Summary:** Plan 299-01 memodifikasi controller untuk mendukung grouping Pre-Post pairs dan perhitungan gain score. Secara umum solid dengan pertimbangan security (IDOR prevention), namun ada beberapa edge case yang belum tercakup dan potensi performance concern.

**Strengths:**
- Security-aware dengan IDOR prevention (T-299-01)
- Gain score formula handling untuk edge case PreScore = 100
- Menggunakan ViewBag untuk passing data (sesuai pattern existing)
- Edge case Pre Completed + Post active di-handle dengan separate query
- Threat model sudah diidentifikasi

**Concerns:**
- **MEDIUM**: Query performance — separate query untuk edge case Pre Completed + Post active bisa menyebabkan N+1 problem jika banyak paired assessments. Pertimbangkan single query dengan UNION atau CTE
- **MEDIUM**: Missing validation — apa yang terjadi jika LinkedSessionId null atau tidak ditemukan? Perlu null-check dan fallback behavior
- **LOW**: Gain score edge case PreScore = 0 — formula (postScore - 0) / (100 - 0) * 100 akan selalu menghasilkan postScore. Apakah ini intended? Harus didokumentasikan
- **MEDIUM**: Concurrent modification — jika essay di-grade setelah user membuka halaman comparison, data stale. Perlu refresh mechanism?
- **HIGH**: Missing database schema info — tidak ada penjelasan tentang field LinkedGroupId dan LinkedSessionId. Apakah sudah ada? Perlu migration?

**Suggestions:**
- Combine queries menggunakan LEFT JOIN untuk Pre-Post pairs
- Tambahkan null-check untuk LinkedSessionId sebelum query Pre session
- Dokumentasikan behavior untuk edge case PreScore = 0
- Pertimbangkan caching untuk Pre session scores jika user me-refresh berkali-kali
- Clarify database schema changes atau konfirmasi bahwa LinkedGroupId sudah ada dari Phase 297

**Risk Assessment: MEDIUM**

---

### Plan 299-02 (Frontend)

**Summary:** Plan 299-02 mengimplementasikan UI untuk Pre-Post card pairing dan comparison table. Pendekatan modular dengan human checkpoint adalah keputusan yang baik. Namun ada concern tentang breaking existing functionality.

**Strengths:**
- Human checkpoint untuk UI verification
- Reuses existing filterCards() function — minimal code duplication
- Card pair visual linking dengan arrow icon
- Gain score color coding untuk quick visual feedback
- Essay pending state ditangani

**Concerns:**
- **HIGH**: "Inner cards don't have class assessment-card" — bisa BREAK existing functionality jika ada JavaScript yang bergantung pada class ini untuk event binding atau filtering. Perlu audit existing JS dependencies
- **MEDIUM**: Tab filtering "pair follows Post status" — Bagaimana jika Pre dan Post memiliki status yang tidak sesuai dengan simple logic?
- **MEDIUM**: Mobile responsiveness — Card pair dengan arrow bisa break di mobile viewport
- **LOW**: Accessibility — Paired cards dengan arrow icon mungkin tidak obvious untuk screen reader users
- **MEDIUM**: D-05 "Pre expired → Post blocked" — detail UI state perlu diperjelas

**Suggestions:**
- **CRITICAL**: Audit existing JavaScript dependencies pada class assessment-card sebelum mengubah structure
- Tambahkan responsive CSS untuk card pairs
- Implement ARIA labels untuk paired cards
- Buat decision matrix untuk tab filtering state combinations

**Risk Assessment: MEDIUM-HIGH**

---

## Consensus Summary

### Agreed Strengths
- Security-aware design (IDOR prevention via userId check)
- Good wave dependency structure (backend first, then frontend)
- Human checkpoint for UI verification
- Edge case handling (PreScore=100, Essay pending)

### Agreed Concerns
1. **HIGH**: Database schema fields (LinkedGroupId, LinkedSessionId) — perlu konfirmasi sudah ada dari Phase 296/297 (CATATAN: sudah ada sejak Phase 296, terverifikasi di RESEARCH.md)
2. **HIGH**: assessment-card class dependency — inner pair cards menghapus class ini, perlu audit JS event binding
3. **MEDIUM**: Query performance — multiple queries untuk edge cases, pertimbangkan optimization
4. **MEDIUM**: Mobile responsiveness untuk card pair layout belum dibahas detail
5. **LOW**: Accessibility (ARIA labels) belum tercakup di plan

### Divergent Views
- Tidak ada divergent views (hanya satu reviewer yang berhasil diinvoke)

### Notes
- Gemini CLI gagal (exit code 41)
- Codex CLI timeout setelah 5+ menit
- Database schema concern dari reviewer sudah terjawab — fields ada sejak Phase 296 (diverifikasi di RESEARCH.md dan CONTEXT.md refs ke Phase 296)
