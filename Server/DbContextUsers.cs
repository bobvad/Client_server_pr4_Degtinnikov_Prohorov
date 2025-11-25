using Microsoft.EntityFrameworkCore;
namespace Server
{
    public class DbContextUsers : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<CommandUser> CommandUsers { get; set; }

    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "server=127.0.0.1;port=3306;database=FtpServerDB;user=root;password=;",
                new MySqlServerVersion(new Version(8, 0, 11))
            );
        }
    }
}
