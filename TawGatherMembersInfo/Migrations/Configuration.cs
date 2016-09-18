namespace TawGatherMembersInfo.Migrations
{
	using System;
	using System.Data.Entity;
	using System.Data.Entity.Migrations;
	using System.Linq;

	internal sealed class Configuration : DbMigrationsConfiguration<TawGatherMembersInfo.MyDbContext>
	{
		public Configuration()
		{
			AutomaticMigrationsEnabled = false;

			ContextKey = "TawGatherMembersInfo.MyDbContext";

			// http://karthicraghupathi.com/2013/01/31/using-mysql-connector-net-6-6-4-with-entity-framework-5/
			SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.Entity.MySqlMigrationSqlGenerator());
		}

		protected override void Seed(TawGatherMembersInfo.MyDbContext context)
		{
			//  This method will be called after migrating to the latest version.

			//  You can use the DbSet<T>.AddOrUpdate() helper extension method
			//  to avoid creating duplicate seed data. E.g.
			//
			//    context.People.AddOrUpdate(
			//      p => p.FullName,
			//      new Person { FullName = "Andrew Peters" },
			//      new Person { FullName = "Brice Lambson" },
			//      new Person { FullName = "Rowan Miller" }
			//    );
			//
		}
	}
}