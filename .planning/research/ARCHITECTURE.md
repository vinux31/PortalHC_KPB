# Architecture Patterns: Pre-deployment Audit & Finalization

**Domain:** ASP.NET Core MVC portal deployment ke IIS production
**Researched:** 2026-03-25
**Confidence:** HIGH (analisis langsung dari Program.cs, appsettings, csproj)

---

## Arsitektur Saat Ini (Development)

```
Browser -> Kestrel (localhost:5xxx) -> ASP.NET Core 8 MVC
                                        |
                                        +-- EF Core -> SQLite (HcPortal.db)
                                        +-- Identity (local password auth)
                                        +-- SignalR (AssessmentHub)
                                        +-- ClosedXML, QuestPDF
```

## Arsitektur Target (Production)

```
Browser -> IIS (Reverse Proxy, HTTPS) -> Kestrel (in-process)
              |                              |
              +-- HTTPS termination          +-- EF Core -> SQL Server
              +-- WebSocket enabled          +-- Identity + HybridAuthService -> AD (LDAP)
              +-- Static file caching        +-- SignalR (WebSocket)
              +-- Request filtering          +-- ClosedXML, QuestPDF (temp file access)
```

---

## Integration Points: Apa yang Berubah untuk Production

### 1. Database: SQLite -> SQL Server

**Status:** Sudah disiapkan. `appsettings.Production.json` punya template SQL Server connection string. Package `Microsoft.EntityFrameworkCore.SqlServer` sudah di csproj.

**Yang perlu dilakukan:**
- Isi placeholder credential di `appsettings.Production.json` (atau lebih baik via environment variable)
- Jalankan migration terhadap SQL Server: `dotnet ef database update`
- Audit semua `ExecuteSqlRaw` / `FromSqlRaw` / `SqlQueryRaw` untuk SQLite-specific syntax
- Program.cs line 129-135: PRAGMA WAL block sudah ada guard `ProviderName == Sqlite` -- aman, tapi bersihkan jika hanya target SQL Server

**Risiko:** Raw SQL yang SQLite-specific. Ditemukan di Program.cs (PRAGMA -- sudah di-guard). Perlu scan seluruh codebase untuk raw SQL lain.

### 2. Authentication: Local -> AD (LDAP)

**Status:** Sudah disiapkan lengkap. Infrastruktur sudah ada:

| File | Peran | Status |
|------|-------|--------|
| `Services/HybridAuthService.cs` | Router: AD first, local fallback untuk admin | Ready |
| `Services/LdapAuthService.cs` | LDAP bind ke domain controller | Ready |
| `Services/LocalAuthService.cs` | Identity password check (fallback) | Ready |
| `Services/IAuthService.cs` | Interface abstraction | Ready |
| Program.cs line 57-87 | Config toggle DI registration | Ready |

**Yang perlu dilakukan:**
- Set `Authentication:UseActiveDirectory = true` di production
- Konfigurasi `LdapPath` sesuai domain controller production (saat ini: `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com`)
- Pastikan IIS app pool identity punya network access ke domain controller
- Test: admin@pertamina.com tetap bisa login via local fallback

### 3. IIS Hosting Configuration

**Yang perlu dibuat/dikonfigurasi:**

| Item | Status | Detail |
|------|--------|--------|
| `web.config` | **Belum ada** | AspNetCoreModuleV2, WebSocket enable, environment var |
| IIS App Pool | Belum | .NET CLR = No Managed Code, Pipeline = Integrated |
| WebSocket protocol | Belum | Wajib untuk SignalR (`/hubs/assessment`) |
| HTTPS binding | Belum | SSL certificate di IIS site |
| `ASPNETCORE_ENVIRONMENT` | Belum | Set `Production` via IIS env var atau web.config |
| ASP.NET Core Hosting Bundle | Belum | Install di server (.NET 8 runtime + IIS module) |

**web.config yang dibutuhkan:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*"
             modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\HcPortal.dll"
                  stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <webSocket enabled="true" />
    </system.webServer>
  </location>
</configuration>
```

### 4. SignalR di IIS

Hub endpoint: `/hubs/assessment` (real-time exam monitoring).

- **Wajib:** WebSocket protocol enabled di IIS site features
- Tanpa WebSocket, SignalR fallback ke Long Polling -- jauh lebih lambat, bisa timeout saat assessment
- Program.cs line 98-109 sudah handle 401 untuk hub endpoints (bukan redirect ke login) -- production-ready

### 5. File Storage & Write Access

**Lokasi file yang ditulis oleh app:**

| Path | Digunakan Oleh | Akses Diperlukan |
|------|----------------|------------------|
| `/uploads/evidence/` | Evidence upload coachee | Write |
| `/uploads/guidance/` | Panduan coaching HC | Write |
| `wwwroot/` | Static files | Read (sudah ada) |
| Temp directory | ClosedXML export, QuestPDF generate | Write |
| `./logs/stdout` | IIS stdout logging | Write |

App pool identity perlu write permission ke direktori-direktori di atas.

### 6. Session & Caching

**Current:** `DistributedMemoryCache` (in-process) + `MemoryCache`. Untuk single-server deployment ini **cukup**.

Cookie config sudah production-appropriate:
- 8 jam expiry dengan sliding expiration
- HttpOnly = true
- IsEssential = true

---

## Component Boundaries

| Component | Tanggung Jawab | Perubahan Production |
|-----------|---------------|---------------------|
| Program.cs | Bootstrap, DI, middleware | Auth toggle aktif, HTTPS redirect aktif, error handler aktif |
| ApplicationDbContext | EF Core data access | Provider switch otomatis via connection string |
| HybridAuthService | AD + local auth routing | **Diaktifkan** via config toggle |
| LdapAuthService | LDAP bind authentication | Perlu network access ke DC |
| AssessmentHub | Real-time exam monitoring | WebSocket harus enabled di IIS |
| SeedData / SeedProtonData | Role & data seeding | **Harus diaudit** -- idempotency critical |
| StaticFileOptions | PDF inline serving | Tetap sama |
| Error handling middleware | Exception handler | `/Home/Error` aktif di non-Development |
| HTTPS redirection | Force HTTPS | Aktif di non-Development |

---

## Data Flow: Production Login

```
1. Browser -> IIS (443/HTTPS)
2. IIS -> Kestrel (in-process via AspNetCoreModuleV2)
3. Cookie auth check -> /Account/Login jika belum auth
4. Login POST -> AccountController -> HybridAuthService
   4a. LdapAuthService.Authenticate(email, password) -> LDAP bind ke DC
   4b. Sukses -> SignInManager.SignInAsync (Identity cookie)
   4c. Gagal + admin@pertamina.com -> fallback LocalAuthService
5. Subsequent requests -> cookie valid -> akses controller
6. EF Core -> SQL Server (bukan SQLite)
7. SignalR -> WebSocket via IIS
```

---

## File Baru / Modifikasi yang Diperlukan

### Baru

| File | Tujuan |
|------|--------|
| `web.config` | IIS hosting configuration |

### Modifikasi

| File | Perubahan |
|------|-----------|
| `appsettings.Production.json` | Real connection string, `UseActiveDirectory: true` |

### Audit (Tidak Diubah, Tapi Harus Diverifikasi)

| File | Yang Diverifikasi |
|------|-------------------|
| Program.cs | Auto-migrate behavior, seed idempotency |
| Semua Controller/Service | Raw SQL compatibility SQL Server |
| Migration files | SQLite-specific syntax |
| SeedData.cs / SeedProtonData.cs | Idempotency -- jangan overwrite data production |

---

## Anti-Pola yang Harus Dihindari

### 1. Credentials di Source Control
**Masalah:** Password SQL Server atau LDAP di-commit ke git via appsettings.Production.json
**Solusi:** Gunakan environment variables di IIS atau User Secrets. appsettings.Production.json hanya berisi template/placeholder.

### 2. Auto-Migrate di Production Tanpa Kontrol
**Masalah:** `context.Database.Migrate()` di Program.cs (line 124) otomatis apply migration saat startup. Jika migration gagal, app crash tanpa rollback.
**Solusi:** Dua opsi: (a) jalankan migration manual sebelum deploy, atau (b) tambahkan config flag `"Database:AutoMigrate": false` untuk production. Rekomendasi: opsi (b) -- tetap auto-migrate di dev, manual di production.

### 3. Seed Data Overwrite Production
**Masalah:** `SeedData.InitializeAsync` dan `SeedProtonData.SeedAsync` dijalankan setiap startup. Jika tidak benar-benar idempotent, bisa reset/duplicate data.
**Solusi:** Audit kedua file -- pastikan selalu check-before-insert. Pertimbangkan skip seed di production setelah initial deployment.

### 4. Logging Sensitive Data
**Masalah:** EF Core command logging (aktif di Development config) bisa log SQL dengan parameter berisi data sensitif.
**Solusi:** Pastikan `Microsoft.EntityFrameworkCore.Database.Command` **tidak** di-set ke Information di production config (saat ini sudah benar -- hanya di Development.json).

---

## Urutan Build yang Benar (Dependency Order)

```
1. SQL COMPATIBILITY AUDIT
   Scan semua raw SQL, migration files untuk SQLite-specific syntax
   (prerequisite -- harus tahu apa yang perlu difix sebelum deploy)
   |
2. PRODUCTION CONFIG
   web.config baru, appsettings.Production.json final (tanpa secret)
   Environment variable setup documentation
   |
3. SEED DATA SAFETY
   Audit SeedData.cs + SeedProtonData.cs idempotency
   Tambah auto-migrate toggle jika perlu
   |
4. SECURITY & LOGGING
   HTTPS enforcement, security headers
   Production logging config (tanpa sensitive data)
   Error page untuk production
   |
5. DEPLOYMENT CHECKLIST
   IIS setup steps, hosting bundle install
   SQL Server migration steps
   AD connectivity test
   SignalR WebSocket verification
   File permission setup
```

**Implikasi roadmap:**
- Step 1-3 bisa dikerjakan tanpa server production (codebase audit)
- Step 4-5 membutuhkan akses ke environment production/staging
- Step 1 harus selesai sebelum migration ke SQL Server bisa dijalankan

---

## Pertimbangan Skalabilitas

| Concern | Single Server (Target) | Multi-Server (Future) |
|---------|----------------------|----------------------|
| Session | In-memory cache (OK) | Redis / SQL distributed cache |
| SignalR | In-process (OK) | Redis backplane |
| File upload | Local disk (OK) | Shared storage |
| Database | Single SQL Server (OK) | Read replicas |
| Deployment | Manual xcopy/publish (OK) | CI/CD pipeline |

Target saat ini adalah single-server IIS -- arsitektur yang ada sudah sesuai.

---

## Sumber

- Source code langsung: `Program.cs`, `appsettings.*.json`, `HcPortal.csproj`
- Service files: `Services/HybridAuthService.cs`, `Services/LdapAuthService.cs`, `Services/LocalAuthService.cs`
- ASP.NET Core 8 IIS hosting model: in-process via AspNetCoreModuleV2 (HIGH confidence)
- SignalR WebSocket requirement di IIS (HIGH confidence)
- Confidence: HIGH -- semua analisis dari kode aktual di repository

---

*Architecture research untuk: v9.0 Pre-deployment Audit & Finalization*
*Researched: 2026-03-25*
