using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HastaneRandevuSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloseTime",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DutyDate",
                table: "Pharmacies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenTime",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Pharmacies",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseTime",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "DutyDate",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "OpenTime",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Pharmacies");
        }
    }
}
