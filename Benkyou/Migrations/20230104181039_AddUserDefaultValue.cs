using Benkyou.DAL.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Benkyou.Migrations
{
    public partial class AddUserDefaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultRecordType",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.Sql("UPDATE \"Users\" SET \"DefaultRecordType\" = 4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRecordType",
                table: "Users");
        }
    }
}
