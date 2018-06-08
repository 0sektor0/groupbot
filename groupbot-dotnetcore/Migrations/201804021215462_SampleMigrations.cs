namespace groupbot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SampleMigrations : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Admins", "CreationTime", c => c.DateTime(nullable: false, precision: 0));
            AddColumn("dbo.Groups", "CreationTime", c => c.DateTime(nullable: false, precision: 0));
            AddColumn("dbo.DelayedRequests", "CreationTime", c => c.DateTime(nullable: false, precision: 0));
            AddColumn("dbo.Posts", "CreationDate", c => c.DateTime(nullable: false, precision: 0));
            AddColumn("dbo.Photos", "UploadTime", c => c.DateTime(nullable: false, precision: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "UploadTime");
            DropColumn("dbo.Posts", "CreationDate");
            DropColumn("dbo.DelayedRequests", "CreationTime");
            DropColumn("dbo.Groups", "CreationTime");
            DropColumn("dbo.Admins", "CreationTime");
        }
    }
}
