using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner
{
    public class CyNewsCornerContext : DbContext
    {
        public CyNewsCornerContext(DbContextOptions<CyNewsCornerContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=DESKTOP-HECM0AC\\SQLEXPRESS;Initial Catalog=cynewscorner;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        }
        public DbSet<DataModels.NewsSource> Sources { get; set; }
        public DbSet<DataModels.Category> Categories { get; set; }
    }
}
