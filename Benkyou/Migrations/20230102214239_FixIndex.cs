using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Benkyou.Migrations
{
    public partial class FixIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_UserId_Content",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_UserId_Content_RecordType",
                table: "Records",
                columns: new[] { "UserId", "Content", "RecordType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_UserId_Content_RecordType",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_UserId_Content",
                table: "Records",
                columns: new[] { "UserId", "Content" },
                unique: true);
        }
    }
}
