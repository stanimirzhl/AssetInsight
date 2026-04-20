using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetInsight.Data.Migrations
{
    /// <inheritdoc />
    public partial class moreFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradingStrategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingStrategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingStrategies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "TradingStrategies",
                columns: new[] { "Id", "CreatedAt", "DefinitionJson", "Name", "UserId" },
                values: new object[] { 1, new DateTime(2026, 4, 19, 15, 1, 55, 382, DateTimeKind.Utc).AddTicks(6395), "{\r\n				    \"type\": \"Condition\",\r\n				    \"indicator\": \"RSI\",\r\n				    \"period\": 14,\r\n				    \"operator\": \"<\",\r\n				    \"value\": 30\r\n				}", "RSI Oversold Strategy", null });

            migrationBuilder.CreateIndex(
                name: "IX_TradingStrategies_UserId",
                table: "TradingStrategies",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradingStrategies");
        }
    }
}
