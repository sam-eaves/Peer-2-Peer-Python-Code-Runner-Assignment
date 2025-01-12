using Microsoft.EntityFrameworkCore;
using WebServer.Models;

namespace WebServer.Data
{
    // DBManager using EntityFramework
    public class DBManager : DbContext
    {
        public DbSet<Client> Clients { get; set; }
        public DbSet<Job> Jobs { get; set; }

        public DBManager(DbContextOptions<DBManager> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source = Clients.db");
        }

        // Seed initial data for Clients, commented out for real use
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Can seed fake data here
        }
    }
}
