using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioSport.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionPlansAndCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalClasses",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedClasses",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TotalClasses",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "UsedClasses",
                table: "Subscriptions");
        }
    }
}
