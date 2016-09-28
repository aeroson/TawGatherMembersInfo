namespace TawGatherMembersInfo.Migrations
{
	using System.Collections.Generic;
	using System.Data.Entity;
	using System.Data.Entity.Migrations;
	using System.Data.Entity.Migrations.Design;
	using System.Data.Entity.Migrations.Model;
	using System.Data.Entity.Migrations.Sql;
	using System.Data.Entity.Migrations.Utilities;
	using System.Linq;

	internal sealed class Configuration : DbMigrationsConfiguration<TawGatherMembersInfo.MyDbContext>
	{
		public Configuration()
		{
			AutomaticMigrationsEnabled = false;

			ContextKey = "TawGatherMembersInfo.MyDbContext";

			// http://karthicraghupathi.com/2013/01/31/using-mysql-connector-net-6-6-4-with-entity-framework-5/
			SetSqlGenerator("MySql.Data.MySqlClient", new SqlGenerator());
			//SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
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

	// taken from: http://stackoverflow.com/a/12060958/782022
	class SqlGenerator : MySql.Data.Entity.MySqlMigrationSqlGenerator
	{
		public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
		{
			IEnumerable<MigrationStatement> res = base.Generate(migrationOperations, providerManifestToken);
			foreach (MigrationStatement ms in res)
			{
				ms.Sql = ms.Sql.Replace(".dbo.", ".");
				ms.Sql = ms.Sql.Replace("dbo.", "");
			}
			return res;
		}
	}
}