# Requirements — Milestone v32.5 Flexible Add/Remove Participant

**Milestone:** v32.5 — Flexible Add/Remove Participant
**Created:** 2026-06-19
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Goal:** Admin/HC dapat menambah & menghapus peserta pada assessment **kapan saja** — baik batch yang belum ada progres peserta maupun yang sedang berjalan (InProgress) — langsung dari layar Monitoring Detail (live, AJAX+SignalR), dengan penghapusan **hybrid by-state** (belum-mulai → hard-delete bersih; sudah-ada-data/Completed → soft-remove + arsip yang reversibel).
**Sumber:** Design spec `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md` (committed `ccdc78ef`), turunan audit kode 4-agen 2026-06-19.
**Konteks:** `AssessmentSession` = per-peserta; batch = sesi share `Title+Category+Schedule.Date`; InProgress = turunan (`StartedAt!=null && CompletedAt==null`). ADD sudah parsial via `EditAssessment` (Phase 391); DELETE 1-peserta **belum ada backend** (stub mati `DeleteAssessmentPeserta`). Branch main. **migration=TRUE** (3 kolom nullable additif). Verifikasi lokal `dotnet build` + xUnit + Playwright.

---

## v1 Requirements

### Tambah Peserta (PART) — lanjut dari v32.0

- [ ] **PART-05**: Admin/HC dapat menambah satu atau lebih peserta ke sebuah assessment langsung dari layar **Monitoring Detail** — baik batch yang belum ada progres maupun yang sedang berjalan (ada peserta `InProgress`) — dan baris peserta baru muncul **live tanpa reload**.
- [x] **PART-06**: Penambahan peserta membuat `AssessmentSession` + `UserPackageAssignment` otomatis dengan **status siap-mulai** (Open/Upcoming, bukan InProgress), **ditolak** bila window ujian (`ExamWindowCloseDate`) sudah lewat, dan **idempoten** (peserta yang sudah aktif di batch tidak terduplikasi).
- [x] **PART-07**: Penambahan peserta ke assessment **Pre/Post** membuat pasangan sesi Pre+Post untuk peserta tersebut.

### Hapus & Pulihkan Peserta (PRMV)

- [x] **PRMV-01**: Admin/HC dapat menghapus peserta dari assessment via kontrol di Monitoring Detail — peserta yang **belum mulai & tanpa data** dihapus bersih (hard-delete cascade), sedangkan peserta yang **sudah mulai/selesai/punya jawaban** di-**soft-remove** (baris + jawaban + skor + sertifikat dipertahankan/diarsip, ditandai `RemovedAt/RemovedBy/RemovalReason`).
- [x] **PRMV-02**: Menghapus peserta yang **sedang aktif** mengerjakan ujian mewajibkan **konfirmasi keras** lebih dulu, lalu peserta langsung **dikeluarkan dari layar ujian (force-kick)** via SignalR dengan pesan jelas.
- [x] **PRMV-03**: Peserta yang telah dihapus **tidak dapat melanjutkan atau mensubmit** ujian (guard di `StartExam`/`SubmitExam`/`Hub.JoinBatch`) — jawaban setelah penghapusan tidak terhitung.
- [x] **PRMV-04**: Admin/HC dapat **memulihkan (restore)** peserta yang di-soft-remove sehingga peserta kembali aktif di batch dan muncul lagi di daftar aktif.
- [x] **PRMV-05**: Menghapus peserta pada assessment **Pre/Post** memperlakukan pasangan Pre+Post **sebagai satu unit** (kedua sesi konsisten — sama-sama hard-delete bila keduanya belum-mulai, atau sama-sama soft-remove bila salah satu sudah berdata).

### Live & Integritas Tampilan (PLIV)

- [x] **PLIV-01**: Peserta yang di-soft-remove **dikecualikan dari semua daftar & perhitungan aktif** (monitoring, hasil, jumlah peserta, pass-rate, count sertifikat) dan ditampilkan terpisah di panel **"Peserta Dikeluarkan"** di Monitoring Detail.
- [x] **PLIV-02**: Penambahan & penghapusan peserta **tersiar live** ke semua Admin/HC yang sedang memantau batch via SignalR (`participantAdded`/`participantRemoved` — baris ter-inject/terhapus + ringkasan count ter-update) tanpa reload.
- [x] **PLIV-03**: Semua aksi tambah/hapus/restore **tercatat di audit** (siapa, kapan, alasan) dan hanya dapat dilakukan oleh **Admin atau HC** (RBAC + antiforgery di setiap endpoint).

---

## Future Requirements (deferred)

- **Notifikasi ke peserta yang dihapus** (email/in-app "Anda dikeluarkan dari ujian X") — di luar scope v32.5; force-kick live + pesan layar sudah cukup.
- **Bulk import peserta live via Excel ke batch berjalan** — saat ini hanya picker manual.
- **Alasan penghapusan wajib + daftar alasan terstandar** — v32.5 alasan opsional bebas-teks; standardisasi ditangguhkan.

---

## Out of Scope (eksklusi eksplisit)

| Fitur | Alasan |
|-------|--------|
| Add/remove peserta untuk assessment **Proton** (`Category="Assessment Proton"`) | Punya state `ProtonTrack`; add/remove live berisiko korup track — endpoint menolak sesi Proton |
| **Self-service enrolment** (peserta daftar sendiri) | Hanya Admin/HC; bukan kebutuhan |
| **Hard-delete peserta bersertifikat** | Diganti soft-remove agar sertifikat utuh + reversibel |
| **Force-disconnect koneksi fisik SignalR** | Cukup event `examRemoved` + redirect client + guard server re-entry |
| Migration tabel/skema selain 3 kolom removal | v32.5 hanya tambah `RemovedAt/RemovedBy/RemovalReason` (additif nullable) |

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| PART-05 | 412 | pending |
| PART-06 | 410 | Complete |
| PART-07 | 410 | Complete |
| PRMV-01 | 411 | Complete |
| PRMV-02 | 412 | Complete |
| PRMV-03 | 409 | Complete |
| PRMV-04 | 411 | Complete |
| PRMV-05 | 411 | Complete |
| PLIV-01 | 412 | Complete |
| PLIV-02 | 412 | Complete |
| PLIV-03 | 411 | Complete |

**Phase mapping (5 fase, 409-413):**
- **Phase 409** Data Foundation + Re-entry Guards + Exclude-Removed Query — PRMV-03 (migration=TRUE `AddParticipantRemovalColumns`; fondasi exclude-query untuk PLIV-01)
- **Phase 410** Add-Participant Backend Live — PART-06, PART-07
- **Phase 411** Remove + Restore Backend Live — PRMV-01, PRMV-04, PRMV-05, PLIV-03
- **Phase 412** Live Monitoring UI + SignalR — PART-05, PRMV-02, PLIV-01, PLIV-02
- **Phase 413** Test + UAT — (no new REQ; verifikasi end-to-end semua 11 REQ)

**Coverage:**
- v1 requirements: 11 total
- Mapped to phases: 11 ✓ (PRMV-03→409; PART-06/07→410; PRMV-01/04/05 + PLIV-03→411; PART-05 + PRMV-02 + PLIV-01/02→412)
- Unmapped: 0 — Orphans: 0 — Duplicates: 0
- migration=TRUE hanya Phase 409 (`AddParticipantRemovalColumns`); Phase 410-413 = migration=FALSE
- Depends chain: 409 → (410 ∥ 411, file-overlap `AssessmentAdminController.cs` → sequential) → 412 → 413

---
*Requirements defined: 2026-06-19*
*Last updated: 2026-06-19 — roadmap created (Phases 409-413), traceability filled, 11/11 mapped*
