---
phase: quick-16
plan: 16
type: execute
wave: 1
depends_on: []
files_modified: []
autonomous: true
requirements: [QUICK-16]
must_haves:
  truths:
    - "Semua menu title di Kelola Data Hub sudah terdaftar lengkap"
  artifacts: []
  key_links: []
---

<objective>
Membaca dan mendaftar semua nama title menu yang ada di Kelola Data Hub (Views/Admin/Index.cshtml).

Purpose: Memberikan inventaris lengkap menu card di hub Kelola Data untuk keperluan QA, penamaan, atau rencana pengembangan.
Output: Daftar lengkap title menu per section.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@Views/Admin/Index.cshtml
</context>

<tasks>

<task type="auto">
  <name>Task 1: Daftar Semua Title Menu di Kelola Data Hub</name>
  <files>Views/Admin/Index.cshtml</files>
  <action>
Baca Views/Admin/Index.cshtml dan kompilasi daftar semua title menu card per section.

Hasil yang diharapkan (sudah dianalisis dari file):

**Section A — Data Management**
1. Manajemen Pekerja — Tambah, edit, hapus, dan kelola data pekerja sistem (selalu tampil)
2. KKJ Matrix — Upload dan kelola dokumen KKJ Matrix (PDF/Excel) per bagian (Admin/HC only)
3. CPDP File Management — Upload dan kelola dokumen CPDP per bagian (PDF/Excel) (Admin/HC only)
4. Silabus & Coaching Guidance — Kelola silabus Proton dan file coaching guidance (selalu tampil)

**Section B — Proton**
5. Coach-Coachee Mapping — Atur assignment coach ke coachee (Admin/HC only)
6. Deliverable Progress Override — Override status progress deliverable (selalu tampil)

**Section C — Assessment & Training**
7. Manage Assessment & Training — Kelola assessment dan training record pekerja (Admin/HC only)
8. Assessment Monitoring — Pantau progress assessment real-time (Admin/HC only)
9. Audit Log — Lihat riwayat aktivitas pengelolaan assessment oleh Admin dan HC (Admin/HC only)

Konfirmasi daftar ini cocok dengan Views/Admin/Index.cshtml dan laporkan hasilnya ke user.
  </action>
  <verify>Semua `&lt;span class="fw-bold"&gt;` di Index.cshtml sudah tercantum dalam daftar</verify>
  <done>9 menu card terdaftar dengan nama title, deskripsi, section, dan visibilitas role</done>
</task>

</tasks>

<verification>
grep "fw-bold" Views/Admin/Index.cshtml — harus mengembalikan 9 baris (tidak termasuk heading section A/B/C)
</verification>

<success_criteria>
Semua 9 menu title terdaftar dengan: nama title, section (A/B/C), dan keterangan visibilitas role (selalu tampil vs Admin/HC only).
</success_criteria>

<output>
Tidak perlu membuat SUMMARY.md — ini quick read task. Sampaikan daftar langsung ke user sebagai output.
</output>
