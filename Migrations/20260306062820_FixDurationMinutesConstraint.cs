using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class FixDurationMinutesConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_AssessmentSession_DurationMinutes')
                    ALTER TABLE [AssessmentSessions] DROP CONSTRAINT [CK_AssessmentSession_DurationMinutes];
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notifications')
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] int NOT NULL IDENTITY,
                        [Type] nvarchar(50) NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [MessageTemplate] nvarchar(max) NOT NULL,
                        [ActionUrlTemplate] nvarchar(500) NULL,
                        [Category] nvarchar(50) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_Notifications_Category] ON [Notifications] ([Category]);
                    CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserNotifications')
                BEGIN
                    CREATE TABLE [UserNotifications] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [Type] nvarchar(50) NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [ActionUrl] nvarchar(500) NULL,
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [ReadAt] datetime2 NULL,
                        [DeliveryStatus] nvarchar(50) NOT NULL DEFAULT 'Delivered',
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_UserNotifications] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserNotifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_UserNotifications_CreatedAt] ON [UserNotifications] ([CreatedAt]);
                    CREATE INDEX [IX_UserNotifications_UserId] ON [UserNotifications] ([UserId]);
                    CREATE INDEX [IX_UserNotifications_UserId_IsRead] ON [UserNotifications] ([UserId], [IsRead]);
                END
            ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_DurationMinutes",
                table: "AssessmentSessions",
                sql: "[DurationMinutes] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentSession_DurationMinutes",
                table: "AssessmentSessions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentSession_DurationMinutes",
                table: "AssessmentSessions",
                sql: "[DurationMinutes] > 0");
        }
    }
}
