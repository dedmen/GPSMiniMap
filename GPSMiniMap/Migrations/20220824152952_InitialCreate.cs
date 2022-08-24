using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPSMiniMap.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeocodeCache",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocodeCache", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "LocationHistory",
                columns: table => new
                {
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Longitude = table.Column<float>(type: "REAL", nullable: false),
                    Latitude = table.Column<float>(type: "REAL", nullable: false),
                    Speed = table.Column<float>(type: "REAL", nullable: false),
                    Heading = table.Column<float>(type: "REAL", nullable: false),
                    Accuracy = table.Column<float>(type: "REAL", nullable: false),
                    Altitude = table.Column<float>(type: "REAL", nullable: false),
                    AltitudeAccuracy = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationHistory", x => x.Time);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeocodeCache");

            migrationBuilder.DropTable(
                name: "LocationHistory");
        }
    }
}
