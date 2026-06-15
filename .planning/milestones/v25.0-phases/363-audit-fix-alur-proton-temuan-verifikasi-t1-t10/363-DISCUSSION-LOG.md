# Phase 363: Audit Fix Alur PROTON (T1-T10) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
**Areas discussed:** Strategi anti-drift T1/T2/T7/T8, T3 gate reaktivasi, T4 penanda nonaktif, Triase T5/T6/T9/T10
**Pra-diskusi:** scout workflow 4-agent paralel verifikasi state kode terkini (10/10 temuan masih valid pasca 360/361/362)

---

## Strategi anti-drift T1/T2/T7

| Option | Description | Selected |
|--------|-------------|----------|
| Helper bersama | Extract approve-core + reject-core dipanggil kedua endpoint — drift mati permanen, butuh test paritas | ✓ |
| Patch per-endpoint | Port logic ke FromProgress saja — diff kecil, drift bisa kambuh | |

| Option | Description | Selected |
|--------|-------------|----------|
| Reject + resubmit | Reset HCApprovalStatus di reject DAN kedua jalur resubmit (belt-and-braces) | ✓ |
| Cukup di reject | Full chain reset hanya di endpoint reject | |

---

## T3 gate reaktivasi

| Option | Description | Selected |
|--------|-------------|----------|
| Gate reaktivasi | Jalur reaktivasi jalankan year gate + exempt, pesan blocked pola 359 | ✓ |
| By-design + test | Reaktivasi dianggap "pernah valid", dokumentasi + pinning test | |

| Option | Description | Selected |
|--------|-------------|----------|
| Exempt stempel permanen | Cek Origin='Bypass' pada assignment yang direaktivasi (konsisten 360 D-04) | ✓ |
| Hanya exempt existing | Pakai isExemptFromCrossYear apa adanya — bisa false-block worker bypass sah | |

---

## T4 penanda nonaktif

| Option | Description | Selected |
|--------|-------------|----------|
| Surface warning | EnsureAsync tetap strict; miss → AuditLogs + notif HC; backfill jalur tambal | ✓ |
| Longgarkan EnsureAsync | Auto-terbit ke assignment nonaktif — bisa salah untuk worker yang dikeluarkan | |
| By-design | Warn log existing saja — tetap senyap bagi admin | |

---

## T8 evidence

| Option | Description | Selected |
|--------|-------------|----------|
| Append history saja | Samakan UploadEvidence; file fisik keep (kebijakan E10) | ✓ |
| Append + hapus file lama | Disk bersih tapi ubah kebijakan keep-evidence | |

---

## T5 Belum Mulai (produk)

| Option | Description | Selected |
|--------|-------------|----------|
| Tampilkan coachee belum mulai | Query include mapping-aktif-tanpa-assignment; badge + filter berfungsi | ✓ |
| Hapus dead branch | Cleanup minimal, scope page tetap | |

---

## T6 ValidUntil

| Option | Description | Selected |
|--------|-------------|----------|
| Buang hardcode | Regrade berhenti set ValidUntil — paritas jalur normal | ✓ |
| Pertahankan +3thn | Asimetri tetap, by-design declare | |

---

## T9 guard

| Option | Description | Selected |
|--------|-------------|----------|
| Guard log-warn | Warning eksplisit 2 titik saat Urutan tidak kontigu; gate tetap | ✓ |
| Accept by-design | Nol perubahan, catat alasan | |
| Guard throw/blok | Tolak operasi — risiko blok data sah | |

---

## T10 backfill

| Option | Description | Selected |
|--------|-------------|----------|
| By-design, dokumentasi | Komentar kode + catatan FINDINGS, nol perubahan logic | ✓ |
| Tambah year-gate check | Melawan rasional backfill sendiri | |

---

## Claude's Discretion

- Bentuk helper D-01 (private method vs service class)
- Nama tipe notif T4 + template
- Pembagian plan/wave + urutan fix (CDPController 5 temuan)
- Strategi test paritas + scope e2e/UAT

## Deferred Ideas

- Hapus file fisik evidence lama (kebijakan keep E10 dipertahankan)
- Konsolidasi pasangan HCReview/upload — hanya kalau drift ditemukan
- Year-gate di backfill — ditolak by-design
