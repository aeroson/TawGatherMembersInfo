namespace TawGatherMembersInfo.Migrations
{
	using Models;
	using Neitri;
	using System;
	using System.Data.Entity.Migrations;
	using System.IO;

	public partial class InitialCreate : DbMigration
	{
		void CreateInitialPeople()
		{
			var fileSystem = new FileSystem();

			SqlFile(fileSystem.GetFile("data", "initalInsertPeople.sql"));
		}

		public override void Up()
		{
			CreateTable(
				"dbo.Events",
				c => new
				{
					EventId = c.Long(nullable: false, identity: true),
					TawId = c.Long(nullable: false),
					Name = c.String(maxLength: 500, storeType: "nvarchar"),
					Description = c.String(unicode: false, storeType: "text"),
					Type = c.String(maxLength: 500, storeType: "nvarchar"),
					Mandatory = c.Boolean(nullable: false),
					Cancelled = c.Boolean(nullable: false),
					From = c.DateTime(nullable: false, precision: 0),
					To = c.DateTime(nullable: false, precision: 0),
					TakenBy_PersonId = c.Long(),
					Unit_UnitId = c.Long(),
				})
				.PrimaryKey(t => t.EventId)
				.ForeignKey("dbo.People", t => t.TakenBy_PersonId)
				.ForeignKey("dbo.Units", t => t.Unit_UnitId)
				.Index(t => t.TawId, unique: true)
				.Index(t => t.TakenBy_PersonId)
				.Index(t => t.Unit_UnitId);

			CreateTable(
				"dbo.PeopleToEvents",
				c => new
				{
					PersonId = c.Long(nullable: false),
					EventId = c.Long(nullable: false),
					AttendanceType = c.Int(nullable: false),
					TimeStamp = c.DateTime(nullable: false, precision: 0),
				})
				.PrimaryKey(t => new { t.PersonId, t.EventId })
				.ForeignKey("dbo.Events", t => t.EventId, cascadeDelete: true)
				.ForeignKey("dbo.People", t => t.PersonId, cascadeDelete: true)
				.Index(t => t.PersonId)
				.Index(t => t.EventId);

			CreateTable(
				"dbo.People",
				c => new
				{
					PersonId = c.Long(nullable: false, identity: true),
					Name = c.String(maxLength: 500, storeType: "nvarchar"),
					RankNameShort = c.String(maxLength: 100, storeType: "nvarchar"),
					SteamId = c.Long(nullable: false),
					AvatarImageUrl = c.String(maxLength: 1000, storeType: "nvarchar"),
					Status = c.String(maxLength: 100, storeType: "nvarchar"),
					DateJoinedTaw = c.DateTime(nullable: false, precision: 0),
					LastProfileDataUpdatedDate = c.DateTime(nullable: false, precision: 0),
					CountryCodeIso3166 = c.String(maxLength: 10, storeType: "nvarchar"),
					BiographyContents = c.String(unicode: false, storeType: "text"),
				})
				.PrimaryKey(t => t.PersonId)
				.Index(t => t.Name, unique: true);

			CreateTable(
				"dbo.PeopleToUnits",
				c => new
				{
					PersonId = c.Long(nullable: false),
					UnitId = c.Long(nullable: false),
					PositionNameShort = c.String(maxLength: 500, storeType: "nvarchar"),
				})
				.PrimaryKey(t => new { t.PersonId, t.UnitId })
				.ForeignKey("dbo.People", t => t.PersonId, cascadeDelete: true)
				.ForeignKey("dbo.Units", t => t.UnitId, cascadeDelete: true)
				.Index(t => t.PersonId)
				.Index(t => t.UnitId);

			CreateTable(
				"dbo.Units",
				c => new
				{
					UnitId = c.Long(nullable: false, identity: true),
					TawId = c.Int(nullable: false),
					Type = c.String(maxLength: 500, storeType: "nvarchar"),
					Name = c.String(maxLength: 500, storeType: "nvarchar"),
					ParentUnit_UnitId = c.Long(),
				})
				.PrimaryKey(t => t.UnitId)
				.ForeignKey("dbo.Units", t => t.ParentUnit_UnitId)
				.Index(t => t.TawId, unique: true)
				.Index(t => t.ParentUnit_UnitId);

			CreateInitialPeople();
		}

		public override void Down()
		{
			DropForeignKey("dbo.Events", "Unit_UnitId", "dbo.Units");
			DropForeignKey("dbo.Events", "TakenBy_PersonId", "dbo.People");
			DropForeignKey("dbo.PeopleToUnits", "UnitId", "dbo.Units");
			DropForeignKey("dbo.Units", "ParentUnit_UnitId", "dbo.Units");
			DropForeignKey("dbo.PeopleToUnits", "PersonId", "dbo.People");
			DropForeignKey("dbo.PeopleToEvents", "PersonId", "dbo.People");
			DropForeignKey("dbo.PeopleToEvents", "EventId", "dbo.Events");
			DropIndex("dbo.Units", new[] { "ParentUnit_UnitId" });
			DropIndex("dbo.Units", new[] { "TawId" });
			DropIndex("dbo.PeopleToUnits", new[] { "UnitId" });
			DropIndex("dbo.PeopleToUnits", new[] { "PersonId" });
			DropIndex("dbo.People", new[] { "Name" });
			DropIndex("dbo.PeopleToEvents", new[] { "EventId" });
			DropIndex("dbo.PeopleToEvents", new[] { "PersonId" });
			DropIndex("dbo.Events", new[] { "Unit_UnitId" });
			DropIndex("dbo.Events", new[] { "TakenBy_PersonId" });
			DropIndex("dbo.Events", new[] { "TawId" });
			DropTable("dbo.Units");
			DropTable("dbo.PeopleToUnits");
			DropTable("dbo.People");
			DropTable("dbo.PeopleToEvents");
			DropTable("dbo.Events");
		}
	}
}