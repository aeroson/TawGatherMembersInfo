using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{

    public class MyDbContext : DbContext
    {
        public virtual IDbSet<Person> Persons { get; set; }
        public MyDbContext() : base("DefaultConnection")
        {

        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }
    }
}
