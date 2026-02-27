using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Models.Competency;

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
        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
        public DbSet<AssessmentOption> AssessmentOptions { get; set; }
        public DbSet<UserResponse> UserResponses { get; set; }
        public DbSet<IdpItem> IdpItems { get; set; }
        
        // Master Data (KKJ & CPDP)
        public DbSet<KkjMatrixItem> KkjMatrices { get; set; }
        public DbSet<KkjBagian> KkjBagians { get; set; }
        public DbSet<CpdpItem> CpdpItems { get; set; }

        // Competency Tracking
        public DbSet<AssessmentCompetencyMap> AssessmentCompetencyMaps { get; set; }
        public DbSet<UserCompetencyLevel> UserCompetencyLevels { get; set; }

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
                entity.HasCheckConstraint("CK_AssessmentSession_DurationMinutes", "[DurationMinutes] > 0");
                entity.HasCheckConstraint("CK_AssessmentSession_PassPercentage", "[PassPercentage] >= 0 AND [PassPercentage] <= 100");

                // Default values
                entity.Property(a => a.PassPercentage).HasDefaultValue(70);
                entity.Property(a => a.AllowAnswerReview).HasDefaultValue(true);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // UserResponse -> AssessmentSession (Restrict Delete to avoid Cycles)
            builder.Entity<UserResponse>(entity =>
            {
                entity.HasOne(r => r.AssessmentSession)
                    .WithMany(s => s.Responses)
                    .HasForeignKey(r => r.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Question)
                    .WithMany()
                    .HasForeignKey(r => r.AssessmentQuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.SelectedOption)
                    .WithMany()
                    .HasForeignKey(r => r.SelectedOptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for performance
                entity.HasIndex(r => new { r.AssessmentSessionId, r.AssessmentQuestionId });
            });

            // AssessmentQuestion indexes and constraints
            builder.Entity<AssessmentQuestion>(entity =>
            {
                entity.HasIndex(q => q.Order);
                entity.HasCheckConstraint("CK_AssessmentQuestion_ScoreValue", "[ScoreValue] > 0");
            });

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

            // Master data tables
            builder.Entity<KkjMatrixItem>(entity =>
            {
                entity.ToTable("KkjMatrices");
            });

            builder.Entity<KkjBagian>(entity =>
            {
                entity.ToTable("KkjBagians");
                entity.HasIndex(b => b.Name).IsUnique();
                entity.HasIndex(b => b.DisplayOrder);
            });

            builder.Entity<CpdpItem>(entity =>
            {
                entity.ToTable("CpdpItems");
            });

            // Competency Tracking configuration
            builder.Entity<AssessmentCompetencyMap>(entity =>
            {
                entity.ToTable("AssessmentCompetencyMaps");

                entity.HasOne(c => c.KkjMatrixItem)
                    .WithMany()
                    .HasForeignKey(c => c.KkjMatrixItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(c => c.AssessmentCategory);
                entity.HasIndex(c => new { c.AssessmentCategory, c.TitlePattern });
            });

            builder.Entity<UserCompetencyLevel>(entity =>
            {
                entity.ToTable("UserCompetencyLevels");

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.KkjMatrixItem)
                    .WithMany()
                    .HasForeignKey(c => c.KkjMatrixItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.AssessmentSession)
                    .WithMany()
                    .HasForeignKey(c => c.AssessmentSessionId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Unique index: one level per user per competency
                entity.HasIndex(c => new { c.UserId, c.KkjMatrixItemId })
                    .IsUnique();

                // Check constraints for level ranges
                entity.HasCheckConstraint("CK_UserCompetencyLevel_CurrentLevel", "[CurrentLevel] >= 0 AND [CurrentLevel] <= 5");
                entity.HasCheckConstraint("CK_UserCompetencyLevel_TargetLevel", "[TargetLevel] >= 0 AND [TargetLevel] <= 5");

                // Ignore computed property Gap
                entity.Ignore(c => c.Gap);
            });

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
                entity.HasIndex(m => m.CoacheeId);
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

            // ProtonDeliverableProgress -> ProtonDeliverable
            builder.Entity<ProtonDeliverableProgress>(entity =>
            {
                entity.HasOne(p => p.ProtonDeliverable)
                    .WithMany()
                    .HasForeignKey(p => p.ProtonDeliverableId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(p => p.CoacheeId);
                entity.HasIndex(p => new { p.CoacheeId, p.ProtonDeliverableId }).IsUnique();
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
        }
    }
}
