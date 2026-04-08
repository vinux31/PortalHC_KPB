# Smoke Test — CRIT-02 (CoachCoacheeMappingEdit)

Branch: `bugfix/proton-coaching`
DB: `HcPortalDB_Dev` (localhost\SQLEXPRESS)
Endpoint: `POST /Admin/CoachCoacheeMappingEdit` (AJAX, JSON)

> **Cara pakai:** jalankan app (`dotnet run`), login sebagai Admin/HC, lalu kerjakan tiap skenario berurutan. Tiap step ada SQL verifikasi — jalankan di SSMS / Azure Data Studio. Centang `[x]` saat lulus.

---

## Persiapan: pilih coachee uji

Pilih satu coachee yang **sudah punya progress dengan evidence file** agar Bug B (file delete) bisa diverifikasi.

```sql
-- Cari kandidat: coachee dengan progress yang punya EvidencePath
SELECT TOP 10
    m.Id AS MappingId,
    m.CoacheeId,
    m.CoachId,
    m.AssignmentSection,
    m.AssignmentUnit,
    a.Id AS AssignmentId,
    a.ProtonTrackId,
    p.Id AS ProgressId,
    p.EvidencePath,
    p.Status
FROM CoachCoacheeMappings m
INNER JOIN ProtonTrackAssignments a ON a.CoacheeId = m.CoacheeId AND a.IsActive = 1
INNER JOIN ProtonDeliverableProgresses p ON p.ProtonTrackAssignmentId = a.Id
WHERE m.IsActive = 1 AND p.EvidencePath IS NOT NULL
ORDER BY m.Id DESC;
```

Catat:
- `MAPPING_ID = ___`
- `COACHEE_ID = ___`
- `OLD_UNIT = ___`
- `OLD_PROGRESS_ID(s) = ___`
- `OLD_EVIDENCE_FOLDER = wwwroot\uploads\evidence\<OLD_PROGRESS_ID>\`

---

## Skenario 1 — Happy path: Phase 129 unit-change rebuild

**Aksi:** di UI mapping admin, klik Edit untuk `MAPPING_ID`, ubah `AssignmentUnit` ke unit valid lain (yang ada di section yang sama), Save.

- [ ] Response Network tab: `200 OK`, `Content-Type: application/json`, body `{"success":true,...}`
- [ ] DB: mapping row tersimpan unit baru
  ```sql
  SELECT Id, AssignmentSection, AssignmentUnit FROM CoachCoacheeMappings WHERE Id = <MAPPING_ID>;
  ```
- [ ] DB: progress lama hilang, progress baru muncul
  ```sql
  SELECT Id, ProtonDeliverableId, Status, CreatedAt
  FROM ProtonDeliverableProgresses
  WHERE ProtonTrackAssignmentId IN (
      SELECT Id FROM ProtonTrackAssignments WHERE CoacheeId = '<COACHEE_ID>' AND IsActive = 1
  );
  ```
  Old progress ID(s) tidak ada lagi, ada row baru dengan `CreatedAt` baru saja.
- [ ] Disk: folder `wwwroot\uploads\evidence\<OLD_PROGRESS_ID>\` **TERHAPUS**
  ```bash
  ls "wwwroot/uploads/evidence/<OLD_PROGRESS_ID>" 2>&1
  ```

---

## Skenario 2 — Failure path: Phase 129 rollback

**Setup:** ulangi Skenario 1 untuk dapat coachee baru dengan progress baru, atau pilih coachee lain dengan evidence. Catat ulang `OLD_UNIT`, `OLD_PROGRESS_ID`.

**Cara memicu failure:** ubah `AssignmentUnit` ke unit yang **tidak punya kompetensi/deliverable yang match** sehingga `AutoCreateProgressForAssignment` throw, ATAU sementara inject `throw new Exception("test")` di awal `AutoCreateProgressForAssignment` (line ~1280-an di `CoachMappingController.cs`) lalu **revert setelah test**.

- [ ] Response: `200 OK`, JSON body `{"success":false,"message":"Gagal menyimpan perubahan: ..."}`
- [ ] **Bukan** HTTP 302 / HTML — ini menutup Bug D
- [ ] DB: mapping unit **tidak berubah** (rollback Bug A)
  ```sql
  SELECT Id, AssignmentUnit FROM CoachCoacheeMappings WHERE Id = <MAPPING_ID>;
  -- Harus = OLD_UNIT
  ```
- [ ] DB: progress lama **masih utuh**
  ```sql
  SELECT Id FROM ProtonDeliverableProgresses WHERE Id IN (<OLD_PROGRESS_ID(s)>);
  -- Harus mengembalikan row yang sama
  ```
- [ ] Disk: folder `wwwroot\uploads\evidence\<OLD_PROGRESS_ID>\` **MASIH ADA** (Bug B fixed)
- [ ] Log: ada entry `CoachCoacheeMappingEdit failed for mapping ...; rolled back`

> Jika Anda inject error sementara, **revert dulu** sebelum lanjut Skenario 3.

---

## Skenario 3 — Happy path: ProtonTrack change

**Aksi:** Edit mapping, ubah `ProtonTrackId` ke track lain yang valid.

- [ ] Response JSON `success=true`
- [ ] DB: assignment lama `IsActive=0`, assignment baru `IsActive=1`
  ```sql
  SELECT Id, ProtonTrackId, IsActive, AssignedAt
  FROM ProtonTrackAssignments
  WHERE CoacheeId = '<COACHEE_ID>'
  ORDER BY Id DESC;
  ```
- [ ] DB: progress baru muncul untuk assignment baru
- [ ] Disk: folder evidence dari progress lama **terhapus**

---

## Skenario 4 — Failure path: ProtonTrack change rollback (Bug C)

Ulangi Skenario 3 tetapi inject error di `AutoCreateProgressForAssignment` lagi (atau pilih track yang akan throw).

- [ ] Response JSON `success=false`
- [ ] DB: assignment lama tetap `IsActive=1` (rollback Bug C)
- [ ] DB: progress lama tetap utuh
- [ ] DB: assignment baru **tidak ada** (rolled back)
- [ ] Disk: folder evidence lama **masih ada**

---

## Skenario 5 — AJAX response shape sanity

Buka DevTools → Network. Untuk semua skenario di atas, periksa request `CoachCoacheeMappingEdit`:

- [ ] Status `200` (bukan 302)
- [ ] Response Headers: `Content-Type: application/json; charset=utf-8`
- [ ] Response Body: valid JSON, bukan HTML

---

## Cleanup setelah test

Jika Anda mengubah unit/track coachee uji untuk testing, pertimbangkan rollback manual via UI atau restore dari backup DB.
