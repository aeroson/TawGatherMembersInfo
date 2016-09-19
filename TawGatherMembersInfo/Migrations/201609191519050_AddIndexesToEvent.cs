namespace TawGatherMembersInfo.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIndexesToEvent : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Events", "From");
            CreateIndex("dbo.Events", "To");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Events", new[] { "To" });
            DropIndex("dbo.Events", new[] { "From" });
        }
    }
}
