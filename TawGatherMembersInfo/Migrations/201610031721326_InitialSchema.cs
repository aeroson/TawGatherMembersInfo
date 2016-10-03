namespace TawGatherMembersInfo.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialSchema : DbMigration
    {
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
                    })
                .PrimaryKey(t => t.EventId)
                .ForeignKey("dbo.People", t => t.TakenBy_PersonId)
                .Index(t => t.TawId, unique: true)
                .Index(t => t.Mandatory)
                .Index(t => t.Cancelled)
                .Index(t => t.From)
                .Index(t => t.To)
                .Index(t => t.TakenBy_PersonId);
            
            CreateTable(
                "dbo.PersonEvents",
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
                .Index(t => t.EventId)
                .Index(t => t.AttendanceType);
            
            CreateTable(
                "dbo.People",
                c => new
                    {
                        PersonId = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 500, storeType: "nvarchar"),
                        SteamId = c.Long(nullable: false),
                        AvatarImageUrl = c.String(maxLength: 1000, storeType: "nvarchar"),
                        Status = c.String(maxLength: 100, storeType: "nvarchar"),
                        DateJoinedTaw = c.DateTime(nullable: false, precision: 0),
                        LastProfileDataUpdatedDate = c.DateTime(nullable: false, precision: 0),
                        CountryCodeIso3166 = c.String(maxLength: 10, storeType: "nvarchar"),
                        BiographyContents = c.String(unicode: false),
                        AppliedForTaw = c.DateTime(nullable: false, precision: 0),
                        AdmittedToTaw = c.DateTime(nullable: false, precision: 0),
                    })
                .PrimaryKey(t => t.PersonId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.PersonCommendations",
                c => new
                    {
                        PersonCommendationId = c.Long(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false, precision: 0),
                        Commendation_CommendationId = c.Long(nullable: false),
                        Person_PersonId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.PersonCommendationId)
                .ForeignKey("dbo.Commendations", t => t.Commendation_CommendationId, cascadeDelete: true)
                .ForeignKey("dbo.People", t => t.Person_PersonId, cascadeDelete: true)
                .Index(t => t.Commendation_CommendationId)
                .Index(t => t.Person_PersonId);
            
            CreateTable(
                "dbo.Commendations",
                c => new
                    {
                        CommendationId = c.Long(nullable: false, identity: true),
                        Name = c.String(unicode: false),
                        Type = c.Int(nullable: false),
                        Image = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.CommendationId);
            
            CreateTable(
                "dbo.PersonCommendationComments",
                c => new
                    {
                        PersonCommendationCommentId = c.Long(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false, precision: 0),
                        Comment = c.String(unicode: false),
                        Person_PersonId = c.Long(nullable: false),
                        PersonCommendation_PersonCommendationId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.PersonCommendationCommentId)
                .ForeignKey("dbo.People", t => t.Person_PersonId, cascadeDelete: true)
                .ForeignKey("dbo.PersonCommendations", t => t.PersonCommendation_PersonCommendationId, cascadeDelete: true)
                .Index(t => t.Person_PersonId)
                .Index(t => t.PersonCommendation_PersonCommendationId);
            
            CreateTable(
                "dbo.PersonRanks",
                c => new
                    {
                        PersonRankId = c.Long(nullable: false, identity: true),
                        TawId = c.Long(nullable: false),
                        NameShort = c.String(maxLength: 10, storeType: "nvarchar"),
                        ValidFrom = c.DateTime(nullable: false, precision: 0),
                        PromotedBy_PersonId = c.Long(),
                        Person_PersonId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.PersonRankId)
                .ForeignKey("dbo.People", t => t.PromotedBy_PersonId)
                .ForeignKey("dbo.People", t => t.Person_PersonId, cascadeDelete: true)
                .Index(t => t.TawId)
                .Index(t => t.ValidFrom)
                .Index(t => t.PromotedBy_PersonId)
                .Index(t => t.Person_PersonId);
            
            CreateTable(
                "dbo.PersonStatus",
                c => new
                    {
                        PersonStatusId = c.Long(nullable: false, identity: true),
                        Other = c.String(unicode: false),
                        Type = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false, precision: 0),
                        ByWho_PersonId = c.Long(),
                        Person_PersonId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.PersonStatusId)
                .ForeignKey("dbo.People", t => t.ByWho_PersonId)
                .ForeignKey("dbo.People", t => t.Person_PersonId, cascadeDelete: true)
                .Index(t => t.ByWho_PersonId)
                .Index(t => t.Person_PersonId);
            
            CreateTable(
                "dbo.PersonUnits",
                c => new
                    {
                        PersonUnitId = c.Long(nullable: false, identity: true),
                        PositionNameShort = c.String(maxLength: 500, storeType: "nvarchar"),
                        Joined = c.DateTime(nullable: false, precision: 0),
                        Removed = c.DateTime(nullable: false, precision: 0),
                        JoinedBy_PersonId = c.Long(),
                        RemovedBy_PersonId = c.Long(),
                        Unit_UnitId = c.Long(nullable: false),
                        Person_PersonId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.PersonUnitId)
                .ForeignKey("dbo.People", t => t.JoinedBy_PersonId)
                .ForeignKey("dbo.People", t => t.RemovedBy_PersonId)
                .ForeignKey("dbo.Units", t => t.Unit_UnitId, cascadeDelete: true)
                .ForeignKey("dbo.People", t => t.Person_PersonId, cascadeDelete: true)
                .Index(t => t.Joined)
                .Index(t => t.Removed)
                .Index(t => t.JoinedBy_PersonId)
                .Index(t => t.RemovedBy_PersonId)
                .Index(t => t.Unit_UnitId)
                .Index(t => t.Person_PersonId);
            
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
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Events", "TakenBy_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "Person_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "Unit_UnitId", "dbo.Units");
            DropForeignKey("dbo.Units", "ParentUnit_UnitId", "dbo.Units");
            DropForeignKey("dbo.UnitEvents", "Event_EventId", "dbo.Events");
            DropForeignKey("dbo.UnitEvents", "Unit_UnitId", "dbo.Units");
            DropForeignKey("dbo.PersonUnits", "RemovedBy_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "JoinedBy_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonStatus", "Person_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonStatus", "ByWho_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonRanks", "Person_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonRanks", "PromotedBy_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonEvents", "PersonId", "dbo.People");
            DropForeignKey("dbo.PersonCommendations", "Person_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonCommendationComments", "PersonCommendation_PersonCommendationId", "dbo.PersonCommendations");
            DropForeignKey("dbo.PersonCommendationComments", "Person_PersonId", "dbo.People");
            DropForeignKey("dbo.PersonCommendations", "Commendation_CommendationId", "dbo.Commendations");
            DropForeignKey("dbo.PersonEvents", "EventId", "dbo.Events");
            DropIndex("dbo.UnitEvents", new[] { "Event_EventId" });
            DropIndex("dbo.UnitEvents", new[] { "Unit_UnitId" });
            DropIndex("dbo.Units", new[] { "ParentUnit_UnitId" });
            DropIndex("dbo.Units", new[] { "TawId" });
            DropIndex("dbo.PersonUnits", new[] { "Person_PersonId" });
            DropIndex("dbo.PersonUnits", new[] { "Unit_UnitId" });
            DropIndex("dbo.PersonUnits", new[] { "RemovedBy_PersonId" });
            DropIndex("dbo.PersonUnits", new[] { "JoinedBy_PersonId" });
            DropIndex("dbo.PersonUnits", new[] { "Removed" });
            DropIndex("dbo.PersonUnits", new[] { "Joined" });
            DropIndex("dbo.PersonStatus", new[] { "Person_PersonId" });
            DropIndex("dbo.PersonStatus", new[] { "ByWho_PersonId" });
            DropIndex("dbo.PersonRanks", new[] { "Person_PersonId" });
            DropIndex("dbo.PersonRanks", new[] { "PromotedBy_PersonId" });
            DropIndex("dbo.PersonRanks", new[] { "ValidFrom" });
            DropIndex("dbo.PersonRanks", new[] { "TawId" });
            DropIndex("dbo.PersonCommendationComments", new[] { "PersonCommendation_PersonCommendationId" });
            DropIndex("dbo.PersonCommendationComments", new[] { "Person_PersonId" });
            DropIndex("dbo.PersonCommendations", new[] { "Person_PersonId" });
            DropIndex("dbo.PersonCommendations", new[] { "Commendation_CommendationId" });
            DropIndex("dbo.People", new[] { "Name" });
            DropIndex("dbo.PersonEvents", new[] { "AttendanceType" });
            DropIndex("dbo.PersonEvents", new[] { "EventId" });
            DropIndex("dbo.PersonEvents", new[] { "PersonId" });
            DropIndex("dbo.Events", new[] { "TakenBy_PersonId" });
            DropIndex("dbo.Events", new[] { "To" });
            DropIndex("dbo.Events", new[] { "From" });
            DropIndex("dbo.Events", new[] { "Cancelled" });
            DropIndex("dbo.Events", new[] { "Mandatory" });
            DropIndex("dbo.Events", new[] { "TawId" });
            DropTable("dbo.UnitEvents");
            DropTable("dbo.Units");
            DropTable("dbo.PersonUnits");
            DropTable("dbo.PersonStatus");
            DropTable("dbo.PersonRanks");
            DropTable("dbo.PersonCommendationComments");
            DropTable("dbo.Commendations");
            DropTable("dbo.PersonCommendations");
            DropTable("dbo.People");
            DropTable("dbo.PersonEvents");
            DropTable("dbo.Events");
        }
    }
}
