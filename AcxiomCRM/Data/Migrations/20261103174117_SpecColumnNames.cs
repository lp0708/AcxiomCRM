using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcxiomCRM.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpecColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Opportunities",
                newName: "OpportunityId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Leads",
                newName: "LeadId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FollowUps",
                newName: "FollowUpId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Customers",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AuditLogs",
                newName: "AuditLogId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Activities",
                newName: "ActivityId");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Opportunities",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Backfill existing rows from Stage (stored as int: 3 = Won, 4 = Lost).
            migrationBuilder.Sql(
                "UPDATE Opportunities SET Status = CASE Stage WHEN 3 THEN 'Won' WHEN 4 THEN 'Lost' ELSE 'Open' END;");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Status",
                table: "Opportunities",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Opportunities_Status",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Opportunities");

            migrationBuilder.RenameColumn(
                name: "OpportunityId",
                table: "Opportunities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "LeadId",
                table: "Leads",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "FollowUpId",
                table: "FollowUps",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Customers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "AuditLogId",
                table: "AuditLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ActivityId",
                table: "Activities",
                newName: "Id");
        }
    }
}
