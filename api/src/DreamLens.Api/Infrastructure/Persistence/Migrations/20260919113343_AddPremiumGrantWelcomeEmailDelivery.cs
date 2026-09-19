using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumGrantWelcomeEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PremiumWelcomeEmailProviderMessageId",
                table: "PremiumGrants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PremiumWelcomeEmailSentAt",
                table: "PremiumGrants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumWelcomeEmailProviderMessageId",
                table: "PremiumGrants");

            migrationBuilder.DropColumn(
                name: "PremiumWelcomeEmailSentAt",
                table: "PremiumGrants");
        }
    }
}
