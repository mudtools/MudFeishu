// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeishuFileServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FolderToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VersionToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FileMD5 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FolderRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ParentFolderToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VersionToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileMD5 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_FileMD5",
                table: "FileRecords",
                column: "FileMD5");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_FileToken",
                table: "FileRecords",
                column: "FileToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_FolderToken",
                table: "FileRecords",
                column: "FolderToken");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_IsDeleted",
                table: "FileRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_FolderToken",
                table: "FolderRecords",
                column: "FolderToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_IsDeleted",
                table: "FolderRecords",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_ParentFolderToken",
                table: "FolderRecords",
                column: "ParentFolderToken");

            migrationBuilder.CreateIndex(
                name: "IX_VersionRecords_FileToken",
                table: "VersionRecords",
                column: "FileToken");

            migrationBuilder.CreateIndex(
                name: "IX_VersionRecords_IsCurrentVersion",
                table: "VersionRecords",
                column: "IsCurrentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_VersionRecords_VersionToken",
                table: "VersionRecords",
                column: "VersionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileRecords");

            migrationBuilder.DropTable(
                name: "FolderRecords");

            migrationBuilder.DropTable(
                name: "VersionRecords");
        }
    }
}
