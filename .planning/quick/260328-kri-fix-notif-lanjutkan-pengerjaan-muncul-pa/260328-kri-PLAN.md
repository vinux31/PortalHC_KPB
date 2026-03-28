---
phase: quick
plan: 260328-kri
type: execute
wave: 1
depends_on: []
files_modified: [Controllers/CMPController.cs]
autonomous: true
must_haves:
  truths:
    - "Worker yang pertama kali masuk assessment baru TIDAK melihat notifikasi 'lanjutkan pengerjaan'"
    - "Worker yang resume assessment yang sudah pernah dikerjakan tetap melihat notifikasi 'lanjutkan pengerjaan'"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "StartExam with correct isResume logic"
      contains: "bool isResume = !justStarted"
  key_links: []
---

<objective>
Fix bug: notifikasi "lanjutkan pengerjaan" muncul saat worker pertama kali masuk assessment baru.

Purpose: StartExam sets StartedAt sebelum mengecek isResume, sehingga isResume selalu true.
Output: 1-line fix di CMPController.cs line 924
</objective>

<execution_context>
@C:\Users\Administrator\.claude\get-shit-done\workflows\execute-plan.md
@C:\Users\Administrator\.claude\get-shit-done\templates\summary.md
</execution_context>

<context>
@Controllers/CMPController.cs (lines 780-930 — StartExam method)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix isResume logic di StartExam</name>
  <files>Controllers/CMPController.cs</files>
  <action>
Di line 924, ganti:
```csharp
bool isResume = assessment.StartedAt != null;
```
menjadi:
```csharp
bool isResume = !justStarted;
```

Variabel `justStarted` sudah didefinisikan di line 782 (`bool justStarted = assessment.StartedAt == null`).
Setelah block line 783-788, StartedAt sudah di-set jadi != null, sehingga check lama selalu true.
Dengan `!justStarted`, isResume = false pada first visit dan true pada resume.
  </action>
  <verify>
    <automated>grep -n "bool isResume = !justStarted" Controllers/CMPController.cs</automated>
  </verify>
  <done>Line 924 menggunakan `!justStarted` bukan `assessment.StartedAt != null`. First visit tidak trigger resume notification.</done>
</task>

</tasks>

<verification>
grep -n "isResume" Controllers/CMPController.cs — hanya satu definisi isResume, menggunakan !justStarted
</verification>

<success_criteria>
- isResume = false saat first visit (justStarted = true)
- isResume = true saat resume (justStarted = false)
- Tidak ada notifikasi "lanjutkan pengerjaan" pada assessment baru
</success_criteria>

<output>
After completion, create `.planning/quick/260328-kri-fix-notif-lanjutkan-pengerjaan-muncul-pa/260328-kri-SUMMARY.md`
</output>
