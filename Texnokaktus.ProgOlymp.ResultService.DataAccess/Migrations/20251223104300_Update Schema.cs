using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreAdjustments_ProblemResults_ProblemResultId",
                table: "ScoreAdjustments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ContestResults_ContestId_Stage",
                table: "ContestResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScoreAdjustments",
                table: "ScoreAdjustments");

            migrationBuilder.DropColumn(
                name: "ContestId",
                table: "ContestResults");

            migrationBuilder.RenameTable(
                name: "ScoreAdjustments",
                newName: "ScoreAdjustment");

            migrationBuilder.RenameIndex(
                name: "IX_ScoreAdjustments_ProblemResultId",
                table: "ScoreAdjustment",
                newName: "IX_ScoreAdjustment_ProblemResultId");

            migrationBuilder.AddColumn<string>(
                name: "ContestName",
                table: "ContestResults",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ContestResults_ContestName_Stage",
                table: "ContestResults",
                columns: new[] { "ContestName", "Stage" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScoreAdjustment",
                table: "ScoreAdjustment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreAdjustment_ProblemResults_ProblemResultId",
                table: "ScoreAdjustment",
                column: "ProblemResultId",
                principalTable: "ProblemResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreAdjustment_ProblemResults_ProblemResultId",
                table: "ScoreAdjustment");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ContestResults_ContestName_Stage",
                table: "ContestResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScoreAdjustment",
                table: "ScoreAdjustment");

            migrationBuilder.DropColumn(
                name: "ContestName",
                table: "ContestResults");

            migrationBuilder.RenameTable(
                name: "ScoreAdjustment",
                newName: "ScoreAdjustments");

            migrationBuilder.RenameIndex(
                name: "IX_ScoreAdjustment_ProblemResultId",
                table: "ScoreAdjustments",
                newName: "IX_ScoreAdjustments_ProblemResultId");

            migrationBuilder.AddColumn<int>(
                name: "ContestId",
                table: "ContestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ContestResults_ContestId_Stage",
                table: "ContestResults",
                columns: new[] { "ContestId", "Stage" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScoreAdjustments",
                table: "ScoreAdjustments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreAdjustments_ProblemResults_ProblemResultId",
                table: "ScoreAdjustments",
                column: "ProblemResultId",
                principalTable: "ProblemResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
