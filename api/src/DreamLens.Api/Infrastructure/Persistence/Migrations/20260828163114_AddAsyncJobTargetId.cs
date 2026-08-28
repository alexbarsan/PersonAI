using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncJobTargetId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetId",
                table: "AsyncJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsyncJobs_JobType_TargetId",
                table: "AsyncJobs",
                columns: new[] { "JobType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AsyncJobs_JobType_TargetId",
                table: "AsyncJobs");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "AsyncJobs");
        }
    }
}
