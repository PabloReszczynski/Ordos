using Ordos.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace Ordos.DataService.Data
{
    public class SystemContext : DbContext
    {

        public static string SQLExpressConnectionString =>
              ConfigurationManager.AppSettings["server"].ToString();
        public SystemContext()
        {
        }

        public SystemContext(DbContextOptions<SystemContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DisturbanceRecording> DisturbanceRecordings { get; set; }
        public DbSet<DRFile> DRFiles { get; set; }
        public DbSet<ConfigurationValue> ConfigurationValues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(SQLExpressConnectionString);
        }
    }
}
