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
        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
        public DbSet<AssessmentOption> AssessmentOptions { get; set; }
        public DbSet<UserResponse> UserResponses { get; set; }
        public DbSet<IdpItem> IdpItems { get; set; }
        
        // Master Data (KKJ & CPDP)
        public DbSet<KkjMatrixItem> KkjMatrices { get; set; }
        public DbSet<CpdpItem> CpdpItems { get; set; }

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

                // Default value for audit field
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

            builder.Entity<CpdpItem>(entity =>
            {
                entity.ToTable("CpdpItems");
            });
        }
    }
}
