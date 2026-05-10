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
    public partial class AddUserAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FolderRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FileRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolderRecords_UserId",
                table: "FolderRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_UserId",
                table: "FileRecords",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileRecords_Users_UserId",
                table: "FileRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FolderRecords_Users_UserId",
                table: "FolderRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileRecords_Users_UserId",
                table: "FileRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_FolderRecords_Users_UserId",
                table: "FolderRecords");

            migrationBuilder.DropIndex(
                name: "IX_FolderRecords_UserId",
                table: "FolderRecords");

            migrationBuilder.DropIndex(
                name: "IX_FileRecords_UserId",
                table: "FileRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FolderRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FileRecords");
        }
    }
}
