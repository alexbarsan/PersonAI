using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyDreamContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyDreamContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Attribution = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FactsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyDreamContent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyDreamContent_ContentDate",
                table: "DailyDreamContent",
                column: "ContentDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyDreamContent");
        }
    }
}
