using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioSport.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions",
                column: "ClassTemplateId",
                principalTable: "ClassTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions",
                column: "ClassTemplateId",
                principalTable: "ClassTemplates",
                principalColumn: "Id");
        }
    }
}
