# Project Instructions

Always respond in Bahasa Indonesia.

## Develop Workflow (Wajib Dibaca)

Status website: **Lokal → Development (10.55.3.3) → Production**.

Aturan ringkas saat fix bug / development:

1. Cek bug via URL Dev (`http://10.55.3.3/KPB-PortalHC`)
2. Reproduce & fix di lokal
3. Verifikasi lokal: `dotnet build` + `dotnet run` (cek di `http://localhost:5277`; **di branch `ITHandoff` pakai `http://localhost:5270`** biar tidak tabrakan dgn worktree `main` — detail [`docs/DEV_WORKFLOW.md`](docs/DEV_WORKFLOW.md) §1) + cek DB lokal (+ Playwright bila ada)
4. Commit & push (sertakan file migration kalau ada)
5. Promosi ke server Dev & DB Dev = **tanggung jawab Team IT**, bukan developer. Notifikasi IT dengan commit hash + flag migration.

❌ Jangan edit kode/DB langsung di server Dev/Prod. ❌ Jangan push tanpa verifikasi lokal.

Detail lengkap (environment map, SOP migration, checklist): lihat [`docs/DEV_WORKFLOW.md`](docs/DEV_WORKFLOW.md).

## Seed Data Workflow (Lokal)

Aturan ringkas saat butuh seed data untuk testing/development:

1. **Klasifikasi dulu** sebelum membuat seed — `temporary + local-only` (untuk test/reproduce) atau `permanent + prod-required` (masuk `Data/SeedData.cs`). Kalau ragu, diskusikan.
2. **Snapshot DB** lokal (`sqlcmd ... BACKUP DATABASE`) sebelum insert seed temporary.
3. **Catat di `docs/SEED_JOURNAL.md`** — tujuan, klasifikasi, dampak entitas tersentuh.
4. **Restore DB** setelah test selesai (sukses *atau* gagal), lalu tandai journal `cleaned`.

❌ Jangan biarkan seed temporary nempel di DB lokal lewat session kerja. ❌ Jangan promosikan seed temporary jadi permanent tanpa review (pindah ke `Data/SeedData.cs` dulu, baru commit).

Detail lengkap (klasifikasi, format journal, command SQL Server BACKUP/RESTORE): lihat [`docs/SEED_WORKFLOW.md`](docs/SEED_WORKFLOW.md).
