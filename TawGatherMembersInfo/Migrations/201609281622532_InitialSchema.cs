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
                        Id = c.Long(nullable: false, identity: true),
                        TawId = c.Long(nullable: false),
                        Name = c.String(maxLength: 500, storeType: "nvarchar"),
                        Description = c.String(unicode: false, storeType: "text"),
                        Type = c.String(maxLength: 500, storeType: "nvarchar"),
                        Mandatory = c.Boolean(nullable: false),
                        Cancelled = c.Boolean(nullable: false),
                        From = c.DateTime(nullable: false, precision: 0),
                        To = c.DateTime(nullable: false, precision: 0),
                        TakenBy_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.People", t => t.TakenBy_Id)
                .Index(t => t.TawId, unique: true)
                .Index(t => t.Mandatory)
                .Index(t => t.Cancelled)
                .Index(t => t.From)
                .Index(t => t.To)
                .Index(t => t.TakenBy_Id);
            
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
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(maxLength: 500, storeType: "nvarchar"),
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
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.PersonCommendations",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false, precision: 0),
                        Commendation_Id = c.Long(),
                        Person_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Commendations", t => t.Commendation_Id)
                .ForeignKey("dbo.People", t => t.Person_Id)
                .Index(t => t.Commendation_Id)
                .Index(t => t.Person_Id);
            
            CreateTable(
                "dbo.Commendations",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(unicode: false),
                        Type = c.Int(nullable: false),
                        Image = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PersonCommendationComments",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false, precision: 0),
                        Comment = c.String(unicode: false),
                        Person_Id = c.Long(),
                        PersonCommendation_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.People", t => t.Person_Id)
                .ForeignKey("dbo.PersonCommendations", t => t.PersonCommendation_Id)
                .Index(t => t.Person_Id)
                .Index(t => t.PersonCommendation_Id);
            
            CreateTable(
                "dbo.PersonRanks",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TawId = c.Long(nullable: false),
                        NameShort = c.String(maxLength: 10, storeType: "nvarchar"),
                        ValidFrom = c.DateTime(nullable: false, precision: 0),
                        NameLong = c.String(unicode: false),
                        ByWho_Id = c.Long(),
                        Person_Id = c.Long(),
                        Person_Id1 = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.People", t => t.ByWho_Id)
                .ForeignKey("dbo.People", t => t.Person_Id)
                .ForeignKey("dbo.People", t => t.Person_Id1)
                .Index(t => t.TawId)
                .Index(t => t.ByWho_Id)
                .Index(t => t.Person_Id)
                .Index(t => t.Person_Id1);
            
            CreateTable(
                "dbo.PersonUnits",
                c => new
                    {
                        PersonId = c.Long(nullable: false),
                        UnitId = c.Long(nullable: false),
                        PositionNameShort = c.String(maxLength: 500, storeType: "nvarchar"),
                        Joined = c.DateTime(nullable: false, precision: 0),
                        Removed = c.DateTime(nullable: false, precision: 0),
                        JoinedBy_Id = c.Long(),
                        RemovedBy_Id = c.Long(),
                        Person_Id = c.Long(),
                    })
                .PrimaryKey(t => new { t.PersonId, t.UnitId })
                .ForeignKey("dbo.People", t => t.JoinedBy_Id)
                .ForeignKey("dbo.People", t => t.PersonId, cascadeDelete: true)
                .ForeignKey("dbo.People", t => t.RemovedBy_Id)
                .ForeignKey("dbo.Units", t => t.UnitId, cascadeDelete: true)
                .ForeignKey("dbo.People", t => t.Person_Id)
                .Index(t => t.PersonId)
                .Index(t => t.UnitId)
                .Index(t => t.JoinedBy_Id)
                .Index(t => t.RemovedBy_Id)
                .Index(t => t.Person_Id);
            
            CreateTable(
                "dbo.Units",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TawId = c.Int(nullable: false),
                        Type = c.String(maxLength: 500, storeType: "nvarchar"),
                        Name = c.String(maxLength: 500, storeType: "nvarchar"),
                        ParentUnit_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Units", t => t.ParentUnit_Id)
                .Index(t => t.TawId, unique: true)
                .Index(t => t.ParentUnit_Id);
            
            CreateTable(
                "dbo.UnitEvents",
                c => new
                    {
                        Unit_Id = c.Long(nullable: false),
                        Event_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.Unit_Id, t.Event_Id })
                .ForeignKey("dbo.Units", t => t.Unit_Id, cascadeDelete: true)
                .ForeignKey("dbo.Events", t => t.Event_Id, cascadeDelete: true)
                .Index(t => t.Unit_Id)
                .Index(t => t.Event_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Events", "TakenBy_Id", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "Person_Id", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "UnitId", "dbo.Units");
            DropForeignKey("dbo.Units", "ParentUnit_Id", "dbo.Units");
            DropForeignKey("dbo.UnitEvents", "Event_Id", "dbo.Events");
            DropForeignKey("dbo.UnitEvents", "Unit_Id", "dbo.Units");
            DropForeignKey("dbo.PersonUnits", "RemovedBy_Id", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "PersonId", "dbo.People");
            DropForeignKey("dbo.PersonUnits", "JoinedBy_Id", "dbo.People");
            DropForeignKey("dbo.PersonRanks", "Person_Id1", "dbo.People");
            DropForeignKey("dbo.PersonRanks", "Person_Id", "dbo.People");
            DropForeignKey("dbo.PersonRanks", "ByWho_Id", "dbo.People");
            DropForeignKey("dbo.PersonCommendations", "Person_Id", "dbo.People");
            DropForeignKey("dbo.PersonCommendationComments", "PersonCommendation_Id", "dbo.PersonCommendations");
            DropForeignKey("dbo.PersonCommendationComments", "Person_Id", "dbo.People");
            DropForeignKey("dbo.PersonCommendations", "Commendation_Id", "dbo.Commendations");
            DropForeignKey("dbo.PersonEvents", "PersonId", "dbo.People");
            DropForeignKey("dbo.PersonEvents", "EventId", "dbo.Events");
            DropIndex("dbo.UnitEvents", new[] { "Event_Id" });
            DropIndex("dbo.UnitEvents", new[] { "Unit_Id" });
            DropIndex("dbo.Units", new[] { "ParentUnit_Id" });
            DropIndex("dbo.Units", new[] { "TawId" });
            DropIndex("dbo.PersonUnits", new[] { "Person_Id" });
            DropIndex("dbo.PersonUnits", new[] { "RemovedBy_Id" });
            DropIndex("dbo.PersonUnits", new[] { "JoinedBy_Id" });
            DropIndex("dbo.PersonUnits", new[] { "UnitId" });
            DropIndex("dbo.PersonUnits", new[] { "PersonId" });
            DropIndex("dbo.PersonRanks", new[] { "Person_Id1" });
            DropIndex("dbo.PersonRanks", new[] { "Person_Id" });
            DropIndex("dbo.PersonRanks", new[] { "ByWho_Id" });
            DropIndex("dbo.PersonRanks", new[] { "TawId" });
            DropIndex("dbo.PersonCommendationComments", new[] { "PersonCommendation_Id" });
            DropIndex("dbo.PersonCommendationComments", new[] { "Person_Id" });
            DropIndex("dbo.PersonCommendations", new[] { "Person_Id" });
            DropIndex("dbo.PersonCommendations", new[] { "Commendation_Id" });
            DropIndex("dbo.People", new[] { "Name" });
            DropIndex("dbo.PersonEvents", new[] { "AttendanceType" });
            DropIndex("dbo.PersonEvents", new[] { "EventId" });
            DropIndex("dbo.PersonEvents", new[] { "PersonId" });
            DropIndex("dbo.Events", new[] { "TakenBy_Id" });
            DropIndex("dbo.Events", new[] { "To" });
            DropIndex("dbo.Events", new[] { "From" });
            DropIndex("dbo.Events", new[] { "Cancelled" });
            DropIndex("dbo.Events", new[] { "Mandatory" });
            DropIndex("dbo.Events", new[] { "TawId" });
            DropTable("dbo.UnitEvents");
            DropTable("dbo.Units");
            DropTable("dbo.PersonUnits");
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
