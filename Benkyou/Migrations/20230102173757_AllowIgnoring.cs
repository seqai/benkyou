using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Benkyou.Migrations
{
    public partial class AllowIgnoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ignored",
                table: "Records",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ignored",
                table: "Records");
        }
    }
}
