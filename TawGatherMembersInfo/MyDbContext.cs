using System.Data.Entity;
using System.Linq;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class MyDbContext : DbContext
	{
		public Unit RootUnit => Units.OrderBy(u => u.TawId).FirstOrDefault();
		public virtual IDbSet<Event> Events { get; set; }
		public virtual IDbSet<Person> Persons { get; set; }
		public virtual IDbSet<Unit> Units { get; set; }
		public virtual IDbSet<PersonToEvent> PersonsToEvents { get; set; }
		public virtual IDbSet<PersonToUnit> PersonsToUnits { get; set; }

		public MyDbContext() : base("DefaultConnection")
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
		}
	}

	public class DbContextProvider
	{
		public MyDbContext NewContext
		{
			get
			{
				return new MyDbContext();
			}
		}
	}
}