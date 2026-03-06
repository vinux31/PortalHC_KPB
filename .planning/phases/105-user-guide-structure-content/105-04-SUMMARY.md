---
phase: 105
plan: 04
title: Split Existing Admin Module Guides into Focused Guides
subsystem: User Guide
tags: [content-organization, admin-panel, guide-splitting]
dependencyGraph:
  requires:
    - "105-02 (CMP module guides pattern)"
    - "105-03 (CDP/Account/Data module guides)"
  provides:
    - "105-04B (Add 4 new Admin guides to reach 12 total)"
  affects:
    - "Views/Home/GuideDetail.cshtml"
    - "Views/Home/Guide.cshtml"
techStack:
  added: []
  patterns: [accordion-navigation, step-variant-styling, focused-single-topic-guides]
keyFiles:
  created: []
  modified:
    - "Views/Home/GuideDetail.cshtml (split 2 combined guides into 5 focused guides)"
    - "Views/Home/Guide.cshtml (updated Admin card count from 12 to 8)"
decisions: []
metrics:
  durationSeconds: 34
  completedDate: 2026-03-06T04:38:13Z
---

# Phase 105 Plan 04: Split Existing Admin Module Guides into Focused Guides Summary

**One-liner:** Split 2 combined Admin guides (Kelola Ujian, Audit Log & Training Record) into 5 focused, single-topic guides for better usability and findability.

## Deviations from Plan

### Auto-fixed Issues

None - plan executed exactly as written.

## Implementation Summary

This plan focused on improving the Admin Panel module guide organization by splitting combined multi-topic guides into focused, single-topic guides. This enhances user experience by making specific features easier to find.

### Tasks Completed

#### Task 4.1: Split "Kelola Ujian" into Three Separate Guides
**Commit:** `935fe93`

Replaced the single combined guide "Cara Kelola Ujian (Buat Assessment, Monitoring, Soal Ujian)" with three focused guides:

1. **Cara Kelola Bank Soal (Paket Ujian)** - 4 steps
   - Create new exam packages
   - Import questions from Excel template
   - Define answer keys
   - Categorize exam packages

2. **Cara Membuat Jadwal Assessment** - 4 steps
   - Access Manage Assessment
   - Set duration and dates
   - Select exam packages
   - Assign participants

3. **Monitoring Ujian Berjalan** - 4 steps
   - Access Assessment Monitoring
   - Real-time monitoring
   - Force-close user if technical issues
   - View user progress

#### Task 4.2: Split "Audit Log & Training Record" into Two Guides
**Commit:** `97b9b4a`

Replaced the single combined guide "Audit Log & Training Record" with two focused guides:

1. **Cara Menambahkan Training Record** - 4 steps
   - Access Add Training
   - Input training data
   - Set category (mandatory/optional)
   - Upload certificate if available

2. **Cara Melihat Audit Log** - 4 steps
   - Open Audit Log
   - Filter by user/action/time
   - View IP address and sensitive activities
   - Investigate suspicious activity

#### Task 4.3: Update Admin Card Count
**Commit:** `9fb040d`

Updated the Admin module card count from "12 panduan tersedia" to "8 panduan tersedia" to reflect the current count after splitting guides. The count will increase to 12 in PLAN-04B when 4 new guides are added.

## Admin Panel Guide Structure (After PLAN-04)

The Admin Panel now has 8 focused guides:

1. Cara Kelola Pekerja (Tambah, Edit, Import Excel & Export) - 3 steps
2. Cara Upload File KKJ Matrix & CPDP Files - 2 steps
3. Cara Mapping Coach-Coachee - 2 steps
4. Cara Kelola Bank Soal (Paket Ujian) - 4 steps [NEW]
5. Cara Membuat Jadwal Assessment - 4 steps [NEW]
6. Monitoring Ujian Berjalan - 4 steps [NEW]
7. Cara Menambahkan Training Record - 4 steps [NEW]
8. Cara Melihat Audit Log - 4 steps [NEW]

## Technical Implementation

### File Changes

**Views/Home/GuideDetail.cshtml:**
- Split `admHeading4/admCollapse4` into `admHeading4/5/6` and `admCollapse4/5/6`
- Split `admHeading5/admCollapse5` into `admHeading7/8` and `admCollapse7/8`
- All guides maintain `step-variant-pink` class for visual consistency
- Each guide now has 3-4 detailed steps

**Views/Home/Guide.cshtml:**
- Updated Admin card count: "12 panduan tersedia" → "8 panduan tersedia"
- Maintains role-based visibility (Admin/HC only)

### Styling Consistency

All new guides follow the established pattern:
- Accordion button with `guide-list-btn btn-admin` classes
- Steps with `step-variant-pink` class for pink gradient styling
- Badge numbers for step sequencing
- Bold step titles with descriptive text

## Verification Criteria Met

- [x] Navigate to Home → Guide → click "Admin Panel" card (as Admin/HC user)
- [x] Verify 8 accordion items visible (after splitting)
- [x] Test each guide for content clarity
- [x] Confirm visual consistency across all Admin guides (pink gradient)
- [x] Verify role-based access (only Admin/HC can see)
- [x] Card count matches actual guide count (8)

## Success Metrics Achieved

- Admin module guide count: 8/12 complete (after PLAN-04, before PLAN-04B)
- Content covers existing Admin features with better organization
- Admin/HC can find specific feature guides easily
- No combined/multi-topic guides (each guide = one feature)

## Next Steps

PLAN-04B will add 4 new Admin guides to reach the target of 12 total guides:
1. Cara Kelola Pertanyaan Ujian (Manage Questions)
2. Cara Melihat dan Export Hasil Assessment
3. Cara Mengatur Role dan Akses User
4. Cara Melihat Dashboard Admin Statistik

## Self-Check: PASSED

**Files created:** None
**Files modified:**
- [x] C:\Users\Administrator\Desktop\PortalHC_KPB\Views\Home\GuideDetail.cshtml
- [x] C:\Users\Administrator\Desktop\PortalHC_KPB\Views\Home\Guide.cshtml

**Commits verified:**
- [x] 935fe93 - feat(105-04): split Kelola Ujian into 3 focused guides
- [x] 97b9b4a - feat(105-04): split Audit Log & Training Record into 2 guides
- [x] 9fb040d - feat(105-04): update Admin Panel card count to 8 guides

All changes committed successfully. Admin module now has 8 focused guides with consistent styling and improved findability.
