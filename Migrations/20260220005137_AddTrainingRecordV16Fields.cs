using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingRecordV16Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomorSertifikat",
                table: "TrainingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TanggalMulai",
                table: "TrainingRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TanggalSelesai",
                table: "TrainingRecords",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomorSertifikat",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "TanggalMulai",
                table: "TrainingRecords");

            migrationBuilder.DropColumn(
                name: "TanggalSelesai",
                table: "TrainingRecords");
        }
    }
}
