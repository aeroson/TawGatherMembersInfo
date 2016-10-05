using Neitri;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;
using System.Linq;
using System.Threading;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class ManuallyMapped : System.Attribute // System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute
	{
	}

	public class MyDbContext : DbContext, IDisposable
	{
		public Unit RootUnit => Units.OrderBy(u => u.TawId).FirstOrDefault();
		public virtual IDbSet<Event> Events { get; set; }
		public virtual IDbSet<Person> People { get; set; }
		public virtual IDbSet<Unit> Units { get; set; }
		public virtual IDbSet<PersonEvent> PersonEvents { get; set; }
		public virtual IDbSet<PersonUnit> PersonUnits { get; set; }
		public virtual IDbSet<PersonRank> PersonRanks { get; set; }

		public DbContextProvider dbContextProvider;

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

		public new void Dispose()
		{
			dbContextProvider?.OnDispose(this);
			base.Dispose();
		}

		protected override void OnModelCreating(DbModelBuilder b)
		{
			b.Entity<Person>().HasMany(t => t.Ranks).WithRequired(t => t.Person);
			b.Entity<Person>().HasMany(t => t.Units).WithRequired(t => t.Person);
			b.Entity<Person>().HasMany(t => t.Events).WithRequired(t => t.Person);
			b.Entity<Person>().HasMany(t => t.Commendations).WithRequired(t => t.Person);
			b.Entity<Person>().HasMany(t => t.Statuses).WithRequired(t => t.Person);

			b.Entity<Unit>().HasMany(t => t.People).WithRequired(t => t.Unit);

			b.Entity<PersonCommendation>().HasRequired(t => t.Commendation);
			b.Entity<PersonCommendation>().HasMany(t => t.Comments).WithRequired(t => t.PersonCommendation);

			b.Entity<PersonCommendationComment>().HasRequired(t => t.Person);

			//modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
		}
	}

	public class DbContextProvider : IOnDependenciesResolved
	{
		[Dependency]
		Config config;

		SemaphoreSlim simultaneousConnectionsSemaphore;

		public MyDbContext NewContext
		{
			get
			{
				simultaneousConnectionsSemaphore.Wait();

				//MySql.Data.MySqlClient.MySqlClientFactory
				// MySql doesn't support  MultipleActiveResultSets=True; http://stackoverflow.com/questions/25953560/mysql-connector-multipleactiveresultsets-issue
				var connectionString = config.MySqlConnectionString;
				var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);

				var context = new MyDbContext(connection, true);
				context.dbContextProvider = this;

				return context;
			}
		}

		public void OnDispose(MyDbContext context)
		{
			simultaneousConnectionsSemaphore.Release();
		}

		public void OnDependenciesResolved()
		{
			simultaneousConnectionsSemaphore = new SemaphoreSlim(config.MaxConcurrentDatabaseConnections);
		}
	}
}