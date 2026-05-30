# DB Handoff IT — Deployment {DATE}

**Commit:** `{COMMIT_HASH}`
**Branch:** `{BRANCH}`
**Migration Flag:** {YES_NO}
**Affected Environment:** Dev (10.55.3.3) / Prod
**Developer Contact:** {DEVELOPER_NAME} ({DEVELOPER_EMAIL})
**Deployment Window:** {WINDOW_START} - {WINDOW_END}

> Template Source: `docs/templates/DB_HANDOFF_IT.template.md` (Phase 338 REST-05)
> Generated via copy-and-fill. Tujuan: systematize backup workflow supaya IT tidak lupa
> jalankan backup pre-migration (root cause Cilacap incident — lihat
> `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-ROOT_CAUSE.md`).

---

## 1. Pre-Deploy Backup (MANDATORY)

**SEBELUM** migration atau redeploy aplikasi, IT WAJIB jalankan salah satu opsi backup berikut:

### Opsi A — Standalone PowerShell script (Recommended)

```powershell
.\scripts\backup-dev-pre-migration.ps1 `
    -Server "10.55.3.3" `
    -Database "HcPortalDB_Dev" `
    -OutputPath "C:\Backup\HcPortalDB_Dev_pre_{DATE}_{HHMMSS}.bak"
```

Script lokasi: `scripts/backup-dev-pre-migration.ps1` di repo (Phase 338 REST-05).
Windows Auth via `-E` flag — tidak hardcode credential.

### Opsi B — Inline T-SQL via SSMS

```sql
BACKUP DATABASE [HcPortalDB_Dev]
TO DISK = N'C:\Backup\HcPortalDB_Dev_pre_{DATE}_{HHMMSS}.bak'
WITH FORMAT, INIT, COMPRESSION,
     NAME = N'HcPortalDB_Dev Pre-Migration Full Backup',
     STATS = 10;
```

### Validation

- [ ] Backup file `.bak` ter-create di disk
- [ ] File size > 100 MB (sesuaikan baseline DB size — kalau jauh lebih kecil = mungkin corrupt)
- [ ] Catat timestamp + path di section "Deployment Log" di bawah

---

## 2. Migration List

{MIGRATION_LIST}

Bila `Migration Flag = NO` (zero-migration deploy), section ini boleh kosong / "N/A".

Contoh format isian:
```
- 20260530143000_AddSessionIdToHistoryRow.cs (additive: AssessmentSessions table, no destructive)
- 20260601092000_AddIndexOnCompletedAt.cs (additive: CREATE INDEX, no data migration)
```

---

## 3. Affected Tables

{AFFECTED_TABLES}

Contoh:
```
- AssessmentSessions (ALTER COLUMN nullable)
- AssessmentAttemptHistory (CREATE INDEX)
```

Indicate kalau ada ALTER yang destructive (DROP COLUMN, RENAME, dst) — risk LOSS data.

---

## 4. Deployment Steps

1. **Pre-deploy:**
   - [ ] Konfirmasi backup completed (Section 1) — share file path + size ke developer
2. **Stop service:**
   - [ ] Stop IIS App Pool atau `systemctl stop dotnet-hcportal` (sesuaikan env)
3. **Pull code:**
   - [ ] `cd /path/to/HcPortal && git fetch && git checkout {COMMIT_HASH}`
4. **Apply migration (bila YES):**
   - [ ] `dotnet ef database update`
   - [ ] Verify no error output
5. **Start service:**
   - [ ] Start IIS App Pool atau `systemctl start dotnet-hcportal`
6. **Smoke test:**
   - [ ] Navigate `http://10.55.3.3/KPB-PortalHC/` (Dev) atau prod URL
   - [ ] Login admin@pertamina.com → ke beranda OK
   - [ ] 1 critical page render (e.g., `/Admin/ManageAssessment` atau `/CMP/Records`)

---

## 5. Rollback Plan

Bila migration gagal atau smoke test FAIL:

```sql
USE master;
ALTER DATABASE [HcPortalDB_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [HcPortalDB_Dev]
FROM DISK = N'C:\Backup\HcPortalDB_Dev_pre_{DATE}_{HHMMSS}.bak'
WITH REPLACE;
ALTER DATABASE [HcPortalDB_Dev] SET MULTI_USER;
```

Lalu `git checkout` commit sebelumnya + restart service.

Notify developer dengan timestamp rollback + error log.

---

## 6. Deployment Log (IT fill on completion)

| Step | Status | Timestamp | Notes |
|------|--------|-----------|-------|
| Backup completed | ☐ OK / ☐ FAIL | | Path: `____` Size: `__ MB` |
| Migration applied | ☐ OK / ☐ SKIP / ☐ FAIL | | |
| Smoke test login | ☐ OK / ☐ FAIL | | |
| Smoke test page | ☐ OK / ☐ FAIL | | Page: `____` |
| Rollback executed | ☐ YES / ☐ NO | | Reason: `____` |

**IT signoff:** ___________________________ Date: ___________
