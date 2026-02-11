using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskUp.Migrations
{
    /// <inheritdoc />
    public partial class AddBannedUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BannedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    BannedBy = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannedUsers_AspNetUsers_BannedBy",
                        column: x => x.BannedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BannedUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BannedUsers_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannedUsers_BannedBy",
                table: "BannedUsers",
                column: "BannedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BannedUsers_BoardId",
                table: "BannedUsers",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BannedUsers_UserId",
                table: "BannedUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedUsers");
        }
    }
}
