# Tabel Improvement Kuantitatif — Ringkasan 7 Fitur

## Tujuan

Ringkasan kuantitatif impact HC Portal terhadap workflow manual sebelumnya, untuk 7 fitur impactful yang masuk cakupan §3.4. Angka berasal dari estimasi internal hasil inventory workflow + observasi proses HC; akan di-refine dengan data riil pasca-implementasi.

## Tabel Ringkasan Per Fitur

| # | Fitur | Step Sebelum | Step Sesudah | Δ Step | Tools Sebelum | Tools Sesudah | Δ Tools | Waktu Sebelum (estimasi) | Waktu Sesudah (estimasi) | Δ Waktu |
|---|-------|:-----------:|:------------:|:------:|:------------:|:------------:|:------:|:------------------------:|:-------------------------:|:------:|
| 01 | Assessment Online | 6 | 2 | **-67%** | 4 | 1 | **-75%** | ~2 jam/paket | ~5 menit/paket | **~95%** |
| 02 | PROTON Coaching | 5 | 2 | **-60%** | 4 | 1 | **-75%** | ~3 jam/bulan/coach | ~10 menit/bulan/coach | **~95%** |
| 03 | IDP / Plan | 4 | 1 | **-75%** | 3 | 1 | **-67%** | ~4 jam/siklus | ~15 menit/siklus | **~94%** |
| 04 | KKJ & Matriks | 4 | 1 | **-75%** | 3 | 1 | **-67%** | ~3 jam/request | real-time | **~99%** |
| 05 | Sertifikat & Renewal | 6 | 2 | **-67%** | 4 | 1 | **-75%** | ~10 menit/pekerja | instant | **~99%** |
| 06 | Reporting / Analytics | 5 | 2 | **-60%** | 3 | 1 | **-67%** | ~4 jam/laporan | ~10 menit | **~96%** |
| 07 | Data Pekerja | 6 | 1 | **-83%** | 5 | 1 | **-80%** | ~30 menit/pekerja | ~5 menit/pekerja | **~83%** |

## Agregat Lintas Fitur

| Metrik | Range | Median |
|--------|-------|--------|
| Pengurangan step proses | -60% s.d. -83% | **-67%** |
| Pengurangan tools | -67% s.d. -80% | **-75%** |
| Pengurangan waktu (estimasi) | -83% s.d. -99% | **~95%** |

## Aspek Kualitatif (Lintas Fitur)

| Aspek | Sebelum | Sesudah |
|-------|---------|---------|
| Single source of truth | Tidak ada (data tersebar 4-5 Excel) | Ada (1 DB) |
| Audit trail | Tidak ada / manual | Lengkap (audit log per aksi) |
| Real-time data | Snapshot manual | Real-time (DB + SignalR) |
| Self-service manajemen | Tidak (bergantung rekap HC) | Ya (dashboard role-based) |
| Workflow approval | Lisan / WA | Terstruktur (Coach → Atasan → HC) |
| Versioning dokumen | Manual (rename file) | Otomatis (timestamp + GUID) |
| Bulk operation | Copy-paste manual | Import Excel + validasi |
| Compliance posture | Reaktif (sering kelewat) | Proaktif (badge expiry, renewal menu) |

## Sumber Estimasi

- **Step count:** dihitung dari diagram swimlane file `01-07-flow-*.md`
- **Tools count:** inventory aplikasi/medium yang dipakai pra-HC Portal (Excel master per modul, FleQi Quiz, Word, Email Pertamina, WhatsApp, arsip fisik)
- **Waktu:** estimasi internal berdasarkan observasi proses HC + benchmark workflow umum manual

## Catatan untuk Reviewer

Angka kuantitatif bersifat **estimasi internal**, bukan hasil time-motion study formal. Tujuan utama adalah menunjukkan **magnitude order improvement** per fitur, bukan presisi absolut. Data riil akan dikumpulkan pasca-implementasi via:
- Audit log untuk durasi proses aktual
- Wawancara HC pasca-1 siklus
- Telemetri Hangfire untuk background job
