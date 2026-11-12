using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bookshelf.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpenLibraryKey = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeathDate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhotoId = table.Column<int>(type: "integer", nullable: true),
                    LastFetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpenLibraryKey = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FirstPublishYear = table.Column<int>(type: "integer", nullable: true),
                    CoverId = table.Column<int>(type: "integer", nullable: true),
                    LastFetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpenLibraryKey = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WorkId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Publishers = table.Column<List<string>>(type: "text[]", nullable: false),
                    PublishDate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NumberOfPages = table.Column<int>(type: "integer", nullable: true),
                    Isbn10 = table.Column<List<string>>(type: "text[]", nullable: false),
                    Isbn13 = table.Column<List<string>>(type: "text[]", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CoverId = table.Column<int>(type: "integer", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "WorkAuthors",
                columns: table => new
                {
                    WorkId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAuthors", x => new { x.WorkId, x.AuthorId });
                    table.ForeignKey(
                        name: "FK_WorkAuthors_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAuthors_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_OpenLibraryKey",
                table: "Authors",
                column: "OpenLibraryKey",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_WorkAuthors_AuthorId",
                table: "WorkAuthors",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_OpenLibraryKey",
                table: "Works",
                column: "OpenLibraryKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Editions");

            migrationBuilder.DropTable(
                name: "WorkAuthors");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Works");
        }
    }
}
