---
phase: 353
slug: admin-backend-gambar-crud-sync-atomic-delete
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-08
---

# Phase 353 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) — HcPortal.Tests |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Image"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Estimated runtime** | ~22s (full, 120 baseline) |

---

## Sampling Rate

- **After every task commit:** Run quick command (`--filter ~Image` + relevant new test class)
- **After every plan wave:** Run full suite (`dotnet test`)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error + Playwright UAT (Phase 355 dedicated, smoke here)
- **Max feedback latency:** ~25s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 353-W0 | 00 | 0 | scaffold | — | N/A | unit-stub | `dotnet build` | ❌ W0 | ⬜ pending |
| 353-sync | — | — | SYN-01 | T-353-sync | Post copy ImagePath+ImageAlt (string), no file op | unit | `dotnet test --filter ~SyncImage` | ❌ W0 | ⬜ pending |
| 353-refcount | — | — | SYN-02 | T-353-del | File.Delete only when no other row shares path (Pre↔Post safe) | unit | `dotnet test --filter ~RefCount` | ❌ W0 | ⬜ pending |
| 353-delpkg | — | — | SYN-02 | T-353-del | DeletePackage collects paths pre-tx, File.Delete post-commit + ref-count | unit | `dotnet test --filter ~DeletePackageImage` | ❌ W0 | ⬜ pending |
| 353-upload | — | — | IMG-01/02/04 | T-353-up | ValidateImageFile gate (reuse Phase 352 — already covered) | unit | `dotnet test --filter ~ValidateImageFile` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/PackageImageSyncTests.cs` — SYN-01: SyncPackagesToPost copies ImagePath+ImageAlt to Post question+options (string copy, same path)
- [ ] `HcPortal.Tests/PackageImageDeleteTests.cs` — SYN-02 ref-count: helper returns false (skip File.Delete) when another PackageQuestion/PackageOption row shares the path; true when last reference; DeletePackage path-collect correctness
- [ ] Reuse `MakeFile` / temp-dir helpers from existing `FileUploadHelperTests.cs`

*Upload validation (IMG-04) already covered by Phase 352 FileUploadHelperTests — no new Wave 0 needed there.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Form upload gambar soal+opsi end-to-end (multipart) | IMG-01/02/03 | Needs running app + browser file picker | Playwright/manual: ManagePackageQuestions → tambah soal + pilih gambar soal+opsi A → submit → cek DB ImagePath set + file di /uploads/questions/{packageId}/ |
| Edit thumbnail prefill + replace + hapus checkbox | IMG-05/06/07, D-05 | Browser file-input behavior (no prefill) + JS | Edit soal bergambar → thumbnail tampil → centang hapus / pilih file baru → submit → verifikasi konflik new-file-wins |
| Client-side FileReader instant thumbnail | D-07 | JS DOM runtime | Pilih file di form → thumbnail muncul sebelum submit |
| _PreviewQuestion render `<img>` | RND-04 | Razor render in browser | Preview soal bergambar → img soal + opsi tampil (img-fluid, alt) |
| Validation error alert (non-image/>5MB) | IMG-04, D-08 | TempData flash in browser | Upload PDF / >5MB → alert merah atas + form repopulate |
| Pre→Post shared-file: delete Pre image does NOT break Post | SYN-01/02, D-10 | Cross-session DB + disk state | Sync Pre→Post (SamePackage) → hapus gambar Pre soal → cek file masih ada (Post pakai) + Post img utuh |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (sync-copy + ref-count/DeletePackage)
- [ ] No watch-mode flags
- [ ] Feedback latency < 25s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
