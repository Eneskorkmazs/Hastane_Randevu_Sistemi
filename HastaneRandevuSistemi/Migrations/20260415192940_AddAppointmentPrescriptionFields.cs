using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HastaneRandevuSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentPrescriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrescriptionCreatedAt",
                table: "Appointments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionDiagnosis",
                table: "Appointments",
                type: "TEXT",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionMedications",
                table: "Appointments",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriptionNotes",
                table: "Appointments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrescriptionCreatedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PrescriptionDiagnosis",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PrescriptionMedications",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PrescriptionNotes",
                table: "Appointments");
        }
    }
}
