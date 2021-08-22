using Microsoft.EntityFrameworkCore.Migrations;

namespace LanternCardGame.Data.Migrations
{
    public partial class AddPlayersStatsGamesStartedColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GamesStarted",
                table: "PlayerStats",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesStarted",
                table: "PlayerStats");
        }
    }
}
