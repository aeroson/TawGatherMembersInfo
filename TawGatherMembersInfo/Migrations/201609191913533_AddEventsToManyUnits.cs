namespace TawGatherMembersInfo.Migrations
{
	using System;
	using System.Data.Entity.Migrations;

	public partial class AddEventsToManyUnits : DbMigration
	{
		public override void Up()
		{
			DropForeignKey("dbo.Events", "UnitId", "dbo.Units");
			DropIndex("dbo.Events", new[] { "UnitId" });
			CreateTable(
				"dbo.UnitEvents",
				c => new
				{
					Unit_UnitId = c.Long(nullable: false),
					Event_EventId = c.Long(nullable: false),
				})
				.PrimaryKey(t => new { t.Unit_UnitId, t.Event_EventId })
				.ForeignKey("dbo.Units", t => t.Unit_UnitId, cascadeDelete: true)
				.ForeignKey("dbo.Events", t => t.Event_EventId, cascadeDelete: true)
				.Index(t => t.Unit_UnitId)
				.Index(t => t.Event_EventId);

			AddColumn("dbo.PeopleToUnits", "From", c => c.DateTime(nullable: false, precision: 0));
			AddColumn("dbo.PeopleToUnits", "To", c => c.DateTime(nullable: false, precision: 0));
			DropColumn("dbo.Events", "UnitId");
		}

		public override void Down()
		{
			AddColumn("dbo.Events", "Unit_UnitId", c => c.Long());
			DropForeignKey("dbo.UnitEvents", "Event_EventId", "dbo.Events");
			DropForeignKey("dbo.UnitEvents", "Unit_UnitId", "dbo.Units");
			DropIndex("dbo.UnitEvents", new[] { "Event_EventId" });
			DropIndex("dbo.UnitEvents", new[] { "Unit_UnitId" });
			DropColumn("dbo.PeopleToUnits", "To");
			DropColumn("dbo.PeopleToUnits", "From");
			DropTable("dbo.UnitEvents");
			CreateIndex("dbo.Events", "Unit_UnitId");
			AddForeignKey("dbo.Events", "Unit_UnitId", "dbo.Units", "UnitId");
		}
	}
}