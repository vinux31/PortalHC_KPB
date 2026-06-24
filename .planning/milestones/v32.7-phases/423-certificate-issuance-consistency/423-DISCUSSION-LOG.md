# Phase 423: Certificate Issuance Consistency - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-24
**Phase:** 423-certificate-issuance-consistency
**Areas discussed:** Cakupan helper, Penomoran cert (atomik + namespace), Umur PendingGrading, Type x ValidUntil, Anti double-cert

---

## Cakupan helper (CERT-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Semua jalur (Rekomendasi) | 2 grading-time auto + manual AddManualAssessment; manual stop hardcode cert=true | ✓ |
| Grading-time auto saja | Hanya 2 jalur auto; manual terpisah | |

**User's choice:** Semua jalur
**Notes:** Pre-Test tetap ditolak di semua kasus. Manual tunduk aturan helper sama → FLD-5.2-02 tertutup.

---

## Penomoran cert — namespace (CERT-04 / FLD-5.2-07)

| Option | Description | Selected |
|--------|-------------|----------|
| Tolak manual menyerupai auto + collision-safe (Rekomendasi) | Free-text divalidasi tak boleh menyerupai KPB/{seq}/.../{year}; try/catch DbUpdateException → pesan ramah | ✓ |
| Prefix manual berbeda | Manual pakai penanda khusus (KPB-M/...) | |
| Pool sama, hanya collision-safe | Tanpa pemisahan namespace, cuma cegah 500 | |

**User's choice:** Tolak manual menyerupai auto + collision-safe
**Notes:** Tak ubah format nomor cert yang sudah tercetak.

---

## Penomoran cert — seq atomik (CERT-03 / GRD-08)

| Option | Description | Selected |
|--------|-------------|----------|
| Selesai tanpa cert + tandai utk HC manual (Rekomendasi) | Non-destruktif, worker tak terblok, HC follow-up | ✓ |
| Gagalkan finalize, minta ulangi | Sesi tak selesai sampai cert berhasil | |
| Selesai diam-diam tanpa cert (sekarang) | Log error saja | |

**User's choice:** Selesai tanpa cert + tandai utk HC manual
**Notes:** Harden retry + jitter di atas pola existing; tanpa tabel SEQUENCE baru (migration=FALSE).

---

## Umur PendingGrading — tampil di (CERT-07)

| Option | Description | Selected |
|--------|-------------|----------|
| EssayGrading + ManageAssessment (Rekomendasi) | Dua tempat HC bekerja | ✓ |
| Halaman EssayGrading saja | | |
| Daftar ManageAssessment saja | | |

**User's choice:** EssayGrading + ManageAssessment
**Notes:** TANPA auto-finalize.

---

## Umur PendingGrading — ambang (CERT-07)

| Option | Description | Selected |
|--------|-------------|----------|
| >3 hari kuning, >7 hari merah (Rekomendasi) | Penanda dini | ✓ |
| >7 hari kuning, >14 hari merah | Lebih longgar | |
| Tampilkan umur saja tanpa warna | | |

**User's choice:** >3 hari kuning, >7 hari merah

---

## Type x ValidUntil — tanggal dasar (CERT-06)

| Option | Description | Selected |
|--------|-------------|----------|
| CompletedAt / tanggal selesai (Rekomendasi) | Konsisten paritas grading existing | ✓ |
| Tanggal terbit cert (saat finalize) | | |
| Tanggal hari ini saat HC set (manual) | | |

**User's choice:** CompletedAt / tanggal selesai

---

## Type x ValidUntil — retroaktif (CERT-06 / FLD-5.2-09)

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya berlaku ke depan (Rekomendasi) | Tak sentuh data lama | ✓ |
| Tampilkan warning di UI, jangan ubah data | | |
| Perbaiki retroaktif via script one-off | | |

**User's choice:** Hanya berlaku ke depan
**Notes:** migration=FALSE; hindari mengubah cert yang sudah diterbitkan.

---

## Anti double-cert (CERT-05 / VAL-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Aktif=permanen/belum kedaluwarsa; renewal dikecualikan (Rekomendasi) | ValidUntil null OR >=hari ini; RenewsSessionId lolos; tak bisa di-bypass ConfirmDuplicateTitle | ✓ |
| Aktif=ada NomorSertifikat apa pun; renewal dikecualikan | Lebih ketat | |
| Blok semua duplikat tanpa pengecualian | Paling ketat | |

**User's choice:** Aktif=permanen/belum kedaluwarsa; renewal dikecualikan

---

## Claude's Discretion

- Penempatan/nama kelas helper baru `ShouldIssueCertificate`
- Bentuk penanda "cert gagal terbit utk HC manual"
- Jumlah retry & strategi jitter
- Format teks umur PendingGrading

## Deferred Ideas

- Cert/analytics atribusi per-unit (backlog v2, butuh migration)
- Sinkronisasi DeriveCertificateStatus baca Annual/3-Year (Phase 425 / backlog bila tak tertutup CERT-06)
