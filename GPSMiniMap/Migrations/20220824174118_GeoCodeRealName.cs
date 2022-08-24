using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPSMiniMap.Migrations
{
    public partial class GeoCodeRealName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RealName",
                table: "GeocodeCache",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RealName",
                table: "GeocodeCache");
        }
    }
}
