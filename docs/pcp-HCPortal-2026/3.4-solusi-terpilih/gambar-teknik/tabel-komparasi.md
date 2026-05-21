# Tabel Komparasi Master — Sebelum vs Sesudah Lintas 7 Fitur

## Tujuan

Ringkasan kuantitatif & kualitatif transformasi HC Portal. Angka kuantitatif = estimasi internal, refine pasca-implementasi.

## Tabel Per Fitur (Kuantitatif)

| # | Fitur | Step Sebelum | Step Sesudah | Δ Step | Tools Sebelum | Tools Sesudah | Δ Tools | Waktu Sebelum | Waktu Sesudah | Δ Waktu |
|---|-------|:-----------:|:------------:|:------:|:------------:|:------------:|:------:|:-------------:|:-------------:|:------:|
| 01 | Assessment Online | 6 | 2 | **-67%** | 4 | 1 | **-75%** | ~2 jam/paket | ~5 menit | **~95%** |
| 02 | PROTON Coaching | 5 | 2 | **-60%** | 4 | 1 | **-75%** | ~3 jam/bulan | ~10 menit | **~95%** |
| 03 | IDP / Plan | 4 | 1 | **-75%** | 3 | 1 | **-67%** | ~4 jam/siklus | ~15 menit | **~94%** |
| 04 | KKJ & Matriks | 4 | 1 | **-75%** | 3 | 1 | **-67%** | ~3 jam/request | real-time | **~99%** |
| 05 | Sertifikat & Renewal | 6 | 2 | **-67%** | 4 | 1 | **-75%** | ~10 menit/pekerja | instant | **~99%** |
| 06 | Reporting / Analytics | 5 | 2 | **-60%** | 3 | 1 | **-67%** | ~4 jam/laporan | ~10 menit | **~96%** |
| 07 | Data Pekerja | 6 | 1 | **-83%** | 5 | 1 | **-80%** | ~30 menit/pekerja | ~5 menit | **~83%** |

## Agregat Lintas Fitur

| Metrik | Range | Median |
|--------|-------|--------|
| Pengurangan step proses | -60% s.d. -83% | **-67%** |
| Pengurangan tools | -67% s.d. -80% | **-75%** |
| Pengurangan waktu (estimasi) | -83% s.d. -99% | **~95%** |

## Tabel Komparasi Aspek (Kualitatif)

| Aspek | Sebelum (Aktual) | Sesudah (Konsep Improvement) |
|-------|-------------------|------------------------------|
| Single Source of Truth | ❌ Tidak ada (data tersebar 4-5 Excel) | ✅ Ada — DB SQL Server terpusat |
| Tools yang Dipakai | ❌ 4-5 aplikasi (Excel, FleQi, Word, Email, WA) | ✅ 1 portal — HC Portal |
| Audit Trail | ❌ Tidak ada / catatan manual | ✅ Audit log lengkap (siapa-apa-kapan) |
| Real-Time Data | ❌ Snapshot manual | ✅ Real-time (DB + SignalR) |
| Workflow Approval | ❌ Lisan / WhatsApp, no trail | ✅ Terstruktur Coach→Atasan→HC dengan status history |
| Self-Service Manajemen | ❌ Bergantung rekap HC | ✅ Dashboard role-based |
| Versioning Dokumen | ❌ Manual rename file | ✅ Otomatis (timestamp + GUID) |
| Renewal Sertifikat | ❌ Reaktif (sering kelewat expired) | ✅ Proaktif (badge expiry + menu Renewal) |
| Compliance Posture | ❌ Reaktif | ✅ Proaktif + audit-ready |
| Bulk Operation | ❌ Copy-paste manual | ✅ Import Excel + validasi |

## Sumber Estimasi

- Step count: dihitung dari diagram swimlane file `flow-proses/01-07-*.md`
- Tools count: inventory aplikasi/medium pra-HC Portal
- Waktu: estimasi internal berdasarkan observasi proses HC

## Catatan untuk Reviewer

Angka kuantitatif bersifat **estimasi internal**. Tujuan: tunjukkan **magnitude order improvement** per fitur, bukan presisi absolut. Refine pasca-implementasi via:
- Audit log untuk durasi proses aktual
- Wawancara HC pasca-1 siklus
- Telemetri Hangfire
