using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackWeatherWeb.Migrations
{
    /// <inheritdoc />
    public partial class TransportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToEmail",
                table: "Requests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Requests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AssignedToEmail",
                table: "Requests",
                column: "AssignedToEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Email",
                table: "Requests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status",
                table: "Requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_AssignedToEmail",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Email",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Status",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AssignedToEmail",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Requests");
        }
    }
}
