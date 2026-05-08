# Seed Journal

Audit trail untuk seed `temporary + local-only`. Lihat [`docs/SEED_WORKFLOW.md`](SEED_WORKFLOW.md) untuk aturan klasifikasi & flow.

**Cara isi:** Tambah satu baris per seed temporary (jangan digabung). Status `active` saat seed masih ada di DB → `cleaned` setelah restore. Entry tidak pernah dihapus (audit trail).

| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
|---------|-------|-------------|--------|----------------------------|---------------|--------|
| 2026-05-08 | 313 | temporary + local-only | UAT FLOW 313 timer enforcement (TMR-01) — 2-tier reject manual submit setelah waktu habis (7 fixtures: Online/PreTest/PostTest × Before/Tier-1/Tier-2 + Manual exclude) | AssessmentSessions(7) prefix `Phase 313 Timer Fixture`; pivot hijack Sessions(2) id 69+72 untuk live test (snapshot revert covers); AuditLog +1 row `SubmitExamBlocked` (snapshot revert covers) | HcPortalDB_Dev.20260508-pre313.bak | cleaned |
