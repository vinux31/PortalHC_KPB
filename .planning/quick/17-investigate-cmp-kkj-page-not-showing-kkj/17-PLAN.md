---
phase: quick
plan: 17
type: execute
wave: 1
depends_on: []
files_modified: [Controllers/CMPController.cs]
autonomous: true
requirements: []
user_setup: []
must_haves:
  truths:
    - "KKJ files uploaded via Admin/KkjMatrix are visible on CMP/Kkj page"
    - "Role-based filtering works correctly (L1-L4 see all sections, L5-L6 see own section)"
    - "Files are filtered by BagianId and IsArchived=false"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "Kkj action with file query logic"
      contains: "public async Task<IActionResult> Kkj(string? section)"
  key_links:
    - from: "CMP/Kkj"
      to: "KkjFiles table"
      via: "EF Core query with BagianId and IsArchived filter"
      pattern: "_context.KkjFiles.Where(f => f.BagianId == selectedBagian.Id && !f.IsArchived)"
---

<objective>
Investigate why CMP/Kkj page is not showing KKJ Matrix files uploaded via Admin/KkjMatrix

Purpose: Verify the file query logic is correctly retrieving uploaded files
Output: Root cause identification and fix if needed
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md

# Key findings from code review:

From Controllers/CMPController.cs (lines 50-99):
- Kkj action queries files: `_context.KkjFiles.Where(f => f.BagianId == selectedBagian.Id && !f.IsArchived)`
- Files are ordered by UploadedAt descending
- Results passed to view via ViewBag.Files

From Views/CMP/Kkj.cshtml:
- Files rendered from ViewBag.Files (line 5)
- Empty state shown when !files.Any() (lines 54-63)
- Files displayed in table with download links (lines 90-115)
- Download action: Admin/KkjFileDownload (line 108)

From Views/Admin/KkjMatrix.cshtml:
- Same KkjFileDownload action used for admin file downloads (line 134)
- Admin uses tabbed interface with FilesByBagian dictionary
- Files filtered by BagianId in each tab

Potential root causes to investigate:
1. **IsArchived flag**: Files uploaded might have IsArchived=true by default
2. **BagianId mismatch**: Selected bagian ID doesn't match uploaded file's BagianId
3. **ViewBag type mismatch**: ViewBag.Files not properly cast to List<KkjFile>
4. **Role filtering**: L5/L6 users filtered to wrong bagian (currentUser.Unit vs bagian.Name)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Verify KkjFile upload logic sets IsArchived=false</name>
  <files>Controllers/AdminController.cs</files>
  <action>
  1. Search for KkjUpload action in AdminController
  2. Verify that new KkjFile records have IsArchived=false when created
  3. Check if there's any code that sets IsArchived=true by default
  4. If found, document the issue in task output
  5. Expected: KkjFile.IsArchived should be false for newly uploaded files

  Use grep to find the upload action:
  - Search for "KkjUpload" in Controllers/AdminController.cs
  - Read the action method and check KkjFile initialization
  </action>
  <verify>
    <automated>grep -n "IsArchived" Controllers/AdminController.cs | head -20</automated>
  </verify>
  <done>KkjFile upload logic verified — IsArchived flag correctly set to false</done>
</task>

<task type="checkpoint:human-verify">
  <what-built>Analysis of KkjFile upload and query logic</what-built>
  <how-to-verify>
  1. Check task output above for IsArchived flag status
  2. Review any other findings from the code investigation
  3. If bug found: approve fix implementation
  4. If no bug found: verify with actual data in browser:
     - Upload a test file in Admin/KkjMatrix
     - Navigate to CMP/Kkj
     - Confirm file appears or document specific failure
  </how-to-verify>
  <resume-signal>Type "approved" to proceed with fix, or describe actual observed behavior</resume-signal>
</task>

<task type="auto" tdd="true">
  <name>Task 2: Fix identified bug (if any)</name>
  <files>Controllers/AdminController.cs, Controllers/CMPController.cs</files>
  <behavior>
    If Task 1 reveals bug:
    - Fix IsArchived initialization in KkjUpload action (if needed)
    - Verify fix with targeted code check
    - Test by uploading file and querying via CMP/Kkj action

    If no bug found in code:
    - Create diagnostic query to check actual database state
    - Verify BagianId values match between KkjBagians and KkjFiles
    - Check for any orphaned files (BagianId pointing to non-existent bagian)
  </behavior>
  <action>
  Based on findings from Task 1:
  1. If IsArchived bug: Update KkjUpload to set IsArchived=false explicitly
  2. If BagianId mismatch: Add diagnostic logging to CMP/Kkj action
  3. If no code bug: Document database investigation query for user to run
  4. Commit fix with descriptive message
  </action>
  <verify>
    <automated>MISSING — Run manual browser test after fix: Upload file via Admin, verify it appears in CMP/Kkj</automated>
  </verify>
  <done>Bug fixed or diagnostic query provided — CMP/Kkj shows uploaded files correctly</done>
</task>

</tasks>

<verification>
After fix completion:
1. Upload a new KKJ file via Admin/KkjMatrix
2. Navigate to CMP/Kkj page
3. Verify the uploaded file appears in the file list
4. Verify download button works
5. Test with different user roles (Admin, HC, SrSpv)
</verification>

<success_criteria>
- CMP/Kkj page displays all active (non-archived) KKJ files for selected bagian
- Role-based filtering works correctly
- Download links function properly
- Empty state shows when no files exist for bagian
</success_criteria>

<output>
After completion, create `.planning/quick/17-investigate-cmp-kkj-page-not-showing-kkj/17-SUMMARY.md`
</output>
