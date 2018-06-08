namespace groupbot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PseudoName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Groups", "PseudoName", c => c.String(unicode: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Groups", "PseudoName");
        }
    }
}
