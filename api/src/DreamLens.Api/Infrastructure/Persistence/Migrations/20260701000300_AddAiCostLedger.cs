using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DreamLensDbContext))]
[Migration("20260701000300_AddAiCostLedger")]
public partial class AddAiCostLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiCostLedger",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DreamId = table.Column<Guid>(type: "uuid", nullable: true),
                Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PersonaId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FailureKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                InputTokens = table.Column<int>(type: "integer", nullable: true),
                OutputTokens = table.Column<int>(type: "integer", nullable: true),
                TotalTokens = table.Column<int>(type: "integer", nullable: true),
                LatencyMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                EstimatedCostUsd = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiCostLedger", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AiCostLedger_DreamId",
            table: "AiCostLedger",
            column: "DreamId");

        migrationBuilder.CreateIndex(
            name: "IX_AiCostLedger_UserSubject_CreatedAt",
            table: "AiCostLedger",
            columns: ["UserSubject", "CreatedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AiCostLedger");
    }
}
