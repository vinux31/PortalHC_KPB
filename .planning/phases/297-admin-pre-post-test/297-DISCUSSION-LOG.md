# Phase 297: Admin Pre-Post Test - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 297-admin-pre-post-test
**Areas discussed:** Create Assessment flow, Monitoring display, Cascade & delete behavior, Sertifikat & TrainingRecord, Renewal Pre-Post Test, Data model & linking strategy, Status lifecycle Pre-Post, Edge cases (Edit, Peserta, Grouping)

---

## Create Assessment Flow

| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown AssessmentType | Tambah dropdown: Standard vs Pre-Post Test | ✓ |
| Radio button | Radio horizontal, lebih visible tapi makan space | |
| Claude decides | Claude pilih pendekatan cocok | |

**User's choice:** Dropdown AssessmentType
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Dual-section expand | 2 section Pre/Post masing-masing punya Schedule + DurationMinutes | ✓ |
| Tab Pre/Post | Tab switcher di dalam form | |
| Claude decides | — | |

**User's choice:** Dual-section expand

| Option | Description | Selected |
|--------|-------------|----------|
| Copy saat submit | Checkbox = copy paket di backend saat submit | |
| Copy realtime di UI | Checkbox = UI mirror paket Post sama dengan Pre | ✓ |
| Claude decides | — | |

**User's choice:** Copy realtime di UI

| Option | Description | Selected |
|--------|-------------|----------|
| ManagePackages + copy button | Paket dikelola di ManagePackages, Post punya tombol Copy dari Pre | ✓ |
| Saat CreateAssessment | Assign paket di form create | |
| Full inline di CreateAssessment | Embed ManagePackages di form | |

**User's choice:** ManagePackages + copy button
**Notes:** User awalnya ingin assign saat CreateAssessment, tapi setelah melihat flow ManagePackages existing, setuju pakai flow terpisah + copy button

| Option | Description | Selected |
|--------|-------------|----------|
| Peserta sama | HC pilih sekali, otomatis Pre+Post | ✓ |
| Peserta bisa beda | — | |

**User's choice:** Peserta sama

| Option | Description | Selected |
|--------|-------------|----------|
| Semua shared kecuali jadwal/durasi | Title/Category/Pass% shared, Schedule/Duration per Pre/Post, Cert hanya Post | ✓ |
| PassPercentage juga per Pre/Post | — | |
| Claude decides | — | |

**User's choice:** Semua shared kecuali jadwal/durasi, GenerateCert/ValidUntil hanya Post
**Notes:** User minta penjelasan detail field form existing sebelum memutuskan

---

## Monitoring Display

| Option | Description | Selected |
|--------|-------------|----------|
| 1 grup parent expandable | 1 baris dengan badge Pre-Post, expand 2 sub-row | ✓ |
| 2 grup terpisah dengan link | — | |
| Claude decides | — | |

**User's choice:** 1 grup parent expandable

| Option | Description | Selected |
|--------|-------------|----------|
| Stat gabungan Pre+Post | Total peserta, completed (Post), passed (Post) | ✓ |
| Stat Post saja | — | |
| Claude decides | — | |

**User's choice:** Stat gabungan Pre+Post

| Option | Description | Selected |
|--------|-------------|----------|
| Link ke detail per phase | Sub-row link ke AssessmentMonitoringDetail existing | ✓ |
| Detail gabungan baru | Halaman baru Pre vs Post side-by-side | |
| Claude decides | — | |

**User's choice:** Link ke detail per phase

| Option | Description | Selected |
|--------|-------------|----------|
| 1 card dengan badge Pre-Post | Di ManageAssessment, 1 entry dengan badge | ✓ |
| 2 card terpisah | — | |
| Claude decides | — | |

**User's choice:** 1 card dengan badge Pre-Post

| Option | Description | Selected |
|--------|-------------|----------|
| Tab Pre/Post di dalam edit | EditAssessment Tab Pre dan Post | ✓ |
| Halaman edit terpisah | — | |
| Claude decides | — | |

**User's choice:** Tab Pre/Post di dalam edit

| Option | Description | Selected |
|--------|-------------|----------|
| Per-phase | Aksi bulk di sub-row Pre/Post | ✓ |
| Per-grup dengan konfirmasi | — | |
| Claude decides | — | |

**User's choice:** Per-phase

---

## Cascade & Delete

| Option | Description | Selected |
|--------|-------------|----------|
| Reset Post juga | Reset Pre cascade ke Post | |
| Reset Pre saja, Post tetap | Hanya Pre di-reset | ✓ |
| Claude decides | — | |

**User's choice:** Reset Pre saja, Post tetap

| Option | Description | Selected |
|--------|-------------|----------|
| Izinkan reset Pre tanpa pengecekan | — | |
| Warning jika Post completed | — | |
| Block reset jika Post completed | HC harus reset Post dulu | ✓ |

**User's choice:** Block reset jika Post completed

| Option | Description | Selected |
|--------|-------------|----------|
| Hapus kedua session + semua data | Delete grup = cascade delete semua | ✓ |
| Soft delete | — | |
| Claude decides | — | |

**User's choice:** Hapus kedua session + semua data

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak diizinkan | Hapus harus per-grup | ✓ |
| Diizinkan dengan warning | — | |
| Claude decides | — | |

**User's choice:** Tidak diizinkan — delete individual diblokir untuk Pre-Post pair

---

## Sertifikat & TrainingRecord

**User's choice:** Pre GenerateCertificate=false, Post mengikuti pilihan HC. NomorSertifikat di-generate saat Post lulus (flow existing). Zero perubahan pada flow sertifikat.
**Notes:** User minta analisa dulu — setelah penjelasan flow NomorSertifikat existing (generate di SubmitExam/AkhiriUjian, bukan CreateAssessment), setuju

**User's choice:** TrainingRecord hanya dari Post-Test (otomatis karena Pre GenerateCertificate=false)
**Notes:** User minta penjelasan TrainingRecord model. Setelah penjelasan bahwa TrainingRecord 1 model (bukan per-tipe), setuju

---

## Renewal Pre-Post Test

| Option | Description | Selected |
|--------|-------------|----------|
| HC pilih tipe | Bebas Standard atau Pre-Post | ✓ |
| Otomatis Pre-Post lagi | — | |
| Claude decides | — | |

**User's choice:** HC pilih tipe

| Option | Description | Selected |
|--------|-------------|----------|
| Point ke Post session asal | RenewsSessionId = Post lama | ✓ |
| Point ke grup | — | |
| Claude decides | — | |

**User's choice:** Point ke Post session asal

---

## Data Model & Linking

| Option | Description | Selected |
|--------|-------------|----------|
| Keduanya (LinkedGroupId + LinkedSessionId) | GroupId untuk batch, SessionId untuk pair | ✓ |
| LinkedSessionId saja | — | |
| LinkedGroupId saja | — | |
| Claude decides | — | |

**User's choice:** Keduanya
**Notes:** User minta analisa lengkap. Setelah perbandingan 3 opsi, setuju keduanya untuk query efficiency

| Option | Description | Selected |
|--------|-------------|----------|
| 2 session per peserta | 1 Pre + 1 Post per user | ✓ |
| 1 session per peserta + flag | — | |

**User's choice:** 2 session per peserta

---

## Status Lifecycle

| Option | Description | Selected |
|--------|-------------|----------|
| Derived dari status Pre+Post | Dihitung dinamis, bukan disimpan | ✓ |
| Field status di LinkedGroup | — | |
| Claude decides | — | |

**User's choice:** Derived

| Option | Description | Selected |
|--------|-------------|----------|
| Status Pre ikuti lifecycle standard | Upcoming/Open/InProgress/Completed/Cancelled | ✓ |
| Pre selalu Open dari awal | — | |

**User's choice:** Status Pre ikuti lifecycle standard

---

## Edge Cases

| Option | Description | Selected |
|--------|-------------|----------|
| Enforce Pre < Post | Backend + frontend validasi jadwal | ✓ |
| Tidak perlu validasi | — | |
| Claude decides | — | |

**User's choice:** Ya, enforce Pre < Post

| Option | Description | Selected |
|--------|-------------|----------|
| Otomatis sinkron | Tambah/hapus peserta = otomatis Pre+Post | ✓ |
| Manual per-phase | — | |
| Claude decides | — | |

**User's choice:** Otomatis sinkron. Validasi: block hapus jika Pre/Post InProgress/Completed

| Option | Description | Selected |
|--------|-------------|----------|
| Dual strategy | Standard = existing grouping, Pre-Post = LinkedGroupId | ✓ |
| Semua pakai LinkedGroupId | — | |
| Claude decides | — | |

**User's choice:** Dual strategy (backward compatible)

---

## Claude's Discretion

- LinkedGroupId value strategy
- Exact UI layout dual-section expand
- Tab styling di EditAssessment
- Badge visual design Pre-Post
- Copy paket soal implementation detail

## Deferred Ideas

- Detail gabungan Pre vs Post side-by-side per peserta — Phase 299
- AssessmentPhase multi-tahap — future
