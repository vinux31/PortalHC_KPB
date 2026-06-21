---
created: 2026-06-11T12:48:09.290Z
title: One-time cleanup data test/audit lokal setelah Phase 367 ship
area: database
files:
  - Controllers/AssessmentAdminController.cs (DeleteAssessment :2184, DeleteAssessmentGroup :2372, DeletePrePostGroup :2558 — akan dirombak 367)
  - docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md (spec C — cascade engine)
---

## Problem

DB lokal (HcPortalDB_Dev) penuh sesi assessment bekas test/audit fase-fase lama yang kini TAMPIL di default view setelah Phase 370 hapus window 7-hari (URG-02): "Legacy Exam 1775xxx" (×6), "Timer Expired 1775xxx" (×4), "Phase 314 Repro Token Bug", "UAT v14 Standard/PrePost", "ojt v1.9/v1.10/v14.2", "UAT ET Distribution Test", "OJT Token Test Q1-2026", "Multi Worker 1775xxx", dll — ±50 sesi + 20 Closed historis. User minta dibersihkan biar rekap HC bersih (konteks 2026-06-11, UAT Phase 370).

JANGAN hapus sebelum Phase 367 ship:
1. Endpoint delete saat ini cascade BOCOR (28 temuan spec C) — ninggalin orphan: EditLogs, PackageUserResponses, AttemptHistory, UserPackageAssignments, notifikasi, penanda Proton Origin='Exam', PendingProtonBypass, pasangan LinkedSessionId, file sertifikat.
2. Data legacy masih dipakai fixture UAT (370 baru pakai; sesi 364 e2e jalan paralel).
3. DB lokal shared dengan worktree main.

## Solution

Setelah Phase 367 (Delete Records Cascade Overhaul) SHIPPED:
1. Snapshot DB dulu (`sqlcmd ... BACKUP DATABASE` per SEED_WORKFLOW) + catat SEED_JOURNAL.
2. Hapus sesi test/audit via fitur delete cascade baru (preview konfirmasi) — sekalian jadi UAT real-data Phase 367.
3. Verifikasi zero orphan (assert per tabel — integration test 367 SC1 sudah sediakan pola).
4. Koordinasi: Phase 368 punya one-time cleanup AttemptHistory orphan legacy — bisa digabung satu sesi.
5. Lingkup = DB LOKAL saja. Dev (10.55.3.3) = jalur IT, jangan edit langsung.

## Resolution

**Promoted to backlog Phase 999.12 at v32.3 close (2026-06-21).** No longer a loose pending todo — now tracked milestone-backlog work. Precondition met (Phase 367 cascade overhaul SHIPPED `15cfbbcb`); DB still holds ~45 legacy test/audit sessions (60 total). Execute via `/gsd-review-backlog` → snapshot → 367 cascade-delete (preview, not raw SQL) → verify zero-orphan. Not a v32.3 deliverable.
