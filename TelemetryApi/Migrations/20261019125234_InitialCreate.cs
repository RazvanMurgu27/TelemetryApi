using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TelemetryApi.Data;
using TelemetryApi.Models;

#nullable disable

namespace TelemetryApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatterySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    AlertEmail = table.Column<string>(type: "TEXT", nullable: false),
                    MaxVoltageThreshold = table.Column<double>(type: "REAL", nullable: false),
                    MinVoltageThreshold = table.Column<double>(type: "REAL", nullable: false),
                    WarningVoltageThreshold = table.Column<double>(type: "REAL", nullable: false),
                    MaxTemperatureThreshold = table.Column<double>(type: "REAL", nullable: false),
                    MinTemperatureThreshold = table.Column<double>(type: "REAL", nullable: false),
                    MaxCurrentThreshold = table.Column<double>(type: "REAL", nullable: false),
                    MaxDeltaVThreshold = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatterySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    Voltage = table.Column<double>(type: "REAL", nullable: false),
                    Current = table.Column<double>(type: "REAL", nullable: false),
                    Power = table.Column<double>(type: "REAL", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    Soc = table.Column<int>(type: "INTEGER", nullable: false),
                    HighestCellVoltage = table.Column<double>(type: "REAL", nullable: true),
                    LowestCellVoltage = table.Column<double>(type: "REAL", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorTimeStamp = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BmsModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    CellCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BmsModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BmsModules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BmsModules_UserId",
                table: "BmsModules",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatterySettings");

            migrationBuilder.DropTable(
                name: "BmsModules");

            migrationBuilder.DropTable(
                name: "TelemetryRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
