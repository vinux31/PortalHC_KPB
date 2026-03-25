# Domain Pitfalls — Pre-deployment Audit & Finalization

**Domain:** ASP.NET Core MVC deployment ke production IIS dengan AD authentication dan SQL Server
**Researched:** 2026-03-25
**Confidence:** HIGH (berdasarkan inspeksi langsung Program.cs, appsettings*.json, SeedData.cs)

---

## Critical Pitfalls

Kesalahan yang menyebabkan app tidak jalan, data bocor, atau security bypass di production.

### Pitfall 1: Active Directory Toggle Tidak Aktif di Production

**What goes wrong:** `appsettings.Production.json` saat ini TIDAK mengandung `Authentication:UseActiveDirectory`. Base `appsettings.json` set `false`. Di production, app akan pakai `LocalAuthService` — siapapun bisa login dengan password Identity biasa, bukan credential AD Pertamina.
**Why it happens:** `appsettings.Production.json` hanya override ConnectionStrings dan Logging, lupa override Authentication section.
**Consequences:** Bypass Active Directory sepenuhnya. Login tanpa credential Pertamina. Celah keamanan kritis.
**Prevention:** Tambahkan ke `appsettings.Production.json`:
```json
"Authentication": {
  "UseActiveDirectory": true
}
```
**Detection:** Cek log startup — jika tidak ada log dari `LdapAuthService`/`HybridAuthService`, AD tidak aktif.

### Pitfall 2: Connection String Placeholder di Production Config

**What goes wrong:** `appsettings.Production.json` berisi literal `Server=YOUR_SQL_SERVER_NAME` dan `Password=YOUR_PASSWORD`.
**Why it happens:** Template connection string belum diganti dengan credential asli.
**Consequences:** App crash saat startup. Lebih buruk: jika placeholder kebetulan resolve, data masuk database yang salah.
**Prevention:**
1. JANGAN hardcode credential di file yang di-commit ke git.
2. Gunakan environment variable di IIS: `ASPNETCORE_ConnectionStrings__DefaultConnection`.
3. Atau buat `appsettings.Production.json` manual di server (tidak di-commit).
**Detection:** Pre-deployment: test connection string bisa connect sebelum deploy.

### Pitfall 3: ASPNETCORE_ENVIRONMENT Tidak Di-set di IIS

**What goes wrong:** IIS tidak otomatis set `ASPNETCORE_ENVIRONMENT`. Tanpa ini, ASP.NET Core default ke `Production` (correct), tapi jika ada yang set ke `Development` di server untuk debugging dan lupa revert — seed data UAT akan masuk production DB.
**Why it happens:** Environment variable management di IIS bukan default workflow yang familiar.
**Consequences:** Jika salah set ke Development: test users dengan weak passwords di-seed ke production DB, UAT assessment data masuk production.
**Prevention:** Set eksplisit di `web.config`:
```xml
<aspNetCore processPath="dotnet" arguments=".\HcPortal.dll">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```
**Detection:** Verifikasi di startup log environment name. Tambahkan log line eksplisit di Program.cs.

### Pitfall 4: SeedProtonData Jalan di Semua Environment

**What goes wrong:** `SeedProtonData.SeedAsync(context)` dipanggil di Program.cs line 125 TANPA environment check. Berbeda dengan `SeedData` yang sudah gate `IsDevelopment()` untuk test users dan UAT data. Jika SeedProtonData berisi data yang hanya relevan untuk testing, itu masuk production.
**Why it happens:** SeedProtonData ditambahkan sebelum pattern environment-gated seeding established.
**Consequences:** Tergantung isi SeedProtonData — jika berisi reference data (ProtonTracks master), ini DIBUTUHKAN di production. Jika berisi test data, itu contaminate production.
**Prevention:** Audit isi `SeedProtonData.SeedAsync()`. Classify: reference data (always seed) vs test data (development only). Pastikan idempotent.
**Detection:** Review setiap insert/upsert di SeedProtonData.

### Pitfall 5: Database.Migrate() di Startup — Silent Failure

**What goes wrong:** `context.Database.Migrate()` dijalankan setiap app start (line 124). Catch block di line 137-141 hanya LOG error lalu CONTINUE. App bisa berjalan dengan schema lama jika migration gagal.
**Why it happens:** Error handling terlalu permissive — app tidak fail-fast pada migration error.
**Consequences:** App running tapi query gagal karena missing column/table. Error muncul secara acak saat user hit fitur yang butuh schema baru.
**Prevention:**
1. Untuk production: jalankan migration terpisah via CLI sebelum deploy (`dotnet ef database update`).
2. Atau: ubah catch block jadi `throw` agar app fail-fast jika migration gagal.
3. Minimal: tambahkan health check endpoint yang verify schema version.
**Detection:** Monitor startup log untuk migration errors. Tambahkan schema version check.

---

## Moderate Pitfalls

### Pitfall 6: Password Policy Terlalu Lemah untuk Admin Fallback

**What goes wrong:** Program.cs disable semua password complexity (RequireDigit=false, RequiredLength=6). Ini ada di code, bukan config — berlaku di semua environment. HybridAuthService fallback ke LocalAuthService untuk `admin@pertamina.com`.
**Why it happens:** Development convenience yang tidak pernah di-harden untuk production.
**Consequences:** Admin fallback account bisa punya password "123456". Jika AD down, fallback account jadi satu-satunya entry point — dengan password lemah.
**Prevention:** Pastikan admin fallback password KUAT sebelum deploy. Pertimbangkan hardcode minimum length 12 untuk production, atau disable fallback entirely di production.

### Pitfall 7: LDAP Path dan Credential Belum Divalidasi

**What goes wrong:** `appsettings.json` mengandung `LdapPath: "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com"` — ini mungkin benar atau mungkin development placeholder. Jika salah, semua login gagal di production.
**Why it happens:** LDAP path spesifik ke AD infrastructure Pertamina — tidak bisa divalidasi tanpa akses ke AD server.
**Consequences:** 100% user tidak bisa login di production. App tampil normal tapi login selalu gagal.
**Prevention:** Test LDAP connection dari production server sebelum deploy. Tambahkan health check endpoint yang test AD connectivity.
**Detection:** Test login dengan satu AD account sebelum go-live.

### Pitfall 8: Static Files dan Upload Folder Tidak Persistent

**What goes wrong:** File uploads (coaching guidance, evidence) disimpan di disk. Saat re-deploy atau server rebuild, file hilang jika folder tidak di luar publish directory.
**Why it happens:** Default ASP.NET Core serve static files dari `wwwroot`. Upload sering ke subfolder di `wwwroot`.
**Consequences:** Semua uploaded evidence, guidance files, dan dokumen hilang setelah redeploy.
**Prevention:** Pastikan upload folder ada di LUAR publish directory, atau gunakan shared storage/NAS. Tambahkan ke deployment checklist dan backup strategy.
**Detection:** After deploy: verify uploaded files masih accessible.

### Pitfall 9: HTTPS Configuration Missing

**What goes wrong:** `UseHttpsRedirection()` aktif di production (line 151-153) tapi tidak ada Kestrel HTTPS config. Jika IIS tidak handle SSL termination, redirect loop terjadi.
**Why it happens:** HTTPS setup tergantung infrastruktur (IIS binding, certificate) yang di luar codebase.
**Consequences:** Redirect loop (HTTP -> HTTPS -> HTTP) atau mixed content warnings.
**Prevention:** Pastikan IIS site binding punya HTTPS dengan valid certificate. Jika IIS handles SSL termination sebagai reverse proxy, tambahkan `ForwardedHeaders` middleware di Program.cs.

### Pitfall 10: DistributedMemoryCache Tidak Survive App Restart

**What goes wrong:** `AddDistributedMemoryCache()` in-process only. Session dan TempData hilang saat app pool recycle (IIS default: setiap 29 jam, atau setelah idle 20 menit).
**Why it happens:** In-memory cache adalah default development config.
**Consequences:** User kehilangan session di tengah assessment exam — jawaban hilang jika belum di-save ke DB. Bukan data loss tapi UX buruk.
**Prevention:** Untuk single-instance (likely), ini acceptable tapi dokumentasikan bahwa IIS idle timeout harus disesuaikan. Pastikan assessment progress di-save ke DB secara incremental, bukan hanya saat final submit.

---

## Minor Pitfalls

### Pitfall 11: Logging Level Terlalu Verbose

**What goes wrong:** Production logging `Default: Information` — menghasilkan log sangat banyak termasuk setiap HTTP request.
**Prevention:** Set production: `Default: Warning`, `Microsoft.AspNetCore: Warning`, `HcPortal: Information` (hanya app-specific logs verbose).

### Pitfall 12: AllowedHosts Wildcard

**What goes wrong:** `"AllowedHosts": "*"` menerima request dari hostname apapun. Di intranet bisa acceptable, tapi host header injection possible.
**Prevention:** Set ke hostname spesifik portal (e.g., `"portalhc.pertamina.com"`).

### Pitfall 13: HcPortal.db di Repository

**What goes wrong:** Git status menunjukkan `HcPortal.db` pernah tracked. SQLite dev database bisa bocor ke production deploy.
**Prevention:** Pastikan `HcPortal.db` di `.gitignore`. Verify tidak ada di publish output.

### Pitfall 14: SQLite WAL Pragma di Production Code Path

**What goes wrong:** Program.cs line 129-135 — SQLite pragma. Sudah guarded by provider check, tidak berbahaya. Tapi noisy untuk code review.
**Prevention:** Biarkan (guarded) atau bungkus dalam `IsDevelopment()`.

### Pitfall 15: bin/Debug dan publish Folder di Repository

**What goes wrong:** Glob menemukan `appsettings*.json` di `bin/Debug/`, `obj/testbuild/`, `publish/` — artinya build artifacts mungkin di-commit atau tidak di-.gitignore.
**Prevention:** Pastikan `bin/`, `obj/`, `publish/` ada di `.gitignore`. Hapus dari tracking jika sudah committed.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Seed data cleanup | P4: SeedProtonData tanpa env guard | Audit setiap seed method, classify reference vs test |
| Production config | P1, P2, P3: AD off, placeholder creds, env not set | Production config checklist with verification |
| AD integration test | P7: LDAP path unvalidated | Test AD login dari production server |
| Logging & error handling | P5, P11: Migration silent fail, verbose logs | Fail-fast migration, tune log levels |
| IIS deployment | P3, P9: Env var, HTTPS | web.config template with all required settings |
| Security hardening | P6, P12: Weak admin password, wildcard host | Harden admin account, restrict AllowedHosts |
| File & storage | P8, P13, P15: Upload persistence, SQLite in repo, build artifacts | Backup strategy, .gitignore audit |
| Session & state | P10: Memory cache not persistent | Tune IIS idle timeout, verify exam auto-save |

---

## Pre-deployment Checklist (derived from pitfalls)

**Config (MUST before deploy):**
1. [ ] `appsettings.Production.json` has `UseActiveDirectory: true`
2. [ ] Connection string valid (bukan placeholder), credentials not in git
3. [ ] `ASPNETCORE_ENVIRONMENT=Production` in web.config or IIS env vars
4. [ ] LDAP path validated by test login from production server
5. [ ] AllowedHosts set ke hostname spesifik

**Database (MUST before deploy):**
6. [ ] SeedProtonData diaudit — only reference data runs in production
7. [ ] Test user seeding skipped in production (OK — IsDevelopment guard exists)
8. [ ] Database migration tested separately before deploy
9. [ ] Migration failure = app should NOT start (fix catch block)

**Infrastructure (MUST before deploy):**
10. [ ] IIS HTTPS binding with valid certificate
11. [ ] Upload folder persistent, outside publish dir, backed up
12. [ ] IIS idle timeout configured appropriately
13. [ ] Admin fallback password changed to strong password

**Cleanup (SHOULD before deploy):**
14. [ ] HcPortal.db not in publish output
15. [ ] bin/, obj/, publish/ in .gitignore
16. [ ] Logging level tuned for production
17. [ ] web.config template finalized

---

## Sources

- Inspeksi langsung: `Program.cs` (187 lines), `appsettings.json`, `appsettings.Production.json`, `appsettings.Development.json`, `Data/SeedData.cs`
- HIGH confidence — semua pitfall berdasarkan actual code review terhadap codebase ini
- ASP.NET Core IIS hosting model: training data knowledge (MEDIUM confidence untuk IIS-specific behavior)
