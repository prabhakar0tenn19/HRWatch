using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRWatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPoliciesAndAddSeverity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Violations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DesignationId",
                table: "Policies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Violations_Severity",
                table: "Violations",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_DesignationId",
                table: "Policies",
                column: "DesignationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_Designations_DesignationId",
                table: "Policies",
                column: "DesignationId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_Designations_DesignationId",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Violations_Severity",
                table: "Violations");

            migrationBuilder.DropIndex(
                name: "IX_Policies_DesignationId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Violations");

            migrationBuilder.DropColumn(
                name: "DesignationId",
                table: "Policies");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
