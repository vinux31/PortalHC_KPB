# Tabel Issue Resolved — Pain Point Manual & Mapping ke Fitur

## Tujuan

Identifikasi 6 pain point sistemik dari workflow manual sebelum HC Portal, beserta mapping ke fitur HC Portal yang menyelesaikan pain point tersebut.

## Daftar Issue (A — F)

| Code | Issue | Deskripsi |
|------|-------|-----------|
| **A** | Tools Terfragmentasi | Workflow tersebar di 4-5 tools terpisah (Excel master + FleQi Quiz + Word + Email + WhatsApp + arsip fisik) tanpa integrasi otomatis. HC harus copy-paste antar tools dengan risiko inkonsistensi |
| **B** | Tidak Ada Single Source of Truth | Data sama disalin di beberapa Excel berbeda (per modul). Update di satu file tidak ter-refleksi di file lain, menimbulkan data mismatch yang menyulitkan rekonsiliasi |
| **C** | Tidak Ada Audit Trail | Perubahan data, approval coaching, generasi sertifikat dilakukan manual tanpa pencatatan siapa-apa-kapan. Sulit ditelusur saat audit eksternal atau investigasi insiden |
| **D** | Reporting Ad-Hoc & Non-Real-Time | Setiap permintaan laporan dari manajemen memerlukan HC melakukan pivot Excel ad-hoc dari beberapa file. Data bersifat snapshot (bukan real-time) dan formula bisa berbeda per laporan |
| **E** | Workflow Tanpa Tracking | Coaching, approval deliverable, dan progress IDP berjalan via koordinasi WhatsApp / email / lisan tanpa workflow terstruktur dengan status history |
| **F** | Renewal Sertifikat Reaktif | Tracking expired sertifikat dilakukan manual di Excel master. HC sering baru menyadari sertifikat expired saat audit, sehingga compliance posture menjadi reaktif |

## Mapping Issue ↔ Fitur HC Portal

| Issue | Fitur yang Menyelesaikan | Mekanisme |
|-------|--------------------------|-----------|
| **A** Tools Terfragmentasi | 01, 02, 03, 04, 05, 07 | Konsolidasi seluruh workflow ke 1 portal; eliminasi FleQi, Excel master per modul, paperwork, WhatsApp koordinasi |
| **B** Tidak Ada Single Source of Truth | 01, 03, 04, 06, 07 | DB SQL Server terpusat; entity pekerja, KKJ, assessment, IDP, deliverable saling-link via foreign key; perubahan di satu modul otomatis ter-refleksi di modul lain |
| **C** Tidak Ada Audit Trail | 01, 02, 05, 07 | Audit log mencatat seluruh aksi CRUD + login + impersonation; status history per deliverable/sertifikat; ASP.NET Identity dengan timestamp |
| **D** Reporting Ad-Hoc & Non-Real-Time | 01, 04, 06 | Analytics Dashboard real-time dengan filter periode/bagian; export Excel/PDF on-demand; KKJ Matrix digital auto-render |
| **E** Workflow Tanpa Tracking | 02, 03 | Workflow Coach → Reviewer (Atasan) → HC dengan status approval (Pending / Approved / Rejected) tersimpan di DB; histori coaching timeline |
| **F** Renewal Sertifikat Reaktif | 05 | Badge expiry (kuning ≤ 90 hari, merah ≥ expired) otomatis; menu Renewal Certificate dengan filter expiring; ekspor untuk perencanaan training tahunan |

## Matriks Coverage

| | 01 Assessment | 02 PROTON | 03 IDP | 04 KKJ | 05 Sertifikat | 06 Reporting | 07 Data Pekerja |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **A** Tools | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| **B** SSoT | ✓ | — | ✓ | ✓ | — | ✓ | ✓ |
| **C** Audit | ✓ | ✓ | — | — | ✓ | — | ✓ |
| **D** Reporting | ✓ | — | — | ✓ | — | ✓ | — |
| **E** Workflow | — | ✓ | ✓ | — | — | — | — |
| **F** Renewal | — | — | — | — | ✓ | — | — |

> Cell `✓` = fitur menyelesaikan / mitigate issue tersebut.

## Konsolidasi Risiko Sebelum vs Sesudah

| Risiko (Sebelum) | Status (Sesudah) |
|------------------|------------------|
| Data Excel rekap hilang/rusak | Mitigated — DB + backup |
| Inkonsistensi data antar modul | Mitigated — 1 DB referensial |
| Sertifikat kelewat expired tanpa renewal | Mitigated — badge expiry + menu Renewal |
| Approval coaching tidak terdokumentasi | Mitigated — workflow Coach→Atasan→HC dengan status history |
| Laporan ke manajemen tidak real-time | Mitigated — Analytics Dashboard |
| HC overload rekap manual | Mitigated — auto-grading, auto-rekap, auto-aggregate |
| Audit eksternal sulit (no trail) | Mitigated — audit log lengkap |
