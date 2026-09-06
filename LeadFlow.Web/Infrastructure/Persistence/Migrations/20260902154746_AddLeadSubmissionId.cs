using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadSubmissionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "Leads",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Leads\" SET \"SubmissionId\" = \"Id\" WHERE \"SubmissionId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubmissionId",
                table: "Leads",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_SubmissionId",
                table: "Leads",
                column: "SubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_SubmissionId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Leads");
        }
    }
}
