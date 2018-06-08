namespace groupbot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Group : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Admins",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VkId = c.Int(nullable: false),
                        FName = c.String(unicode: false),
                        SName = c.String(unicode: false),
                        ActiveGroup_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Groups", t => t.ActiveGroup_Id)
                .Index(t => t.ActiveGroup_Id);
            
            CreateTable(
                "dbo.Groups",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VkId = c.Int(nullable: false),
                        Name = c.String(unicode: false),
                        PostTime = c.Int(nullable: false),
                        PostponeEnabled = c.Boolean(nullable: false),
                        Limit = c.Int(nullable: false),
                        Text = c.String(unicode: false),
                        Offset = c.Int(nullable: false),
                        IsWt = c.Boolean(nullable: false),
                        Notify = c.Boolean(nullable: false),
                        PostsCounter = c.Int(nullable: false),
                        MinPostCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DelayedRequests",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Request = c.String(unicode: false),
                        Group_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Groups", t => t.Group_Id)
                .Index(t => t.Group_Id);
            
            CreateTable(
                "dbo.GroupAdmins",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GroupId = c.Int(nullable: false),
                        AdminId = c.Int(nullable: false),
                        Notify = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Admins", t => t.AdminId, cascadeDelete: true)
                .ForeignKey("dbo.Groups", t => t.GroupId, cascadeDelete: true)
                .Index(t => t.GroupId)
                .Index(t => t.AdminId);
            
            CreateTable(
                "dbo.Posts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Text = c.String(unicode: false),
                        IsPublished = c.Boolean(nullable: false),
                        Group_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Groups", t => t.Group_Id)
                .Index(t => t.Group_Id);
            
            CreateTable(
                "dbo.Photos",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PictureName = c.String(unicode: false),
                        XPictureAddress = c.String(unicode: false),
                        SPictureAddress = c.String(unicode: false),
                        Post_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Posts", t => t.Post_Id)
                .Index(t => t.Post_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Admins", "ActiveGroup_Id", "dbo.Groups");
            DropForeignKey("dbo.Photos", "Post_Id", "dbo.Posts");
            DropForeignKey("dbo.Posts", "Group_Id", "dbo.Groups");
            DropForeignKey("dbo.GroupAdmins", "GroupId", "dbo.Groups");
            DropForeignKey("dbo.GroupAdmins", "AdminId", "dbo.Admins");
            DropForeignKey("dbo.DelayedRequests", "Group_Id", "dbo.Groups");
            DropIndex("dbo.Photos", new[] { "Post_Id" });
            DropIndex("dbo.Posts", new[] { "Group_Id" });
            DropIndex("dbo.GroupAdmins", new[] { "AdminId" });
            DropIndex("dbo.GroupAdmins", new[] { "GroupId" });
            DropIndex("dbo.DelayedRequests", new[] { "Group_Id" });
            DropIndex("dbo.Admins", new[] { "ActiveGroup_Id" });
            DropTable("dbo.Photos");
            DropTable("dbo.Posts");
            DropTable("dbo.GroupAdmins");
            DropTable("dbo.DelayedRequests");
            DropTable("dbo.Groups");
            DropTable("dbo.Admins");
        }
    }
}
