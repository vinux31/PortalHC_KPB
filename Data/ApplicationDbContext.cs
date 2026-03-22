using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Database context untuk HC Portal dengan Identity support
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ========== DbSets untuk semua entity ==========
        public DbSet<TrainingRecord> TrainingRecords { get; set; }
        public DbSet<CoachingLog> CoachingLogs { get; set; }
        public DbSet<AssessmentSession> AssessmentSessions { get; set; }
        public DbSet<IdpItem> IdpItems { get; set; }
        
        // Master Data (KKJ & CPDP)
        public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
        public DbSet<KkjFile> KkjFiles { get; set; }
        public DbSet<CpdpFile> CpdpFiles { get; set; }

        // Coaching Sessions (Phase 4)
        public DbSet<CoachingSession> CoachingSessions { get; set; }
        public DbSet<ActionItem> ActionItems { get; set; }
        public DbSet<CoachCoacheeMapping> CoachCoacheeMappings { get; set; }

        // Proton Deliverable Tracking (Phase 5)
        public DbSet<ProtonKompetensi> ProtonKompetensiList { get; set; }
        public DbSet<ProtonSubKompetensi> ProtonSubKompetensiList { get; set; }
        public DbSet<ProtonDeliverable> ProtonDeliverableList { get; set; }
        public DbSet<ProtonTrackAssignment> ProtonTrackAssignments { get; set; }
        public DbSet<ProtonDeliverableProgress> ProtonDeliverableProgresses { get; set; }

        // Approval Workflow & Completion (Phase 6)
        public DbSet<ProtonNotification> ProtonNotifications { get; set; }
        public DbSet<ProtonFinalAssessment> ProtonFinalAssessments { get; set; }

        // Proton Track (Phase 33 — normalized track entity)
        public DbSet<ProtonTrack> ProtonTracks { get; set; }

        // Coaching Guidance Files (Phase 51)
        public DbSet<CoachingGuidanceFile> CoachingGuidanceFiles { get; set; }

        // Test Packages — Phase 17
        public DbSet<AssessmentPackage> AssessmentPackages { get; set; }
        public DbSet<PackageQuestion> PackageQuestions { get; set; }
        public DbSet<PackageOption> PackageOptions { get; set; }
        public DbSet<UserPackageAssignment> UserPackageAssignments { get; set; }
        public DbSet<PackageUserResponse> PackageUserResponses { get; set; }

        // Audit Log — Phase 24
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Attempt History — Phase 46
        public DbSet<AssessmentAttemptHistory> AssessmentAttemptHistory { get; set; }

        // Deliverable Status History — Phase 117
        public DbSet<DeliverableStatusHistory> DeliverableStatusHistories { get; set; }

        // Notification System — Phase 99
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        // Exam Activity Log — Phase 166
        public DbSet<ExamActivityLog> ExamActivityLogs { get; set; }

        // Assessment Categories — Phase 190
        public DbSet<AssessmentCategory> AssessmentCategories { get; set; }

        // ET Scores per Session — Phase 223
        public DbSet<SessionElemenTeknisScore> SessionElemenTeknisScores { get; set; }

        // ========== Helper Methods untuk OrganizationUnit (Phase 221) ==========

        public async Task<List<string>> GetAllSectionsAsync()
        {
            return await OrganizationUnits
                .Where(u => u.ParentId == null && u.IsActive)
                .OrderBy(u => u.DisplayOrder)
                .Select(u => u.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetUnitsForSectionAsync(string sectionName)
        {
            var parent = await OrganizationUnits
                .FirstOrDefaultAsync(u => u.ParentId == null && u.Name == sectionName && u.IsActive);
            if (parent == null) return new List<string>();
            return await OrganizationUnits
                .Where(u => u.ParentId == parent.Id && u.IsActive)
                .OrderBy(u => u.DisplayOrder)
                .Select(u => u.Name)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<string>>> GetSectionUnitsDictAsync()
        {
            var bagians = await OrganizationUnits
                .Where(u => u.ParentId == null && u.IsActive)
                .OrderBy(u => u.DisplayOrder)
                .ToListAsync();
            var bagianIds = bagians.Select(b => b.Id).ToList();
            var units = await OrganizationUnits
                .Where(u => u.ParentId != null && bagianIds.Contains(u.ParentId!.Value) && u.IsActive)
                .OrderBy(u => u.DisplayOrder)
                .ToListAsync();
            return bagians.ToDictionary(
                b => b.Name,
                b => units.Where(u => u.ParentId == b.Id).Select(u => u.Name).ToList()
            );
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========== Customize table names ==========
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
            });

            // ========== Configure relationships ==========

            // TrainingRecord -> User
            builder.Entity<TrainingRecord>(entity =>
            {
                entity.HasOne(t => t.User)
                    .WithMany(u => u.TrainingRecords)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Phase 200: Renewal Chain FKs
                // NoAction: SQL Server blocks ON DELETE SET NULL on self/cross FKs that create cascade cycles.
                // Null-clearing on source delete is handled at application level.
                entity.Property(t => t.RenewsTrainingId).IsRequired(false);
                entity.Property(t => t.RenewsSessionId).IsRequired(false);

                entity.HasOne<TrainingRecord>()
                    .WithMany()
                    .HasForeignKey(t => t.RenewsTrainingId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne<AssessmentSession>()
                    .WithMany()
                    .HasForeignKey(t => t.RenewsSessionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // AssessmentSession -> User
            builder.Entity<AssessmentSession>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => new { a.UserId, a.Status });
                entity.HasIndex(a => a.Schedule);
                entity.HasIndex(a => a.AccessToken); // Removed .IsUnique() to allow shared tokens

                // Check constraints for data integrity
                entity.HasCheckConstraint("CK_AssessmentSession_Progress", "[Progress] >= 0 AND [Progress] <= 100");
                entity.HasCheckConstraint("CK_AssessmentSession_DurationMinutes", "[DurationMinutes] >= 0");
                entity.HasCheckConstraint("CK_AssessmentSession_PassPercentage", "[PassPercentage] >= 0 AND [PassPercentage] <= 100");

                // Default values
                entity.Property(a => a.PassPercentage).HasDefaultValue(70);
                entity.Property(a => a.AllowAnswerReview).HasDefaultValue(true);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // AssessmentSession: Proton exam fields (Phase 53)
                entity.Property(a => a.ProtonTrackId).IsRequired(false);
                entity.Property(a => a.TahunKe).HasMaxLength(20).IsRequired(false);
                entity.Property(a => a.InterviewResultsJson).HasColumnType("TEXT").IsRequired(false);

                // Phase 192: Unique filtered index on NomorSertifikat (excludes nulls)
                entity.HasIndex(a => a.NomorSertifikat)
                    .IsUnique()
                    .HasFilter("[NomorSertifikat] IS NOT NULL")
                    .HasDatabaseName("IX_AssessmentSessions_NomorSertifikat_Unique");

                // Phase 200: Renewal Chain FKs
                // NoAction: SQL Server blocks ON DELETE SET NULL on self/cross FKs that create cascade cycles.
                // Null-clearing on source delete is handled at application level.
                entity.Property(a => a.RenewsSessionId).IsRequired(false);
                entity.Property(a => a.RenewsTrainingId).IsRequired(false);

                entity.HasOne<AssessmentSession>()
                    .WithMany()
                    .HasForeignKey(a => a.RenewsSessionId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne<TrainingRecord>()
                    .WithMany()
                    .HasForeignKey(a => a.RenewsTrainingId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Legacy UserResponse and AssessmentQuestion tables removed in Phase 227 (CLEN-02 migration).

            // IdpItem -> User
            builder.Entity<IdpItem>(entity =>
            {
                entity.HasOne(i => i.User)
                    .WithMany()
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CoachingLog (no FK for now, uses string IDs)
            builder.Entity<CoachingLog>(entity =>
            {
                entity.HasIndex(c => c.CoachId);
                entity.HasIndex(c => c.CoacheeId);
            });

            // KkjFile: uploaded PDF/Excel files per organization unit
            builder.Entity<KkjFile>(entity =>
            {
                entity.ToTable("KkjFiles");
                entity.HasOne(f => f.OrganizationUnit)
                      .WithMany(b => b.KkjFiles)
                      .HasForeignKey(f => f.OrganizationUnitId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CpdpFile: uploaded PDF/Excel files per organization unit (mirrors KkjFile)
            builder.Entity<CpdpFile>(entity =>
            {
                entity.ToTable("CpdpFiles");
                entity.HasOne(f => f.OrganizationUnit)
                      .WithMany(b => b.CpdpFiles)
                      .HasForeignKey(f => f.OrganizationUnitId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AssessmentCompetencyMap + UserCompetencyLevel removed (Phase 227 CLEN-03 — orphan tables dropped)

            // CoachingSession configuration
            builder.Entity<CoachingSession>(entity =>
            {
                entity.HasIndex(s => s.CoachId);
                entity.HasIndex(s => s.CoacheeId);
                entity.HasIndex(s => new { s.CoacheeId, s.Date });
                entity.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ActionItem configuration
            builder.Entity<ActionItem>(entity =>
            {
                entity.HasOne(a => a.CoachingSession)
                    .WithMany(s => s.ActionItems)
                    .HasForeignKey(a => a.CoachingSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(a => a.CoachingSessionId);
                entity.HasIndex(a => a.Status);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // CoachCoacheeMapping configuration
            builder.Entity<CoachCoacheeMapping>(entity =>
            {
                entity.HasIndex(m => m.CoachId);
                entity.HasIndex(m => m.CoacheeId)
                    .HasFilter("[IsActive] = 1")
                    .IsUnique()
                    .HasDatabaseName("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique");
                entity.HasIndex(m => new { m.CoachId, m.CoacheeId });
            });

            // Proton Deliverable Tracking configuration (Phase 5)

            // ProtonTrack entity configuration (Phase 33)
            builder.Entity<ProtonTrack>(entity =>
            {
                entity.ToTable("ProtonTracks");
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => new { t.TrackType, t.TahunKe }).IsUnique();
                entity.Property(t => t.TrackType).IsRequired().HasMaxLength(50);
                entity.Property(t => t.TahunKe).IsRequired().HasMaxLength(50);
                entity.Property(t => t.DisplayName).IsRequired().HasMaxLength(100);
            });

            // ProtonKompetensi -> ProtonTrack (Phase 33)
            builder.Entity<ProtonKompetensi>(entity =>
            {
                entity.HasOne(k => k.ProtonTrack)
                    .WithMany(t => t.KompetensiList)
                    .HasForeignKey(k => k.ProtonTrackId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(k => k.ProtonTrackId);
            });

            // ProtonTrackAssignment -> ProtonTrack (Phase 33) + existing indexes
            builder.Entity<ProtonTrackAssignment>(entity =>
            {
                entity.HasOne(a => a.ProtonTrack)
                    .WithMany()
                    .HasForeignKey(a => a.ProtonTrackId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(a => a.ProtonTrackId);
                entity.HasIndex(a => a.CoacheeId);
                entity.HasIndex(a => new { a.CoacheeId, a.IsActive });
            });

            // ProtonSubKompetensi -> ProtonKompetensi
            builder.Entity<ProtonSubKompetensi>(entity =>
            {
                entity.HasOne(s => s.ProtonKompetensi)
                    .WithMany(k => k.SubKompetensiList)
                    .HasForeignKey(s => s.ProtonKompetensiId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(s => s.ProtonKompetensiId);
            });

            // ProtonDeliverable -> ProtonSubKompetensi
            builder.Entity<ProtonDeliverable>(entity =>
            {
                entity.HasOne(d => d.ProtonSubKompetensi)
                    .WithMany(s => s.Deliverables)
                    .HasForeignKey(d => d.ProtonSubKompetensiId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(d => d.ProtonSubKompetensiId);
            });

            // ProtonDeliverableProgress -> ProtonDeliverable + ProtonTrackAssignment
            builder.Entity<ProtonDeliverableProgress>(entity =>
            {
                entity.HasOne(p => p.ProtonDeliverable)
                    .WithMany()
                    .HasForeignKey(p => p.ProtonDeliverableId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.ProtonTrackAssignment)
                    .WithMany()
                    .HasForeignKey(p => p.ProtonTrackAssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(p => p.CoacheeId);
                entity.HasIndex(p => p.ProtonTrackAssignmentId);
                entity.HasIndex(p => new { p.ProtonTrackAssignmentId, p.ProtonDeliverableId }).IsUnique();
                entity.HasIndex(p => p.Status);
            });

            // ProtonFinalAssessment -> ProtonTrackAssignment (Phase 6)
            builder.Entity<ProtonFinalAssessment>(entity =>
            {
                entity.HasOne(fa => fa.ProtonTrackAssignment)
                    .WithMany()
                    .HasForeignKey(fa => fa.ProtonTrackAssignmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(fa => fa.CoacheeId);
                entity.HasIndex(fa => new { fa.CoacheeId, fa.Status });
            });

            // ProtonNotification indexes (Phase 6)
            builder.Entity<ProtonNotification>(entity =>
            {
                entity.HasIndex(n => n.RecipientId);
                entity.HasIndex(n => new { n.RecipientId, n.IsRead });
                entity.HasIndex(n => n.CoacheeId);
            });

            // CoachingGuidanceFile -> ProtonTrack (Phase 51)
            builder.Entity<CoachingGuidanceFile>(entity =>
            {
                entity.HasOne(f => f.ProtonTrack)
                    .WithMany()
                    .HasForeignKey(f => f.ProtonTrackId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(f => new { f.Bagian, f.Unit, f.ProtonTrackId });
            });

            // ProtonDeliverableProgress HCApprovalStatus default (Phase 6)
            builder.Entity<ProtonDeliverableProgress>()
                .Property(p => p.HCApprovalStatus)
                .HasDefaultValue("Pending");

            // ========== Test Package System (Phase 17) ==========

            // AssessmentPackage -> AssessmentSession (Cascade)
            builder.Entity<AssessmentPackage>(entity =>
            {
                entity.HasOne(p => p.AssessmentSession)
                    .WithMany()
                    .HasForeignKey(p => p.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.AssessmentSessionId);
            });

            // PackageQuestion -> AssessmentPackage (Cascade)
            builder.Entity<PackageQuestion>(entity =>
            {
                entity.HasOne(q => q.AssessmentPackage)
                    .WithMany(p => p.Questions)
                    .HasForeignKey(q => q.AssessmentPackageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(q => q.AssessmentPackageId);
                entity.HasIndex(q => q.Order);
            });

            // PackageOption -> PackageQuestion (Cascade)
            builder.Entity<PackageOption>(entity =>
            {
                entity.HasOne(o => o.PackageQuestion)
                    .WithMany(q => q.Options)
                    .HasForeignKey(o => o.PackageQuestionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(o => o.PackageQuestionId);
            });

            // UserPackageAssignment -> AssessmentSession (Cascade)
            // UserPackageAssignment -> AssessmentPackage (Restrict — don't cascade-delete assignments on package delete)
            builder.Entity<UserPackageAssignment>(entity =>
            {
                entity.HasOne(a => a.AssessmentSession)
                    .WithMany()
                    .HasForeignKey(a => a.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AssessmentPackage)
                    .WithMany()
                    .HasForeignKey(a => a.AssessmentPackageId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique index: one assignment per (AssessmentSession, UserId) pair
                entity.HasIndex(a => new { a.AssessmentSessionId, a.UserId })
                    .IsUnique();

                entity.HasIndex(a => a.UserId);
            });

            // PackageUserResponse -> AssessmentSession (Restrict — same as UserResponse pattern)
            builder.Entity<PackageUserResponse>(entity =>
            {
                entity.HasOne(r => r.AssessmentSession)
                    .WithMany()
                    .HasForeignKey(r => r.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.PackageQuestion)
                    .WithMany()
                    .HasForeignKey(r => r.PackageQuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.PackageOption)
                    .WithMany()
                    .HasForeignKey(r => r.PackageOptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.AssessmentSessionId, r.PackageQuestionId })
                    .IsUnique();
            });

            // ========== Audit Log (Phase 24) ==========
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => a.ActorUserId);
                entity.HasIndex(a => a.ActionType);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ========== Attempt History (Phase 46) ==========
            builder.Entity<AssessmentAttemptHistory>(entity =>
            {
                entity.HasOne(h => h.User)
                    .WithMany()
                    .HasForeignKey(h => h.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(h => h.UserId);
                entity.HasIndex(h => new { h.UserId, h.Title });
                entity.Property(h => h.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ========== Notification System (Phase 99) ==========
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.Category);
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<UserNotification>(entity =>
            {
                // Indexes for performance (CRITICAL for notification queries)
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => n.CreatedAt);

                // Foreign key to ApplicationUser
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Default values
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(n => n.IsRead).HasDefaultValue(false);
                entity.Property(n => n.DeliveryStatus).HasDefaultValue("Delivered");
            });

            // ========== Exam Activity Log (Phase 166) ==========
            builder.Entity<ExamActivityLog>(entity =>
            {
                entity.HasOne(l => l.Session)
                    .WithMany()
                    .HasForeignKey(l => l.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(l => l.SessionId);
                entity.HasIndex(l => l.Timestamp);
            });

            // ========== Organization Units (Phase 219) ==========
            builder.Entity<OrganizationUnit>(entity =>
            {
                entity.ToTable("OrganizationUnits");
                entity.HasIndex(u => u.Name).IsUnique();
                entity.HasIndex(u => new { u.ParentId, u.DisplayOrder });
                entity.Property(u => u.IsActive).HasDefaultValue(true);

                entity.HasOne(u => u.Parent)
                      .WithMany(u => u.Children)
                      .HasForeignKey(u => u.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== ET Scores per Session (Phase 223) ==========
            builder.Entity<SessionElemenTeknisScore>(entity =>
            {
                entity.HasOne(e => e.AssessmentSession)
                    .WithMany()
                    .HasForeignKey(e => e.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AssessmentSessionId, e.ElemenTeknis })
                    .IsUnique()
                    .HasDatabaseName("IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis");
            });

            // ========== Assessment Categories (Phase 190) ==========
            builder.Entity<AssessmentCategory>(entity =>
            {
                entity.ToTable("AssessmentCategories");
                entity.HasIndex(c => c.Name).IsUnique();
                entity.HasIndex(c => c.SortOrder);
                entity.Property(c => c.IsActive).HasDefaultValue(true);
                entity.Property(c => c.DefaultPassPercentage).HasDefaultValue(70);

                // Self-referencing FK — Phase 195
                entity.HasOne(c => c.Parent)
                      .WithMany(c => c.Children)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Signatory FK — Phase 195
                entity.HasOne(c => c.Signatory)
                      .WithMany()
                      .HasForeignKey(c => c.SignatoryUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
