using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDisqualification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisqualificationNote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContestResultId = table.Column<int>(type: "int", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisqualificationNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisqualificationNote_ContestResults_ContestResultId",
                        column: x => x.ContestResultId,
                        principalTable: "ContestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisqualificationNote_ContestResultId",
                table: "DisqualificationNote",
                column: "ContestResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisqualificationNote");
        }
    }
}
