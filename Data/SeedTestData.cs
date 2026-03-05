using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

namespace HcPortal.Data
{
    /// <summary>
    /// Comprehensive test data seeding for CDP (Competency Development Platform) flows
    /// Covers all 5 role levels: Coachee, Coach, SrSpv, SectionHead, HC/Admin
    /// Phase 94-00: CDP Section Audit precondition
    /// </summary>
    public static class SeedTestData
    {
        /// <summary>
        /// Seeds comprehensive test data for all CDP workflows
        /// Call from AdminController action or Program.cs initialization
        /// </summary>
        public static async Task SeedCDPTestData(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            string actorId,
            ILogger? logger = null)
        {
            try
            {
                // Check if already seeded
                if (await context.ProtonTrackAssignments.AnyAsync())
                {
                    logger?.LogInformation("SeedCDPTestData: Already seeded, skipping");
                    return;
                }

                var now = DateTime.UtcNow;
                var random = new Random(42); // Fixed seed for reproducibility

                // ========================================
                // STEP 1: Verify/Create Users for All Roles
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 1 - Ensuring users exist for all roles");

                var allUsers = await context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.RoleLevel)
                    .ToListAsync();

                if (allUsers.Count < 10)
                {
                    throw new InvalidOperationException(
                        $"SeedCDPTestData: Need at least 10 active users. Found: {allUsers.Count}. " +
                        "Please create users via Admin/ManageWorkers first.");
                }

                // Find or use existing users for each role
                var adminUsers = allUsers.Where(u => u.RoleLevel <= 2).Take(2).ToList();
                var hcUsers = allUsers.Where(u => u.RoleLevel == 2).Take(2).ToList();
                var sectionHeadUsers = allUsers.Where(u => u.RoleLevel == 5).Take(2).ToList();
                var coachUsers = allUsers.Where(u => u.RoleLevel == 3 || u.RoleLevel == 4).Take(3).ToList();
                var coacheeUsers = allUsers.Where(u => u.RoleLevel == 6).Take(5).ToList();

                if (!adminUsers.Any() || !hcUsers.Any() || !sectionHeadUsers.Any() ||
                    !coachUsers.Any() || !coacheeUsers.Any())
                {
                    throw new InvalidOperationException(
                        "SeedCDPTestData: Missing users for one or more roles. " +
                        $"Admin: {adminUsers.Count}, HC: {hcUsers.Count}, SectionHead: {sectionHeadUsers.Count}, " +
                        $"Coach: {coachUsers.Count}, Coachee: {coacheeUsers.Count}");
                }

                // Log user assignments
                logger?.LogInformation($"SeedCDPTestData: Found {coacheeUsers.Count} coachees, {coachUsers.Count} coaches, " +
                    $"{sectionHeadUsers.Count} section heads, {hcUsers.Count} HC users, {adminUsers.Count} admin users");

                // ========================================
                // STEP 2: Find Existing Proton Tracks with Deliverables
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 2 - Finding Proton tracks with deliverables");

                var tracks = await context.ProtonTracks
                    .Include(t => t.KompetensiList)
                        .ThenInclude(k => k.SubKompetensiList)
                            .ThenInclude(s => s.Deliverables)
                    .Where(t => t.KompetensiList
                        .Any(k => k.SubKompetensiList
                            .Any(s => s.Deliverables.Any())))
                    .OrderBy(t => t.Urutan)
                    .Take(3)
                    .ToListAsync();

                if (!tracks.Any())
                {
                    throw new InvalidOperationException(
                        "SeedCDPTestData: No Proton tracks with deliverables found. " +
                        "Please add Silabus data via Admin/ProtonData first.");
                }

                // Get all deliverables from selected tracks
                var allDeliverables = tracks
                    .SelectMany(t => t.KompetensiList)
                    .SelectMany(k => k.SubKompetensiList)
                    .SelectMany(s => s.Deliverables)
                    .ToList();

                logger?.LogInformation($"SeedCDPTestData: Found {tracks.Count} tracks with {allDeliverables.Count} deliverables");

                // ========================================
                // STEP 3: Create ProtonTrackAssignments
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 3 - Creating track assignments");

                var assignments = new List<ProtonTrackAssignment>();
                var bagianOptions = new[] { "RFCC", "DHT/HMU", "NGP", "GAST" };
                var unitOptions = new[] { "Operations", "Maintenance", "Technical Services" };

                // Assign tracks to coachees with different Bagian/Unit combinations
                for (int i = 0; i < coacheeUsers.Count; i++)
                {
                    var coachee = coacheeUsers[i];
                    var track = tracks[i % tracks.Count];

                    // Update coachee's Bagian/Unit to match assignment
                    coachee.Section = bagianOptions[i % bagianOptions.Length];
                    coachee.Unit = unitOptions[i % unitOptions.Length];

                    var assignment = new ProtonTrackAssignment
                    {
                        CoacheeId = coachee.Id,
                        AssignedById = adminUsers.First().Id,
                        ProtonTrackId = track.Id,
                        IsActive = true,
                        AssignedAt = now.AddMinutes(-random.Next(1, 100))
                    };
                    assignments.Add(assignment);
                }

                // Create some inactive assignments for testing IsActive filtering
                var inactiveAssignment = new ProtonTrackAssignment
                {
                    CoacheeId = coacheeUsers.First().Id,
                    AssignedById = adminUsers.First().Id,
                    ProtonTrackId = tracks.First().Id,
                    IsActive = false,
                    AssignedAt = now.AddDays(-30)
                };
                assignments.Add(inactiveAssignment);

                await context.ProtonTrackAssignments.AddRangeAsync(assignments);
                await context.SaveChangesAsync();

                logger?.LogInformation($"SeedCDPTestData: Created {assignments.Count} track assignments");

                // ========================================
                // STEP 4: Create ProtonDeliverableProgress Records
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 4 - Creating deliverable progress records");

                var activeAssignments = assignments.Where(a => a.IsActive).ToList();
                var progressRecords = new List<ProtonDeliverableProgress>();

                // For each active assignment, create progress records with various statuses
                foreach (var assignment in activeAssignments)
                {
                    var coacheeId = assignment.CoacheeId;
                    var trackId = assignment.ProtonTrackId;

                    // Get deliverables for this track
                    var trackDeliverables = allDeliverables
                        .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId)
                        .ToList();

                    // Create progress records with different statuses
                    var statusPermutations = new[]
                    {
                        // Status, SrSpvStatus, ShStatus, HCStatus, HasEvidence
                        ("Pending", "Pending", "Pending", "Pending", false),
                        ("Submitted", "Pending", "Pending", "Pending", true),
                        ("Submitted", "Approved", "Pending", "Pending", true),
                        ("Submitted", "Approved", "Approved", "Pending", true),
                        ("Approved", "Approved", "Approved", "Reviewed", true),
                        ("Rejected", "Rejected", "Pending", "Pending", true),
                        ("Rejected", "Approved", "Rejected", "Pending", true)
                    };

                    // Create at least one of each status type
                    for (int i = 0; i < Math.Min(statusPermutations.Length, trackDeliverables.Count); i++)
                    {
                        var (status, srSpvStatus, shStatus, hcStatus, hasEvidence) = statusPermutations[i];
                        var deliverable = trackDeliverables[i];

                        var progress = new ProtonDeliverableProgress
                        {
                            CoacheeId = coacheeId,
                            ProtonDeliverableId = deliverable.Id,
                            Status = status,
                            SrSpvApprovalStatus = srSpvStatus,
                            ShApprovalStatus = shStatus,
                            HCApprovalStatus = hcStatus,
                            CreatedAt = now.AddHours(-random.Next(1, 100))
                        };

                        // Set timestamps based on status
                        if (status == "Submitted" || status == "Approved" || status == "Rejected")
                        {
                            progress.SubmittedAt = now.AddHours(-random.Next(1, 50));
                        }

                        if (srSpvStatus == "Approved")
                        {
                            progress.SrSpvApprovedById = coachUsers[random.Next(coachUsers.Count)].Id;
                            progress.SrSpvApprovedAt = now.AddHours(-random.Next(1, 30));
                        }
                        else if (srSpvStatus == "Rejected")
                        {
                            progress.SrSpvApprovedById = coachUsers[random.Next(coachUsers.Count)].Id;
                            progress.SrSpvApprovedAt = now.AddHours(-random.Next(1, 30));
                            progress.RejectionReason = "SrSpv rejection: Quality not sufficient";
                        }

                        if (shStatus == "Approved")
                        {
                            progress.ShApprovedById = sectionHeadUsers[random.Next(sectionHeadUsers.Count)].Id;
                            progress.ShApprovedAt = now.AddHours(-random.Next(1, 20));
                        }
                        else if (shStatus == "Rejected")
                        {
                            progress.ShApprovedById = sectionHeadUsers[random.Next(sectionHeadUsers.Count)].Id;
                            progress.ShApprovedAt = now.AddHours(-random.Next(1, 20));
                            progress.RejectionReason = "Section Head rejection: Needs more detail";
                        }

                        if (hcStatus == "Reviewed")
                        {
                            progress.HCReviewedById = hcUsers[random.Next(hcUsers.Count)].Id;
                            progress.HCReviewedAt = now.AddHours(-random.Next(1, 10));
                        }

                        if (status == "Approved")
                        {
                            progress.ApprovedById = adminUsers.First().Id;
                            progress.ApprovedAt = now.AddHours(-random.Next(1, 5));
                        }

                        if (hasEvidence)
                        {
                            progress.EvidenceFileName = $"evidence_{progress.Id}.pdf";
                            progress.EvidencePath = $"/uploads/evidence/{progress.Id}/evidence_{progress.Id}.pdf";
                        }

                        progressRecords.Add(progress);
                    }

                    // Add some additional Pending records
                    var remainingDeliverables = trackDeliverables.Skip(statusPermutations.Length);
                    foreach (var deliverable in remainingDeliverables.Take(3))
                    {
                        progressRecords.Add(new ProtonDeliverableProgress
                        {
                            CoacheeId = coacheeId,
                            ProtonDeliverableId = deliverable.Id,
                            Status = "Pending",
                            SrSpvApprovalStatus = "Pending",
                            ShApprovalStatus = "Pending",
                            HCApprovalStatus = "Pending",
                            CreatedAt = now.AddHours(-random.Next(1, 100))
                        });
                    }
                }

                await context.ProtonDeliverableProgresses.AddRangeAsync(progressRecords);
                await context.SaveChangesAsync();

                logger?.LogInformation($"SeedCDPTestData: Created {progressRecords.Count} progress records");

                // ========================================
                // STEP 5: Create Evidence Files
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 5 - Creating evidence files");

                var evidenceFolder = Path.Combine(env.WebRootPath, "uploads", "evidence");
                Directory.CreateDirectory(evidenceFolder);

                var progressWithEvidence = progressRecords.Where(p => !string.IsNullOrEmpty(p.EvidencePath)).ToList();
                var evidenceCreated = 0;

                foreach (var progress in progressWithEvidence)
                {
                    var progressFolder = Path.Combine(evidenceFolder, progress.Id.ToString());
                    Directory.CreateDirectory(progressFolder);

                    var evidencePath = Path.Combine(progressFolder, progress.EvidenceFileName ?? "evidence.pdf");

                    if (!System.IO.File.Exists(evidencePath))
                    {
                        // Create a dummy PDF file (just text for now)
                        await System.IO.File.WriteAllTextAsync(evidencePath,
                            $"Test Evidence File for Deliverable Progress {progress.Id}\n" +
                            $"Coachee: {progress.CoacheeId}\n" +
                            $"Status: {progress.Status}\n" +
                            $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                            $"This is a test file seeded by SeedCDPTestData for Phase 94 QA");
                        evidenceCreated++;
                    }
                }

                logger?.LogInformation($"SeedCDPTestData: Created {evidenceCreated} evidence files");

                // ========================================
                // STEP 6: Create CoachingGuidanceFile Records
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 6 - Creating coaching guidance files");

                var guidanceFolder = Path.Combine(env.WebRootPath, "uploads", "guidance");
                Directory.CreateDirectory(guidanceFolder);

                var guidanceFiles = new List<CoachingGuidanceFile>();
                var guidanceFileCount = 0;

                // Create guidance files for different Bagian/Unit/Track combinations
                foreach (var track in tracks)
                {
                    foreach (var bagian in bagianOptions.Take(2))
                    {
                        foreach (var unit in unitOptions.Take(2))
                        {
                            var fileName = $"guidance_{track.TrackType}_{bagian}_{unit}.pdf";
                            var filePath = Path.Combine(guidanceFolder, fileName);

                            if (!System.IO.File.Exists(filePath))
                            {
                                await System.IO.File.WriteAllTextAsync(filePath,
                                    $"Coaching Guidance for {track.DisplayName}\n" +
                                    $"Bagian: {bagian}\n" +
                                    $"Unit: {unit}\n" +
                                    $"Track: {track.TrackType}\n" +
                                    $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                    $"This is a test guidance file seeded by SeedCDPTestData for Phase 94 QA");
                                guidanceFileCount++;
                            }

                            var guidance = new CoachingGuidanceFile
                            {
                                Bagian = bagian,
                                Unit = unit,
                                ProtonTrackId = track.Id,
                                FileName = fileName,
                                FilePath = $"/uploads/guidance/{fileName}",
                                FileSize = new System.IO.FileInfo(filePath).Length,
                                UploadedAt = now.AddHours(-random.Next(1, 200)),
                                UploadedById = adminUsers.First().Id
                            };

                            guidanceFiles.Add(guidance);
                        }
                    }
                }

                await context.CoachingGuidanceFiles.AddRangeAsync(guidanceFiles);
                await context.SaveChangesAsync();

                logger?.LogInformation($"SeedCDPTestData: Created {guidanceFiles.Count} guidance file records ({guidanceFileCount} files)");

                // ========================================
                // STEP 7: Create AuditLog Entries
                // ========================================
                logger?.LogInformation("SeedCDPTestData: Step 7 - Creating audit log entries");

                var auditLogs = new List<AuditLog>();
                var actions = new[]
                {
                    "Track Assigned", "Deliverable Submitted", "Deliverable Approved",
                    "Deliverable Rejected", "HC Review Completed", "Guidance Uploaded",
                    "User Created", "Role Assigned"
                };

                for (int i = 0; i < 50; i++)
                {
                    var actor = adminUsers.Concat(hcUsers).ElementAt(random.Next(adminUsers.Count + hcUsers.Count));
                    var action = actions[random.Next(actions.Length)];
                    var targetUser = coacheeUsers[random.Next(coacheeUsers.Count)];

                    auditLogs.Add(new AuditLog
                    {
                        ActorUserId = actor.Id,
                        ActorName = actor.FullName ?? actor.UserName ?? "Unknown",
                        ActionType = action,
                        TargetType = "User",
                        TargetId = null, // User IDs are strings, can't map to int?
                        Description = $"Seed test data: {action} by {actor.FullName} for user {targetUser.FullName}",
                        CreatedAt = now.AddHours(-random.Next(1, 500))
                    });
                }

                await context.AuditLogs.AddRangeAsync(auditLogs);
                await context.SaveChangesAsync();

                logger?.LogInformation($"SeedCDPTestData: Created {auditLogs.Count} audit log entries");

                // ========================================
                // SUMMARY
                // ========================================
                logger?.LogInformation("=== SeedCDPTestData Complete ===");
                logger?.LogInformation($"  Users: {allUsers.Count} (Coachee: {coacheeUsers.Count}, Coach: {coachUsers.Count}, " +
                    $"SectionHead: {sectionHeadUsers.Count}, HC: {hcUsers.Count}, Admin: {adminUsers.Count})");
                logger?.LogInformation($"  Track Assignments: {assignments.Count} (Active: {activeAssignments.Count})");
                logger?.LogInformation($"  Deliverable Progress: {progressRecords.Count}");
                logger?.LogInformation($"  Evidence Files: {evidenceCreated}");
                logger?.LogInformation($"  Guidance Files: {guidanceFiles.Count}");
                logger?.LogInformation($"  Audit Logs: {auditLogs.Count}");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SeedCDPTestData failed");
                throw;
            }
        }

        /// <summary>
        /// Gets test data summary for documentation
        /// </summary>
        public static async Task<string> GetTestDataSummaryAsync(ApplicationDbContext context)
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== CDP Test Data Summary ===");
            summary.AppendLine();

            // Users by role
            var users = await context.Users.Where(u => u.IsActive).GroupBy(u => u.RoleLevel)
                .Select(g => new { RoleLevel = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.RoleLevel)
                .ToListAsync();

            summary.AppendLine("Users by Role Level:");
            foreach (var group in users)
            {
                var roleName = group.RoleLevel switch
                {
                    1 or 2 => "Admin/HC",
                    3 or 4 => "Coach/SrSpv",
                    5 => "SectionHead",
                    6 => "Coachee",
                    _ => "Unknown"
                };
                summary.AppendLine($"  Level {group.RoleLevel} ({roleName}): {group.Count}");
            }
            summary.AppendLine();

            // Track assignments
            var assignments = await context.ProtonTrackAssignments.CountAsync();
            var activeAssignments = await context.ProtonTrackAssignments.CountAsync(a => a.IsActive);
            summary.AppendLine($"Track Assignments: {assignments} total, {activeAssignments} active");
            summary.AppendLine();

            // Deliverable progress by status
            var progressByStatus = await context.ProtonDeliverableProgresses
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            summary.AppendLine("Deliverable Progress by Status:");
            foreach (var group in progressByStatus.OrderByDescending(g => g.Count))
            {
                summary.AppendLine($"  {group.Status}: {group.Count}");
            }
            summary.AppendLine();

            // Evidence files
            var withEvidence = await context.ProtonDeliverableProgresses
                .CountAsync(p => !string.IsNullOrEmpty(p.EvidencePath));
            summary.AppendLine($"Progress with Evidence: {withEvidence}");
            summary.AppendLine();

            // Guidance files
            var guidanceFiles = await context.CoachingGuidanceFiles.CountAsync();
            summary.AppendLine($"Coaching Guidance Files: {guidanceFiles}");

            return summary.ToString();
        }
    }
}
