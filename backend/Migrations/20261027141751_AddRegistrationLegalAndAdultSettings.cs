using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationLegalAndAdultSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedPrivacyVersion",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedTermsVersion",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdultConfirmedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyAcceptedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireAdultConfirmation",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedPrivacyVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdultConfirmedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequireAdultConfirmation",
                table: "Companies");
        }
    }
}
