using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncJobLatencyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProviderLatencyMilliseconds",
                table: "DreamImages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstStartedAt",
                table: "AsyncJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProcessingDurationMilliseconds",
                table: "AsyncJobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "QueueWaitMilliseconds",
                table: "AsyncJobs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderLatencyMilliseconds",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "FirstStartedAt",
                table: "AsyncJobs");

            migrationBuilder.DropColumn(
                name: "ProcessingDurationMilliseconds",
                table: "AsyncJobs");

            migrationBuilder.DropColumn(
                name: "QueueWaitMilliseconds",
                table: "AsyncJobs");
        }
    }
}
