# Phase 392: Perbaikan CreateWorker + Audit Field - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input planning/research/execution. Keputusan ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-17
**Phase:** 392-perbaikan-createworker-audit-field
**Areas discussed:** UX field AD pasca-buka, Verifikasi Playwright mode AD, Cleanup DB test create, Kedalaman validasi inline, (audit) Login AD, Validasi live, Scope EditWorker

---

## Gray area selection (round 1)

User memilih SEMUA 4 area: UX field AD pasca-buka, Verifikasi Playwright mode AD, Cleanup DB test create, Kedalaman validasi inline.

## Round 2 — deep-dive (4 pertanyaan)

User awalnya "tidak paham" → dijelaskan ulang bahasa sederhana. Lalu menyetujui keempat rekomendasi.

### UX field AD pasca-buka (D-01/D-02)
| Opsi | Deskripsi | Dipilih |
|------|-----------|---------|
| Hapus bg-light + reword teks | Field tampak editable + teks akurat | ✓ |
| Hapus bg-light + hapus teks | Bersih total, hilang konteks AD | |
| Hanya hapus readonly | Diff minimal, UX kontradiktif | |
**Pilihan user:** ikut rekomendasi (hapus bg-light + reword). Diperhalus saat ronde audit jadi "Isi sesuai akun AD Pertamina; diselaraskan saat login".

### Verifikasi Playwright mode AD (D-06)
**Pilihan user:** e2e AD-off + guard markup (proporsional, hindari NTLM loopback). Disempurnakan oleh kritik F-NEW-04 → guard = **source-grep statik** (bukan runtime, yang lolos hampa di AD-off).

### Cleanup DB test create (D-07)
| Opsi | Deskripsi | Dipilih |
|------|-----------|---------|
| Email unik + hapus baris | Ringan, self-cleaning | ✓ |
| Snapshot + restore penuh | Strict CLAUDE.md, berat | |
**Pilihan user:** email-unik + hapus. Disempurnakan F-NEW-07 → teardown via DeleteWorker POST (cascade role), jalan walau test gagal.

### Kedalaman validasi inline (D-04)
| Opsi | Deskripsi | Dipilih |
|------|-----------|---------|
| Span saja (view-only murni) | Surface error server, tetap optional | ✓ |
| Span + wajibkan Bagian/Unit | Ubah perilaku, scope creep | |
**Pilihan user:** span saja, field tetap optional. Audit mengoreksi: hanya 4 field org (+opsional Role) yang kurang span; sisanya sudah ada.

---

## Round 3 — temuan audit multi-agent (4 agen, 31 finding)

Workflow `createworker-completeness-audit` (view-audit + controller-contract + login-viability + adversarial critic). Koreksi kunci: FullName SUDAH [Required]; Email/Nama/NIP/Tanggal/Password SUDAH punya span; client-validation BISA view-only.

### Login AD (batas scope)
**Pertanyaan:** form fix tak otomatis bikin login AD jalan (login divalidasi server AD).
**Jawaban user (free-text):** "sudah pasti bikin akun yang sudah ada di AD pertamina" → akun selalu untuk pekerja yang sudah eksis di AD; batas diterima; login jalan asal Email cocok akun AD. Aksi: reword teks info AD jadi pengingat cocokkan akun AD (fold ke D-02).

### Validasi live (D-05)
| Opsi | Deskripsi | Dipilih |
|------|-----------|---------|
| Ya, aktifkan validasi live | Pesan error muncul saat mengetik (view-only via _ValidationScriptsPartial) | ✓ |
| Tidak, cukup setelah Simpan | Server-only | |
**Pilihan user:** aktifkan validasi live.

### Scope EditWorker (D-08)
| Opsi | Deskripsi | Dipilih |
|------|-----------|---------|
| CreateWorker saja | Sesuai roadmap; Edit-lock defensible | ✓ |
| Perbaiki keduanya | Perluas scope ke EditWorker.cshtml | |
**Pilihan user:** CreateWorker saja.

---

## Claude's Discretion
- Wording final teks info AD (D-02); span Role (D-04, error unreachable); format email-unik + teardown (D-07).

## Deferred Ideas
- AD provisioning (OUT of scope); EditWorker readonly identik (sengaja skip); shared-cascade.js placeholder hard-coded; email↔AD-username mapping risk; todo cleanup Phase 367 (not folded).
