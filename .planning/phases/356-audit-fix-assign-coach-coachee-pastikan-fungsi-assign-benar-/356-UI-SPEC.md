---
phase: 356
slug: audit-fix-assign-coach-coachee
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-09
scope: thin-audit-fix
---

# Phase 356 — UI Design Contract (SCOPED TIPIS)

> Kontrak interaksi & microcopy untuk **audit-fix backend** Assign Coach×Coachee.
> Ini BUKAN fase fitur frontend. Tidak ada surface visual baru, tidak ada halaman/komponen baru, tidak ada pekerjaan design-system.
> Hanya **2 kontrak sempit** yang dikunci di sini: (1) AF-2 UI guard "1 unit per batch" di modal assign existing, dan (2) AF-5 microcopy notifikasi reassign.
> Prinsip pemandu: **cocokkan pola modal/notifikasi existing persis verbatim; kunci HANYA perilaku interaksi baru + microcopy.**

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none — ASP.NET Core MVC server-rendered (Razor `.cshtml`) |
| Preset | not applicable |
| Component library | Bootstrap 5.3 (existing app-wide via `_Layout.cshtml`) — REUSE verbatim, jangan tambah |
| Icon library | Bootstrap Icons + Font Awesome 6.5 (existing) — tidak dipakai di fase ini |
| Font | Inter (Google Fonts, existing) — tidak diubah |

**Catatan stack:** Vanilla JS event handlers (`updateAssignmentDefaults()`, `filterCoacheesBySection()`, `submitAssign()`). Org labels via `@OrgLabels.GetLabel(N)`. **TIDAK ADA** JS framework / CSS-in-JS / Tailwind. shadcn gate: N/A (bukan React/Next.js/Vite).

---

## Spacing Scale

**N/A — out of scope** (backend audit-fix). Reuse spacing Bootstrap 5 + konvensi modal existing verbatim (`mb-3`, `ms-1`, `padding:0.5rem`). Tidak mendeklarasikan skala token baru.

---

## Typography

**N/A — out of scope** (backend audit-fix). Reuse tipografi existing modal verbatim (`form-label fw-semibold`, `.text-muted small`, `.form-text`). Tidak mendeklarasikan ukuran/weight baru.

---

## Color

**N/A — out of scope** (backend audit-fix). Reuse warna semantik Bootstrap existing verbatim:
- `text-danger` — tanda wajib `*` (existing)
- `badge bg-light text-dark border` — badge seksi coachee (existing)
- `form-text text-success` — hint sukses (pola `#coachSuggestHint`, existing)
- `text-muted` — state disabled/de-emphasized & empty state (existing)

Tidak ada token warna baru, tidak ada split 60/30/10 yang dideklarasikan (bukan fase visual).

---

## Interaction Contract — AF-2: UI Guard "1 Unit per Batch"

**Lokasi:** `Views/Admin/CoachCoacheeMapping.cshtml` — blok `#coacheeChecklist` (~L407-433), assign form (~L397-465).
**Sumber keputusan:** CONTEXT.md D-07 (Opsi A, LOCKED) + spec AF-2 §73-82.
**Backend `AutoCreateProgressForAssignment` TIDAK diubah** — guard murni di sisi UI agar semantik `AssignmentUnit` eksplisit tetap utuh.

### Tujuan
Cegah satu batch assign mencampur coachee dari **lebih dari satu unit**. Cross-unit batch menyebabkan satu `AssignmentUnit` tunggal diterapkan ke semua → deliverable unit salah. Hasil yang diinginkan: **satu batch = tepat satu unit**.

### Aset existing yang dipakai (REUSE — jangan invent)
- Tiap baris: `<div class="form-check coachee-item" data-section="@coachee.Section" data-unit="@coachee.Unit">`.
- Checkbox: `<input class="form-check-input coachee-checkbox" ... onchange="updateAssignmentDefaults()">`.
- `updateAssignmentDefaults()` SUDAH membangun `units` Set (L686-710) dan punya cabang "Multiple different units or no match" — ini hook alami untuk guard.
- Hint sukses existing `#coachSuggestHint` = `<div class="form-text text-success">` — gunakan **gaya yang sama** untuk hint constraint baru (boleh kelas warna berbeda, lihat di bawah).

### Mekanisme (kontrak, bukan implementasi paksa)
Saat coachee **pertama** dicentang, tentukan unitnya dari `data-unit`. Lalu:
- Setiap `.coachee-checkbox` pada `.coachee-item` dengan `data-unit` **berbeda** → set `disabled = true`.
- Saat **semua** centang dilepas (selection clear) → hapus `disabled` dari semua checkbox (reset).
- Item yang sedang ter-filter sembunyi via `filterCoacheesBySection()` (`display:none`) tetap dihormati — guard hanya menyentuh atribut `disabled`, bukan `display`.
- Hint constraint ditampilkan saat ≥1 dicentang, disembunyikan saat selection kosong.

> Discretion executor (CONTEXT.md): mekanisme persis (disable checkbox vs validasi submit vs filter) boleh dipilih **asal hasil = 1 unit/batch**. Kontrak ini mengunci **opsi disable-checkbox** sebagai default karena paling jelas secara visual dan paling dekat dengan hook `updateAssignmentDefaults()` yang sudah ada. Backstop submit-time (cek `units.size === 1` di `submitAssign()` sebelum `fetch`) opsional tapi direkomendasikan sebagai sabuk-pengaman.

### Appearance disabled state (REUSE Bootstrap verbatim — jangan token baru)
- Checkbox: state native `.form-check-input:disabled` Bootstrap 5 (opacity berkurang, kursor `not-allowed`) — **tanpa CSS baru**.
- Label: redupkan dengan kelas existing `text-muted` pada `.coachee-item` ter-disable (mis. toggle class `text-muted` di `.coachee-item` yang lain-unit). Cukup utility class existing; jangan tulis warna hex baru.
- Tidak ada animasi, tidak ada warna kustom, tidak ada ikon tambahan.

### Hint / help text (Bahasa Indonesia) — gaya `form-text` existing
Tampilkan di bawah `#coacheeChecklist` (sejajar pola `#coachSuggestHint`), `<div class="form-text text-muted">`:

> **`Satu batch assign hanya untuk satu unit. Coachee dari unit lain dinonaktifkan — kosongkan pilihan untuk berganti unit.`**

(Gunakan `text-muted` agar dibaca sebagai constraint informatif, bukan sukses/error. Boleh diganti `text-info` bila konsisten dengan hint informatif lain di app — discretion executor.)

### States (lengkap)
| State | Kondisi | Perilaku UI |
|-------|---------|-------------|
| Default | 0 coachee dicentang | Semua checkbox enabled; hint constraint disembunyikan. |
| One-unit-selected | ≥1 dicentang, semua satu unit X | Checkbox unit ≠ X disabled + label `text-muted`; hint constraint tampil; auto-fill `AssignmentSection`/`AssignmentUnit` jalan seperti existing (L697-708). |
| Selection-cleared (reset) | centang terakhir dilepas | Semua checkbox re-enabled; hint disembunyikan; perilaku auto-fill kembali ke baseline. |
| Empty / edge | `!eligibleCoachees.Any()` | **Sudah ditangani** existing (L428-431: `<p class="text-muted small mb-0">Tidak ada coachee yang tersedia (semua sudah memiliki coach aktif).</p>`). **Jangan redesign** — guard tidak berlaku (tak ada checkbox). |

---

## Copywriting Contract — AF-5: Microcopy Notifikasi Reassign

**Lokasi:** `Controllers/CoachMappingController.cs` — `ApproveReassignSuggestion` (~L1614-1638).
**Sumber keputusan:** CONTEXT.md D-08 + spec AF-5 §114-120.
**COPYWRITING ONLY** — memakai sistem notifikasi existing (`_notificationService.SendAsync`), tanpa layout visual.

### Pola existing yang ditiru (verbatim signature)
`await _notificationService.SendAsync(userId, "EVENT_TYPE", "Judul", "Pesan singkat", "/CDP/CoachingProton");`
Dibungkus `try { ... } catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }` (warn-only, tidak throw — pola Assign L645, Edit L812-819, Deactivate L957-964).

### Microcopy 3 recipient (Bahasa Indonesia)
| Recipient | EVENT_TYPE | Judul | Pesan (body) |
|-----------|-----------|-------|--------------|
| Coach **lama** (dilepas) | `COACH_REASSIGNED` | `Penugasan Coaching Dialihkan` | `Penugasan coaching Anda dengan {coacheeName} telah dialihkan ke coach lain.` |
| Coach **baru** (ditunjuk) | `COACH_REASSIGNED` | `Coach Ditunjuk` | `Anda ditunjuk sebagai coach untuk {coacheeName}.` |
| **Coachee** (dipindah) | `COACH_REASSIGNED` | `Coach Anda Berubah` | `Coach Anda telah diganti menjadi {coachName}.` |

**Catatan tone:** mengikuti gaya kalimat existing — kalimat tunggal, sopan, subjek "Anda", URL `/CDP/CoachingProton`. `{coacheeName}`/`{coachName}` = placeholder nama lengkap (resolve dari mapping, seperti pola Edit/Deactivate). EVENT_TYPE baru `COACH_REASSIGNED` konsisten dengan konvensi UPPER_SNAKE existing (`COACH_ASSIGNED`, `COACH_MAPPING_EDITED`, `COACH_MAPPING_DEACTIVATED`).

> Discretion executor (CONTEXT.md): wording persis boleh disesuaikan asal selaras pola COACH-02. Judul "Coach Ditunjuk" sengaja identik dengan notif Assign existing (L646) agar coach baru merasakan pengalaman yang sama dengan ditunjuk langsung.

### Microcopy lain di fase ini (di luar AF-5, dicatat agar tidak lupa)
| Item | Sumber | Copy |
|------|--------|------|
| AF-6 pesan error duplikat spesifik | CONTEXT.md D-09 (controller, bukan view) | `Coachee sudah memiliki coach aktif untuk unit ini. Nonaktifkan mapping lama terlebih dahulu.` (menggantikan generic "Gagal menyimpan assignment" hanya pada pelanggaran `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`). Discretion wording per CONTEXT.md. |

---

## Standard Copywriting Slots (template)

| Element | Copy |
|---------|------|
| Primary CTA | **N/A — out of scope.** Tombol "Simpan" modal assign existing (L468) tidak diubah. |
| Empty state heading | **N/A** — empty state coachee sudah ada (L428-431), tidak diredesign. |
| Empty state body | Existing verbatim: `Tidak ada coachee yang tersedia (semua sudah memiliki coach aktif).` |
| Error state | AF-6: `Coachee sudah memiliki coach aktif untuk unit ini. Nonaktifkan mapping lama terlebih dahulu.` (lihat tabel AF-6 di atas). |
| Destructive confirmation | **N/A — out of scope.** Tidak ada aksi destruktif baru di fase ini. (Deactivate/graduate existing memakai konfirmasi existing.) |

---

## Registry Safety

**N/A — out of scope.** Tidak ada shadcn, tidak ada registry pihak ketiga. Semua UI memakai Bootstrap 5 yang sudah ter-bundle di app. Tidak ada blok eksternal masuk kontrak.

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none | none | not applicable — no registry, no shadcn |

---

## Out-of-Scope Dimensions (ringkas)

Dimensi UI-SPEC penuh berikut **sengaja N/A** karena ini backend audit-fix; reuse Bootstrap 5 + konvensi app verbatim:
- Spacing scale / token baru — N/A
- Typography (ukuran/weight) — N/A
- Color tokens / split 60-30-10 — N/A
- Component inventory penuh — N/A (hanya menyentuh modal assign existing)
- Responsive grid / breakpoint — N/A
- Surface / halaman / komponen baru — N/A (nol)

---

## Checker Sign-Off

> Catatan untuk gsd-ui-checker: ini UI-SPEC **scoped tipis** (audit-fix). Dimensi visual/color/typography/spacing sengaja N/A dengan alasan satu baris. Validasi hanya 2 kontrak aktif: AF-2 interaction (states + disabled appearance + hint copy) dan AF-5 copywriting (3 recipient + tone match existing).

- [ ] Dimension 1 Copywriting: PASS (AF-5 3 recipient + AF-6 error + hint AF-2)
- [ ] Dimension 2 Visuals: N/A — reuse Bootstrap modal existing verbatim
- [ ] Dimension 3 Color: N/A — reuse semantik Bootstrap existing
- [ ] Dimension 4 Typography: N/A — reuse tipografi modal existing
- [ ] Dimension 5 Spacing: N/A — reuse spacing modal existing
- [ ] Dimension 6 Registry Safety: N/A — no registry / no shadcn

**Approval:** pending
