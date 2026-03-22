# Research Summary: Gap Analysis — Assessment & Training Management System

**Domain:** Corporate HR/HC Assessment & Training Management System (Internal Portal Pertamina)
**Researched:** 2026-03-21
**Overall confidence:** HIGH (code inspection langsung + web research industry best practices)

---

## Executive Summary

Portal HC KPB sudah melampaui banyak baseline LMS korporat dalam hal exam engine: package-based shuffle, SignalR real-time monitoring, certificate generation dengan QuestPDF, renewal chain via Union-Find, dan audit log terpusat. Dibanding sistem TMS komersial, fitur-fitur teknis assessment-nya solid.

Gap terbesar ada di tiga area: (1) **analytics & insight** — data sudah ada tapi tidak bisa diakses; (2) **question bank architecture** — soal terikat per-session sehingga tidak bisa reuse dan tidak ada item quality analysis; (3) **compliance matrix** — sistem tahu siapa sudah training apa, tapi tidak ada definisi "training wajib per jabatan" sehingga HC tidak bisa otomatis detect gap.

Gap-gap ini bukan fitur mewah — mereka adalah capability operasional dasar yang HC perlukan untuk mengelola workforce competency dengan efektif. Tanpa analytics, HC harus export Excel manual untuk menjawab pertanyaan sederhana seperti "section mana paling banyak gagal assessment?". Tanpa compliance matrix, audit compliance adalah proses manual per-individu.

Sistem notifikasi sudah ada tapi terbatas: hanya notifikasi in-portal, tidak ada email, tidak ada eskalasi multi-level, dan tidak ada scheduled job untuk push reminder sertifikat yang akan expired.

---

## Key Findings

**Stack:** ASP.NET Core MVC + EF Core + SQLite + SignalR + QuestPDF — sudah tepat, tidak perlu diganti
**Architecture:** Monolith MVC dengan controller besar — pattern benar untuk internal portal skala ini
**Strengths:** Exam engine solid, certificate lifecycle lengkap, renewal chain cerdas, audit log ada
**Critical gap:** Question-per-session coupling menghalangi reuse dan analytics soal
**Critical gap:** Tidak ada training compliance matrix (jabatan → training wajib)
**Critical gap:** Analytics dashboard hampir tidak ada; data kaya tapi tidak bisa diakses HC
**Design issue:** UserResponse tidak menyimpan timestamp jawaban (anti-forensics)
**Design issue:** ElemenTeknis scoring tidak dipersist — hanya visual, hilang setelah session selesai
**Notification gap:** Tidak ada email reminder, tidak ada multi-stage escalation sertifikat expired

---

## Implications for Roadmap

Berdasarkan research, urutan prioritas yang disarankan:

1. **Analytics Dashboard (HC)** — effort medium, ROI tertinggi, data sudah ada di DB
   - Addresses: FEATURES.md §Reporting & Analytics
   - Avoids: PITFALLS.md §Dashboard Premature Complexity
   - Deliverables: pass rate per section/category, score distribution chart, expiry heatmap, sertifikat at-risk count

2. **Training Compliance Matrix** — kebutuhan operasional fundamental HC
   - Addresses: FEATURES.md §Compliance Tracking
   - Avoids: PITFALLS.md §Compliance Without Definition
   - Deliverables: model RequiredTraining (jabatan × training type), compliance gap view per worker/section

3. **Question Bank Library (Independen dari Session)** — prerequisite untuk item analysis dan assessment reuse
   - Addresses: FEATURES.md §Question Management
   - Avoids: PITFALLS.md §Session-Coupled Questions
   - Deliverables: QuestionBank model terpisah, import soal ke bank, assign dari bank ke assessment

4. **Notification Enhancement (Email + Escalation)** — sertifikat expired adalah liability HSE
   - Addresses: FEATURES.md §Certificate Expiry
   - Avoids: PITFALLS.md §Silent Expiry
   - Deliverables: scheduled job email 90/30/7 hari sebelum expired, eskalasi ke Section Head

**Phase ordering rationale:**
- Analytics dulu karena tidak butuh perubahan schema — hanya query + view baru, zero risk
- Compliance Matrix butuh model baru (RequiredTraining) tapi tidak merusak yang ada
- Question Bank butuh migrasi schema dan ada risiko ke exam engine — perlu lebih hati-hati
- Notification Enhancement bisa dikerjakan paralel dengan item manapun

**Research flags untuk phases:**
- Question Bank: perlu riset migration strategy (legacy AssessmentQuestion vs PackageQuestion coexist + exam engine read path)
- Compliance Matrix: perlu diskusi dengan user tentang definisi "wajib" — by position? by unit? by category?
- Analytics: standar query aggregation, tidak perlu riset tambahan
- Notification: perlu cek SMTP config di appsettings + apakah ada email service yang sudah dikonfigurasi

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Strengths Assessment | HIGH | Berdasarkan code inspection langsung semua model dan controller |
| Gap Identification | HIGH | Gap diidentifikasi dari struktur model yang ada + industry standards |
| Feature Prioritization | MEDIUM | Asumsi tentang operasional HC; perlu validasi volume data aktual |
| Architecture Implications | HIGH | Monolith MVC pattern sudah established, extension pattern jelas |
| Pitfalls | MEDIUM | Beberapa pitfalls bersifat asumsi operasional, belum ada feedback dari HC langsung |

---

## Gaps to Address (Pre-Planning)

- Volume data production belum diketahui (berapa assessment sessions aktif? berapa workers?)
- Apakah HC sudah punya spreadsheet "training wajib per jabatan" yang bisa jadi seed data compliance matrix?
- SMTP email sudah dikonfigurasi di production? (cek appsettings.Production.json)
- Apakah user pernah complaint tentang tidak bisa "cari soal yang sudah pernah dipakai"?
- Kapasitas HC untuk maintain question bank — perlu workflow review/approval soal atau tidak?
