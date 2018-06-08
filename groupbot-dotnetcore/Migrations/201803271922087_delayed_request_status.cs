namespace groupbot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class delayed_request_status : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DelayedRequests", "IsResended", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DelayedRequests", "IsResended");
        }
    }
}
