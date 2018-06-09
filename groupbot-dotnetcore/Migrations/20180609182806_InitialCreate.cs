using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace groupbot.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VkId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    PseudoName = table.Column<string>(nullable: true),
                    PostTime = table.Column<int>(nullable: false),
                    PostponeEnabled = table.Column<bool>(nullable: false),
                    Limit = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true),
                    Offset = table.Column<int>(nullable: false),
                    IsWt = table.Column<bool>(nullable: false),
                    Notify = table.Column<bool>(nullable: false),
                    PostsCounter = table.Column<int>(nullable: false),
                    MinPostCount = table.Column<int>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VkId = table.Column<int>(nullable: false),
                    FName = table.Column<string>(nullable: true),
                    SName = table.Column<string>(nullable: true),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    ActiveGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Groups_ActiveGroupId",
                        column: x => x.ActiveGroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DelayedRequests",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Request = table.Column<string>(nullable: true),
                    IsResended = table.Column<bool>(nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    GroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelayedRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelayedRequests_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(nullable: true),
                    IsPublished = table.Column<bool>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    GroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<int>(nullable: false),
                    AdminId = table.Column<int>(nullable: false),
                    Notify = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupAdmins_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupAdmins_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PictureName = table.Column<string>(nullable: true),
                    XPictureAddress = table.Column<string>(nullable: true),
                    SPictureAddress = table.Column<string>(nullable: true),
                    UploadTime = table.Column<DateTime>(nullable: false),
                    PostId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_ActiveGroupId",
                table: "Admins",
                column: "ActiveGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DelayedRequests_GroupId",
                table: "DelayedRequests",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAdmins_AdminId",
                table: "GroupAdmins",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAdmins_GroupId",
                table: "GroupAdmins",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_PostId",
                table: "Photos",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupId",
                table: "Posts",
                column: "GroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DelayedRequests");

            migrationBuilder.DropTable(
                name: "GroupAdmins");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
