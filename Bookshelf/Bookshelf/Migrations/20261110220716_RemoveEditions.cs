using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bookshelf.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Editions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Works",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPages",
                table: "Works",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Works",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceEditionKey",
                table: "Works",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfPages",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "SourceEditionKey",
                table: "Works");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Works",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkId = table.Column<int>(type: "integer", nullable: false),
                    CoverId = table.Column<int>(type: "integer", nullable: true),
                    Isbn10 = table.Column<List<string>>(type: "text[]", nullable: false),
                    Isbn13 = table.Column<List<string>>(type: "text[]", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NumberOfPages = table.Column<int>(type: "integer", nullable: true),
                    OpenLibraryKey = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishDate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Publishers = table.Column<List<string>>(type: "text[]", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Editions_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Editions_Isbn13",
                table: "Editions",
                column: "Isbn13")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Editions_OpenLibraryKey",
                table: "Editions",
                column: "OpenLibraryKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Editions_WorkId",
                table: "Editions",
                column: "WorkId");
        }
    }
}
