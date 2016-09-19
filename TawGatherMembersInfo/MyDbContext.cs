using Neitri;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;
using System.Linq;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class MyDbContext : DbContext
	{
		public Unit RootUnit => Units.OrderBy(u => u.TawId).FirstOrDefault();
		public virtual IDbSet<Event> Events { get; set; }
		public virtual IDbSet<Person> People { get; set; }
		public virtual IDbSet<Unit> Units { get; set; }
		public virtual IDbSet<PersonToEvent> PeopleToEvents { get; set; }
		public virtual IDbSet<PersonToUnit> PeopleToUnits { get; set; }

		static MyDbContext()
		{
			// DropCreateDatabaseAlways, CreateDatabaseIfNotExists, DropCreateDatabaseIfModelChanges, MigrateDatabaseToLatestVersion
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyDbContext, TawGatherMembersInfo.Migrations.Configuration>());
		}

		public MyDbContext() :
			base(
				new MySql.Data.MySqlClient.MySqlConnection(
					(
						new Config()
						.LoadFile(
							new FileSystem()
							.GetFile("data", "config.xml")
						) as Config
					).MySqlConnectionString
				),
				true
			)
		{
			// DropCreateDatabaseAlways, CreateDatabaseIfNotExists, DropCreateDatabaseIfModelChanges, MigrateDatabaseToLatestVersion
		}

		public MyDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			//modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
		}
	}

	public class DbContextProvider
	{
		[Dependency]
		Config config;

		public MyDbContext NewContext
		{
			get
			{
				//MySql.Data.MySqlClient.MySqlClientFactory
				// MySql doesn't support  MultipleActiveResultSets=True; http://stackoverflow.com/questions/25953560/mysql-connector-multipleactiveresultsets-issue
				var connectionString = config.MySqlConnectionString;
				var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);

				return new MyDbContext(connection, true);
			}
		}
	}
}