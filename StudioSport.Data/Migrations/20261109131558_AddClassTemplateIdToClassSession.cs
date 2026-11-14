using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioSport.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassTemplateIdToClassSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassTemplateId",
                table: "ClassSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ClassTemplateId",
                table: "ClassSessions",
                column: "ClassTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions",
                column: "ClassTemplateId",
                principalTable: "ClassTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_ClassTemplates_ClassTemplateId",
                table: "ClassSessions");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_ClassTemplateId",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "ClassTemplateId",
                table: "ClassSessions");
        }
    }
}
