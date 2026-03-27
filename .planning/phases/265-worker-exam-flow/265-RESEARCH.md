# Phase 265: Worker Exam Flow - Research

**Researched:** 2026-03-27
**Domain:** UAT — worker exam flow testing di server development
**Confidence:** HIGH

## Summary

Phase ini adalah UAT (User Acceptance Testing) untuk flow worker mengerjakan ujian assessment OJT. Semua kode sudah ada dan di-deploy di server development. Tidak ada development baru — hanya testing, bug discovery, dan bug fix di project lokal.

Kode exam flow sudah lengkap dan matang: Assessment.cshtml (lobby/list), StartExam.cshtml (~900 baris dengan inline JS untuk timer, auto-save, pagination, abandon), assessment-hub.js (SignalR), dan CMPController.cs (8 action endpoints). Testing fokus pada 3 worker dengan pembagian skenario yang sudah ditentukan user.

**Primary recommendation:** Buat skenario UAT terstruktur per worker (rino=token+happy path, arsyad=non-token, widyadhana=abandon), jalankan semua dulu, kumpulkan bug, fix batch.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Test 2 assessment: 1 dengan token, 1 tanpa token (keduanya dibuat di Phase 264)
- **D-02:** rino.prasetyo — happy path lengkap di assessment **dengan token** (EXAM-01 s/d EXAM-06 + token verification EXAM-02)
- **D-03:** mohammad.arsyad — happy path di assessment **tanpa token** (verifikasi flow tanpa token juga lancar)
- **D-04:** moch.widyadhana — **khusus test abandon** (EXAM-08), tidak perlu selesaikan ujian
- **D-05:** Cukup verifikasi badge `#hubStatusBadge` ("Live") dan `#networkStatusBadge` ("Tersimpan") tampil dalam kondisi normal saat ujian berjalan
- **D-06:** Test koneksi putus, reconnect, dan offline behavior ada di Phase 267 (Resilience & Edge Cases)
- **D-07:** Test abandon menggunakan worker terpisah (moch.widyadhana) agar tidak mengganggu flow test worker lain
- **D-08:** Verifikasi: setelah abandon, worker tidak bisa masuk ujian lagi (redirect dengan pesan error)
- **D-09:** Alur sama: jalankan semua skenario test dulu → kumpulkan semua bug → fix batch di project lokal
- **D-10:** Verifikasi dual: visual check di browser + query database untuk konfirmasi data tersimpan benar

### Claude's Discretion
- Urutan langkah-langkah test spesifik per worker (Claude tentukan berdasarkan analisa kode)
- Query database apa yang perlu dijalankan untuk verifikasi auto-save dan timer
- Skenario navigasi halaman mana yang ditest (first→next, jump, last→prev, dll)

### Deferred Ideas (OUT OF SCOPE)
- Test koneksi putus/resume/offline → Phase 267
- Test timer habis behavior → Phase 267
- Test review jawaban & submit → Phase 266
- Test grading & sertifikat → Phase 266
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EXAM-01 | Worker dapat melihat daftar assessment (status badge, jadwal) | Assessment.cshtml: cards dengan status badge (Open/Upcoming/InProgress/Expired), jadwal, durasi, token indicator. Tab filtering Open/Upcoming. |
| EXAM-02 | Token verification berfungsi (jika assessment pakai token) | Token modal di Assessment.cshtml: 6-digit input, auto-uppercase, AJAX POST ke VerifyToken. Server validates token match, sets TempData, redirects to StartExam. |
| EXAM-03 | Worker dapat memulai ujian, soal ditampilkan dengan benar | StartExam action: marks InProgress, loads questions via packages, shuffles options. View renders paginated questions with radio buttons. |
| EXAM-04 | Timer berjalan akurat, format tampilan benar (MM:SS/HH:MM:SS) | Wall-clock anchor timer (Date.now() based), drift-proof. Format MM:SS. Warning threshold 5 menit (text-danger). |
| EXAM-05 | Jawaban auto-save saat worker memilih opsi | Radio change → debounce 300ms → fetch POST SaveAnswer → retry 3x exponential backoff → queue jika gagal. Server: atomic upsert ke PackageUserResponses. |
| EXAM-06 | Navigasi antar halaman soal berfungsi (10 soal/halaman) | Client-side page switching. Wait pending saves sebelum navigasi (5s timeout). Session progress saved on every page change. |
| EXAM-07 | Network status indicator tampil di sticky header | `#hubStatusBadge` (SignalR: Connecting/Live/Reconnecting/Disconnected) dan `#networkStatusBadge` (Tersimpan/Menyimpan/Offline) di sticky header. |
| EXAM-08 | Tombol "Keluar Ujian" (abandon) berfungsi dengan benar | confirmAbandon() → confirm dialog → POST AbandonExam → status="Abandoned" → redirect Assessment list. Re-entry blocked (redirect + error message). |
</phase_requirements>

## Architecture Patterns

### Exam Flow Sequence (dari analisa kode)

```
Worker buka /CMP/Assessment
  → Lihat assessment cards (Open/Upcoming/InProgress)
  → Klik "Start Assessment"
    → Jika token required: modal token → AJAX POST VerifyToken → redirect StartExam
    → Jika non-token: confirm dialog → AJAX POST VerifyToken(token="") → redirect StartExam
  → GET StartExam/{id}
    → Server: mark InProgress, set StartedAt, load questions, shuffle options
    → Render exam page: timer, pagination, question cards
  → Worker jawab soal (radio click)
    → JS: debounce 300ms → POST SaveAnswer → upsert PackageUserResponses
    → Visual: save indicator, network badge update
  → Navigasi halaman: changePage() → wait pending saves → switch page div → saveSessionProgress()
  → Abandon: confirmAbandon() → POST AbandonExam → status=Abandoned → redirect
```

### Key URLs untuk Testing
- Assessment list: `http://10.55.3.3/KPB-PortalHC/CMP/Assessment`
- Start exam: `http://10.55.3.3/KPB-PortalHC/CMP/StartExam/{id}`

### Database Verification Queries

```sql
-- Cek assessment sessions untuk worker tertentu
SELECT Id, Title, Status, StartedAt, IsTokenRequired, DurationMinutes
FROM AssessmentSessions WHERE UserId = '{userId}';

-- Cek jawaban tersimpan (auto-save verification)
SELECT pur.AssessmentSessionId, pur.PackageQuestionId, pur.PackageOptionId, pur.SubmittedAt
FROM PackageUserResponses pur
WHERE pur.AssessmentSessionId = {sessionId}
ORDER BY pur.SubmittedAt;

-- Cek session progress (elapsed time + current page)
SELECT Id, ElapsedSeconds, LastActivePage, UpdatedAt
FROM AssessmentSessions WHERE Id = {sessionId};

-- Cek status abandon
SELECT Id, Status, StartedAt, UpdatedAt
FROM AssessmentSessions WHERE UserId = '{userId}' AND Status = 'Abandoned';
```

## Common Pitfalls

### Pitfall 1: Token TempData Hilang
**What goes wrong:** TempData hanya bertahan 1 request. Jika worker refresh setelah VerifyToken tapi sebelum StartExam, TempData hilang dan token harus dimasukkan ulang.
**How to avoid:** Ini by-design — InProgress sessions bypass token check (line 741-742: `assessment.StartedAt == null` guard). Hanya first entry yang butuh token.

### Pitfall 2: Assessment Belum Punya Soal
**What goes wrong:** Jika Phase 264 belum import soal dengan benar, StartExam menampilkan "No Questions Available".
**How to avoid:** Verifikasi Phase 264 selesai dengan benar sebelum mulai Phase 265.

### Pitfall 3: Pagination Tidak Tertest
**What goes wrong:** Jika assessment hanya punya <=10 soal, pagination tidak tertest karena hanya 1 halaman.
**How to avoid:** D-01 dari Phase 264 sudah specify buat assessment dengan jumlah soal berbeda. Pastikan minimal 1 assessment punya >10 soal.

### Pitfall 4: Abandon Lalu Re-entry
**What goes wrong:** Setelah abandon, worker mencoba masuk lagi.
**How to avoid:** Kode sudah handle (line 759-763): status "Abandoned" → redirect + error message. Ini yang perlu diverifikasi di D-08.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (browser-based) |
| Config file | N/A — UAT skenario dalam PLAN.md |
| Quick run command | Browser test di server dev |
| Full suite command | Semua skenario UAT per worker |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EXAM-01 | Assessment list dengan badge & jadwal | manual | Browser: /CMP/Assessment | N/A |
| EXAM-02 | Token verification | manual | Browser: token modal | N/A |
| EXAM-03 | Start exam, soal tampil | manual | Browser: /CMP/StartExam/{id} | N/A |
| EXAM-04 | Timer akurat | manual | Browser: observe timer | N/A |
| EXAM-05 | Auto-save jawaban | manual | Browser + DB query | N/A |
| EXAM-06 | Navigasi halaman | manual | Browser: next/prev/jump | N/A |
| EXAM-07 | Network status indicator | manual | Browser: observe badges | N/A |
| EXAM-08 | Abandon exam | manual | Browser + DB query | N/A |

### Sampling Rate
- **Per skenario:** Visual check + DB verification
- **Phase gate:** Semua 8 EXAM requirements PASS

### Wave 0 Gaps
None — ini UAT, bukan automated testing.

## Test Account Reference

| Worker | Email | Password | Assignment |
|--------|-------|----------|------------|
| Rino Prasetyo | rino.prasetyo@pertamina.com | (dari STATE.md) | Token assessment — happy path lengkap |
| Mohammad Arsyad | mohammad.arsyad@pertamina.com | Pertamina@2026 | Non-token assessment — happy path |
| Moch Widyadhana | moch.widyadhana@pertamina.com | Balikpapan@2026 | Abandon test only |

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` — Assessment, VerifyToken, StartExam, SaveAnswer, AbandonExam actions
- `Views/CMP/Assessment.cshtml` — Assessment lobby UI, token modal, tab filtering
- `Views/CMP/StartExam.cshtml` — Full exam UI with timer, auto-save, pagination, abandon
- `265-CONTEXT.md` — All user decisions and canonical references

## Metadata

**Confidence breakdown:**
- Exam flow code: HIGH — langsung baca source code
- Test scenarios: HIGH — berdasarkan analisa kode + user decisions
- Database queries: HIGH — berdasarkan model dan controller logic

**Research date:** 2026-03-27
**Valid until:** 2026-04-27 (stable — kode sudah mature)
