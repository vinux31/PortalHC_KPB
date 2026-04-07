# Phase 302: Accessibility WCAG Quick Wins - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 302-accessibility-wcag-quick-wins
**Areas discussed:** Skip link & focus management, Keyboard navigation, Screen reader & ARIA, Font size & extra time, Scope halaman, Extra time UI detail, Keyboard shortcut konflik, Testing & validasi

---

## Skip link & focus management

| Option | Description | Selected |
|--------|-------------|----------|
| Hidden until Tab | Link tersembunyi, muncul saat Tab pertama kali | ✓ |
| Always visible | Link selalu terlihat di atas halaman | |

**User's choice:** Hidden until Tab
**Notes:** Pattern standar WCAG

### Auto-focus saat pindah halaman

| Option | Description | Selected |
|--------|-------------|----------|
| Soal pertama di halaman | Focus langsung ke card soal pertama | ✓ |
| Header halaman/nomor page | Focus ke heading "Halaman X" | |

**User's choice:** Soal pertama di halaman

### Skip link target

| Option | Description | Selected |
|--------|-------------|----------|
| Area soal langsung | Lompat ke container soal | ✓ |
| Timer dulu, lalu soal | Skip ke timer lalu Tab ke soal | |

**User's choice:** Area soal langsung
**Notes:** User meminta penjelasan dulu soal skip link target, setelah dijelaskan memilih area soal

---

## Keyboard navigation

| Option | Description | Selected |
|--------|-------------|----------|
| Arrow keys | Arrow Up/Down untuk opsi, Space/Enter untuk pilih | ✓ |
| Tab antar opsi | Tab untuk pindah antar opsi | |

**User's choice:** Arrow keys (native radio/checkbox behavior)

### Antar soal

| Option | Description | Selected |
|--------|-------------|----------|
| Tab ke soal berikutnya | Natural flow via Tab | ✓ |
| Shortcut khusus (Alt+N/P) | Keyboard shortcut | |

**User's choice:** Tab ke soal berikutnya

### Sticky footer keyboard

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, masuk Tab order | Buttons ikut tab order natural | ✓ |
| Shortcut keyboard saja | Diakses via shortcut | |

**User's choice:** Masuk Tab order

---

## Screen reader & ARIA

**User's choice:** DIHAPUS DARI SCOPE
**Notes:** User meminta penjelasan fungsi screen reader, lalu memutuskan untuk menghapus dari scope phase ini

---

## Font size & extra time

### Font size control
**User's choice:** DIHAPUS DARI SCOPE

### Extra time — lokasi setting

| Option | Description | Selected |
|--------|-------------|----------|
| Di EditAssessment per peserta | Set sebelum ujian dimulai | |
| Di AssessmentMonitoring | Set dari monitoring saat berlangsung | ✓ |

**User's choice:** Di AssessmentMonitoring
**Notes:** Berlaku untuk SEMUA worker di assessment (bukan per individu). Yang sudah submit tidak berlaku. Tombol buka modal dengan input waktu.

### Extra time — range

| Option | Description | Selected |
|--------|-------------|----------|
| 5-120 menit, kelipatan 5 | Dropdown atau number input | ✓ |
| Persentase dari durasi asli | 25%, 50%, 100% tambahan | |

**User's choice:** 5-120 menit, kelipatan 5

### Extra time — live update

| Option | Description | Selected |
|--------|-------------|----------|
| Reload otomatis via SignalR | Timer langsung bertambah real-time | ✓ |
| Efek saat refresh halaman | Berlaku saat peserta refresh | |

**User's choice:** SignalR real-time update

---

## Scope halaman

| Option | Description | Selected |
|--------|-------------|----------|
| StartExam saja | Fokus halaman ujian utama | ✓ |
| StartExam + ExamSummary | Termasuk review page | |
| Semua halaman CMP worker | Scope lebih besar | |

**User's choice:** StartExam saja

---

## Extra time UI detail

**User's choice:** Tombol di monitoring (bukan per peserta), buka modal, berlaku semua peserta belum submit

---

## Keyboard shortcut konflik

**User's choice:** Minta audit dulu
**Notes:** Audit dilakukan — anti-copy hanya block Ctrl+C/A/U/S/P, tidak konflik dengan Tab/Arrow/Enter/Space

---

## Testing & validasi

| Option | Description | Selected |
|--------|-------------|----------|
| Manual testing saja | Tab through, test keyboard nav | ✓ |
| Automated (axe-core) | Integrate axe-core | |

**User's choice:** Manual testing saja

---

## Extra time scope klarifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Per assessment (semua peserta) | Satu tombol, berlaku semua | ✓ |
| Per sesi (per peserta) | Per individu, field di AssessmentSession | |

**User's choice:** Per assessment (semua peserta)

## Skip link scope klarifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Di _Layout.cshtml (semua halaman) | Global | |
| Di StartExam saja | Minimal scope | ✓ |

**User's choice:** Di StartExam saja

---

## Deferred Ideas

- Screen reader / aria-live support (A11Y-03) — phase terpisah
- Font size control A+/A- (A11Y-04) — phase terpisah
- Skip link global di _Layout.cshtml
- Automated accessibility testing (axe-core)
